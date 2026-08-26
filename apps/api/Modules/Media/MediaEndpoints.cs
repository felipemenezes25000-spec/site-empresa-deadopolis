using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MunicipalPlatform.Api.Infrastructure.Persistence;
using MunicipalPlatform.Api.Modules.Media.Domain;
using MunicipalPlatform.Api.Modules.Media.Providers;
using MunicipalPlatform.Api.Modules.Operations.Domain;
using MunicipalPlatform.Api.Platform.Storage;
using MunicipalPlatform.Api.Platform.Tenancy;

namespace MunicipalPlatform.Api.Modules.Media;

public static class MediaEndpoints
{
    private const long MaxBytes = 25 * 1024 * 1024;

    public static IEndpointRouteBuilder MapMediaEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/admin/media").WithTags("Admin", "Media").RequireAuthorization(p => p.RequireClaim("capability", "media.manage"));
        group.MapGet("/", ListAsync);
        group.MapPost("/upload", UploadAsync).DisableAntiforgery();
        group.MapPut("/{id:guid}/metadata", UpdateMetadataAsync);
        return endpoints;
    }

    private static async Task<IResult> ListAsync(ApplicationDbContext db, CancellationToken ct) =>
        Results.Ok(await db.MediaAssets.AsNoTracking().OrderByDescending(x => x.UploadedAt).Take(200).ToListAsync(ct));

    private static async Task<IResult> UploadAsync(IFormFile file, ClaimsPrincipal principal, HttpContext context, ApplicationDbContext db, TenantContext tenant, IObjectStorageProvider storage, IMalwareScanner scanner, CancellationToken ct, string? altText = null, string? caption = null, string? credit = null)
    {
        if (file.Length <= 0 || file.Length > MaxBytes) return Results.ValidationProblem(new Dictionary<string, string[]> { ["file"] = ["Arquivo deve possuir até 25 MB."] });
        if (storage.State == "NOT_CONFIGURED") return Results.Problem(title: "Storage não configurado", detail: storage.Description, statusCode: StatusCodes.Status503ServiceUnavailable);
        await using var stream = file.OpenReadStream(MaxBytes);
        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory, ct);
        var bytes = memory.ToArray();
        var detected = Detect(bytes);
        if (detected is null) return Results.Problem(title: "Tipo de arquivo não permitido", detail: "São aceitos JPEG, PNG, WebP e PDF identificados pelos bytes reais do arquivo.", statusCode: StatusCodes.Status415UnsupportedMediaType);
        var sha = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        var objectKey = $"media/{DateTimeOffset.UtcNow:yyyy/MM}/{Guid.NewGuid():N}.{detected.Value.Extension}";
        await storage.SaveAsync(objectKey, bytes, ct);
        var actor = RequireActor(principal);
        var asset = new MediaAsset(tenant.RequireMunicipalityId(), objectKey, Path.GetFileName(file.FileName), detected.Value.Mime, bytes.LongLength, sha, actor);
        asset.UpdateMetadata(altText, caption, credit);
        var scan = await scanner.ScanAsync(bytes, ct);
        if (scan.IsClean && scanner.State != "NOT_CONFIGURED") asset.Approve();
        db.MediaAssets.Add(asset);
        db.AuditEvents.Add(new AuditEvent(
            tenant.RequireMunicipalityId(), actor, "media.uploaded", "MediaAsset", asset.Id.ToString(),
            JsonSerializer.Serialize(new { asset.OriginalFileName, asset.MimeType, asset.SizeBytes, asset.Sha256, asset.Status, scannerState = scanner.State, storageState = storage.State }),
            context.TraceIdentifier));
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/admin/media/{asset.Id}", new { asset.Id, asset.ObjectKey, asset.OriginalFileName, asset.MimeType, asset.SizeBytes, asset.Sha256, asset.Status, scan = new { scannerState = scanner.State, scan.Detail } });
    }

    private static async Task<IResult> UpdateMetadataAsync(Guid id, MediaMetadataRequest request, ClaimsPrincipal principal, HttpContext context, ApplicationDbContext db, TenantContext tenant, CancellationToken ct)
    {
        var asset = await db.MediaAssets.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (asset is null) return Results.NotFound();
        try { asset.UpdateMetadata(request.AltText, request.Caption, request.Credit); }
        catch (ArgumentException ex) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["metadata"] = [ex.Message] }); }
        db.AuditEvents.Add(new AuditEvent(tenant.RequireMunicipalityId(), RequireActor(principal), "media.metadata.updated", "MediaAsset", asset.Id.ToString(), JsonSerializer.Serialize(new { asset.AltText, asset.Caption, asset.Credit }), context.TraceIdentifier));
        await db.SaveChangesAsync(ct);
        return Results.Ok(asset);
    }

    private static (string Mime, string Extension)? Detect(byte[] bytes)
    {
        if (bytes.Length >= 4 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF) return ("image/jpeg", "jpg");
        if (bytes.Length >= 8 && bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })) return ("image/png", "png");
        if (bytes.Length >= 12 && System.Text.Encoding.ASCII.GetString(bytes, 0, 4) == "RIFF" && System.Text.Encoding.ASCII.GetString(bytes, 8, 4) == "WEBP") return ("image/webp", "webp");
        if (bytes.Length >= 5 && System.Text.Encoding.ASCII.GetString(bytes, 0, 5) == "%PDF-") return ("application/pdf", "pdf");
        return null;
    }

    private static Guid RequireActor(ClaimsPrincipal p) => Guid.TryParse(p.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : throw new InvalidOperationException("Sessão inválida.");
    public sealed record MediaMetadataRequest(string? AltText, string? Caption, string? Credit);
}
