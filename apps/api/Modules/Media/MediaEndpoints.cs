using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MunicipalPlatform.Api.Infrastructure.Persistence;
using MunicipalPlatform.Api.Modules.Media.Domain;
using MunicipalPlatform.Api.Modules.Media.Providers;
using MunicipalPlatform.Api.Modules.Media.Services;
using MunicipalPlatform.Api.Modules.Operations.Domain;
using MunicipalPlatform.Api.Platform.Storage;
using MunicipalPlatform.Api.Platform.Tenancy;

namespace MunicipalPlatform.Api.Modules.Media;

public static class MediaEndpoints
{
    public static IEndpointRouteBuilder MapMediaEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/media/{id:guid}", PublicReadAsync).AllowAnonymous().WithTags("Media");
        endpoints.MapGet("/api/v1/media/{id:guid}/metadata", PublicMetadataAsync).AllowAnonymous().WithTags("Media");
        var group = endpoints.MapGroup("/api/v1/admin/media").WithTags("Admin", "Media").RequireAuthorization(p => p.RequireClaim("capability", "media.manage"));
        group.MapGet("/", ListAsync);
        group.MapPost("/upload", UploadAsync).DisableAntiforgery();
        group.MapPut("/{id:guid}/metadata", UpdateMetadataAsync);
        group.MapPut("/{id:guid}/presentation", UpdatePresentationAsync);
        group.MapPost("/{id:guid}/review", ReviewAsync);
        group.MapPost("/{id:guid}/reject", RejectAsync);
        return endpoints;
    }

    private static async Task<IResult> PublicReadAsync(Guid id, HttpContext context, ApplicationDbContext db, IObjectStorageProvider storage, CancellationToken ct)
    {
        var asset = await db.MediaAssets.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.Status == "APPROVED", ct);
        if (asset is null) return Results.NotFound();
        if (storage.State == "NOT_CONFIGURED") return Results.Problem(title: "Mídia indisponível", detail: "O storage público ainda não foi configurado.", statusCode: StatusCodes.Status503ServiceUnavailable);
        var etag = $"\"sha256-{asset.Sha256}\"";
        context.Response.Headers.ETag = etag;
        context.Response.Headers.CacheControl = "public,max-age=86400,immutable";
        if (context.Request.Headers.IfNoneMatch.Any(value => string.Equals(value, etag, StringComparison.Ordinal))) return Results.StatusCode(StatusCodes.Status304NotModified);
        var bytes = await storage.ReadAsync(asset.ObjectKey, ct);
        return bytes is null ? Results.NotFound() : Results.File(bytes, asset.MimeType, enableRangeProcessing: false);
    }

    private static async Task<IResult> PublicMetadataAsync(Guid id, ApplicationDbContext db, CancellationToken ct)
    {
        var asset = await db.MediaAssets.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.Status == "APPROVED", ct);
        if (asset is null) return Results.NotFound();
        return Results.Ok(new
        {
            asset.Id,
            asset.MimeType,
            asset.AltText,
            asset.Caption,
            asset.Credit,
            tags = ParseTags(asset.TagsCsv),
            focalPoint = new { x = asset.FocalPointX ?? 0.5m, y = asset.FocalPointY ?? 0.5m },
            crop = asset.CropX.HasValue && asset.CropY.HasValue && asset.CropWidth.HasValue && asset.CropHeight.HasValue
                ? new { x = asset.CropX.Value, y = asset.CropY.Value, width = asset.CropWidth.Value, height = asset.CropHeight.Value }
                : null,
            asset.Sha256
        });
    }

    private static async Task<IResult> ListAsync(string? q, string? status, int? page, int? pageSize, HttpContext context, ApplicationDbContext db, CancellationToken ct)
    {
        var normalizedPage = Math.Clamp(page.GetValueOrDefault(1), 1, 1_000_000);
        var normalizedPageSize = Math.Clamp(pageSize.GetValueOrDefault(50), 1, 100);
        var query = db.MediaAssets.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = status.Trim().ToUpperInvariant();
            query = query.Where(asset => asset.Status == normalizedStatus);
        }
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            if (term.Length > 120) return Results.ValidationProblem(new Dictionary<string, string[]> { ["q"] = ["A busca deve possuir até 120 caracteres."] });
            if (string.Equals(db.Database.ProviderName, "Npgsql.EntityFrameworkCore.PostgreSQL", StringComparison.Ordinal))
            {
                var pattern = $"%{term}%";
                query = query.Where(asset =>
                    EF.Functions.ILike(asset.OriginalFileName, pattern)
                    || EF.Functions.ILike(asset.MimeType, pattern)
                    || EF.Functions.ILike(asset.AltText, pattern)
                    || EF.Functions.ILike(asset.Caption, pattern)
                    || EF.Functions.ILike(asset.Credit, pattern)
                    || EF.Functions.ILike(asset.TagsCsv, pattern));
            }
            else
            {
                query = query.Where(asset =>
                    asset.OriginalFileName.Contains(term, StringComparison.OrdinalIgnoreCase)
                    || asset.MimeType.Contains(term, StringComparison.OrdinalIgnoreCase)
                    || asset.AltText.Contains(term, StringComparison.OrdinalIgnoreCase)
                    || asset.Caption.Contains(term, StringComparison.OrdinalIgnoreCase)
                    || asset.Credit.Contains(term, StringComparison.OrdinalIgnoreCase)
                    || asset.TagsCsv.Contains(term, StringComparison.OrdinalIgnoreCase));
            }
        }

        var total = await query.CountAsync(ct);
        context.Response.Headers.Append("X-Total-Count", total.ToString(CultureInfo.InvariantCulture));
        var items = await query
            .OrderByDescending(asset => asset.UploadedAt)
            .ThenBy(asset => asset.Id)
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToListAsync(ct);
        return Results.Ok(items);
    }

    private static async Task<IResult> UploadAsync(IFormFile file, ClaimsPrincipal principal, HttpContext context, ApplicationDbContext db, TenantContext tenant, IObjectStorageProvider storage, IMalwareScanner scanner, CancellationToken ct, [FromForm] string? altText = null, [FromForm] string? caption = null, [FromForm] string? credit = null)
    {
        if (file.Length <= 0 || file.Length > DocumentFileInspector.MaxBytes) return Results.ValidationProblem(new Dictionary<string, string[]> { ["file"] = ["Arquivo deve possuir até 25 MB."] });
        if (storage.State == "NOT_CONFIGURED") return Results.Problem(title: "Storage não configurado", detail: storage.Description, statusCode: StatusCodes.Status503ServiceUnavailable);
        await using var stream = file.OpenReadStream();
        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory, ct);
        var bytes = memory.ToArray();
        var originalFileName = Path.GetFileName(file.FileName);
        var detected = DocumentFileInspector.Detect(bytes, originalFileName);
        if (detected is null) return Results.Problem(title: "Tipo de arquivo não permitido", detail: "São aceitos JPEG, PNG, WebP, PDF e documentos Office identificados por extensão, estrutura e bytes reais.", statusCode: StatusCodes.Status415UnsupportedMediaType);
        if (!DocumentFileInspector.IsDeclaredMimeCompatible(file.ContentType, detected.MimeType))
            return Results.Problem(title: "MIME incompatível", detail: "O tipo declarado pelo upload não corresponde ao conteúdo real do arquivo.", statusCode: StatusCodes.Status415UnsupportedMediaType);
        var sha = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        var scan = await scanner.ScanAsync(bytes, ct);
        if (!scan.IsClean && scanner.State != "NOT_CONFIGURED")
            return Results.Problem(title: "Arquivo recusado", detail: scan.Detail, statusCode: StatusCodes.Status422UnprocessableEntity);
        var objectKey = $"media/{DateTimeOffset.UtcNow:yyyy/MM}/{Guid.NewGuid():N}.{detected.Extension}";
        await storage.SaveAsync(objectKey, bytes, ct);
        var actor = RequireActor(principal);
        var asset = new MediaAsset(tenant.RequireMunicipalityId(), objectKey, originalFileName, detected.MimeType, bytes.LongLength, sha, actor);
        asset.UpdateMetadata(altText, caption, credit);
        if (scan.IsClean && scanner.State != "NOT_CONFIGURED") asset.Approve();
        db.MediaAssets.Add(asset);
        AddAudit(db, tenant, actor, "media.uploaded", asset, context.TraceIdentifier, new { asset.OriginalFileName, asset.MimeType, asset.SizeBytes, asset.Sha256, asset.Status, scannerState = scanner.State, storageState = storage.State });
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/admin/media/{asset.Id}", new { asset.Id, asset.ObjectKey, asset.OriginalFileName, asset.MimeType, asset.SizeBytes, asset.Sha256, asset.Status, scan = new { scannerState = scanner.State, scan.Detail } });
    }

    private static async Task<IResult> UpdateMetadataAsync(Guid id, MediaMetadataRequest request, ClaimsPrincipal principal, HttpContext context, ApplicationDbContext db, TenantContext tenant, CancellationToken ct)
    {
        var asset = await db.MediaAssets.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (asset is null) return Results.NotFound();
        try { asset.UpdateMetadata(request.AltText, request.Caption, request.Credit); }
        catch (ArgumentException ex) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["metadata"] = [ex.Message] }); }
        var actor = RequireActor(principal);
        AddAudit(db, tenant, actor, "media.metadata.updated", asset, context.TraceIdentifier, new { asset.AltText, asset.Caption, asset.Credit });
        await db.SaveChangesAsync(ct);
        return Results.Ok(asset);
    }

    private static async Task<IResult> UpdatePresentationAsync(Guid id, MediaPresentationRequest request, ClaimsPrincipal principal, HttpContext context, ApplicationDbContext db, TenantContext tenant, CancellationToken ct)
    {
        var asset = await db.MediaAssets.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (asset is null) return Results.NotFound();
        try
        {
            asset.UpdatePresentation(request.Tags, request.FocalPointX, request.FocalPointY, request.CropX, request.CropY, request.CropWidth, request.CropHeight);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["presentation"] = [ex.Message] });
        }
        var actor = RequireActor(principal);
        AddAudit(db, tenant, actor, "media.presentation.updated", asset, context.TraceIdentifier, new
        {
            asset.TagsCsv,
            focalPoint = new { x = asset.FocalPointX, y = asset.FocalPointY },
            crop = new { x = asset.CropX, y = asset.CropY, width = asset.CropWidth, height = asset.CropHeight }
        });
        await db.SaveChangesAsync(ct);
        return Results.Ok(asset);
    }

    private static async Task<IResult> ReviewAsync(Guid id, ClaimsPrincipal principal, HttpContext context, ApplicationDbContext db, TenantContext tenant, IObjectStorageProvider storage, IMalwareScanner scanner, CancellationToken ct)
    {
        var asset = await db.MediaAssets.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (asset is null) return Results.NotFound();
        if (asset.Status == "APPROVED") return Results.Ok(asset);
        if (storage.State == "NOT_CONFIGURED") return Results.Problem(title: "Storage não configurado", detail: storage.Description, statusCode: StatusCodes.Status503ServiceUnavailable);
        if (scanner.State == "NOT_CONFIGURED") return Results.Problem(title: "Scanner antimalware não configurado", detail: scanner.Description, statusCode: StatusCodes.Status503ServiceUnavailable);

        var bytes = await storage.ReadAsync(asset.ObjectKey, ct);
        if (bytes is null) return Results.Problem(title: "Objeto de mídia ausente", detail: "O metadado existe, mas o arquivo não foi localizado no storage.", statusCode: StatusCodes.Status409Conflict);
        var scan = await scanner.ScanAsync(bytes, ct);
        var actor = RequireActor(principal);
        if (!scan.IsClean)
        {
            asset.Reject();
            AddAudit(db, tenant, actor, "media.rejected.by_scan", asset, context.TraceIdentifier, new { scannerState = scanner.State, scan.Detail });
            await db.SaveChangesAsync(ct);
            return Results.Json(new { asset.Id, asset.Status, scannerState = scanner.State, scan.Detail }, statusCode: StatusCodes.Status422UnprocessableEntity);
        }

        asset.Approve();
        AddAudit(db, tenant, actor, "media.approved.after_scan", asset, context.TraceIdentifier, new { scannerState = scanner.State, scan.Detail, asset.Sha256 });
        await db.SaveChangesAsync(ct);
        return Results.Ok(asset);
    }

    private static async Task<IResult> RejectAsync(Guid id, MediaRejectRequest request, ClaimsPrincipal principal, HttpContext context, ApplicationDbContext db, TenantContext tenant, CancellationToken ct)
    {
        var asset = await db.MediaAssets.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (asset is null) return Results.NotFound();
        var reason = NormalizeReason(request.Reason);
        asset.Reject();
        var actor = RequireActor(principal);
        AddAudit(db, tenant, actor, "media.rejected.manual", asset, context.TraceIdentifier, new { reason, asset.Sha256 });
        await db.SaveChangesAsync(ct);
        return Results.Ok(asset);
    }

    private static string[] ParseTags(string value) => string.IsNullOrWhiteSpace(value)
        ? []
        : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static string NormalizeReason(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "Rejeição administrativa sem justificativa informada.";
        var normalized = value.Trim();
        return normalized.Length <= 500 ? normalized : normalized[..500];
    }

    private static void AddAudit(ApplicationDbContext db, TenantContext tenant, Guid actor, string action, MediaAsset asset, string correlationId, object diff) =>
        db.AuditEvents.Add(new AuditEvent(tenant.RequireMunicipalityId(), actor, action, "MediaAsset", asset.Id.ToString(), JsonSerializer.Serialize(diff), correlationId));

    private static Guid RequireActor(ClaimsPrincipal p) => Guid.TryParse(p.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : throw new InvalidOperationException("Sessão inválida.");
    public sealed record MediaMetadataRequest(string? AltText, string? Caption, string? Credit);
    public sealed record MediaPresentationRequest(string? Tags, decimal FocalPointX, decimal FocalPointY, decimal? CropX, decimal? CropY, decimal? CropWidth, decimal? CropHeight);
    public sealed record MediaRejectRequest(string? Reason);
}
