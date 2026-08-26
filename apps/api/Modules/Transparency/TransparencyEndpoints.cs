using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using MunicipalPlatform.Api.Infrastructure.Persistence;
using MunicipalPlatform.Api.Modules.Operations.Domain;
using MunicipalPlatform.Api.Modules.Transparency.Domain;
using MunicipalPlatform.Api.Platform.Storage;
using MunicipalPlatform.Api.Platform.Tenancy;

namespace MunicipalPlatform.Api.Modules.Transparency;

public static partial class TransparencyEndpoints
{
    private const long MaxDatasetBytes = 50L * 1024 * 1024;

    public static IEndpointRouteBuilder MapTransparencyEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var publicGroup = endpoints.MapGroup("/api/v1/public/datasets").WithTags("Public", "OpenData").AllowAnonymous();
        publicGroup.MapGet("/", ListPublishedAsync);
        publicGroup.MapGet("/{slug}", GetPublishedAsync);
        publicGroup.MapGet("/{datasetId:guid}/versions/{version:int}/download", DownloadVersionAsync);

        var admin = endpoints.MapGroup("/api/v1/admin/datasets").WithTags("Admin", "OpenData").RequireAuthorization(p => p.RequireClaim("capability", "datasets.manage"));
        admin.MapGet("/", ListAdminAsync);
        admin.MapPost("/", CreateAsync);
        admin.MapPut("/{id:guid}", UpdateAsync);
        admin.MapPost("/{id:guid}/publish", PublishAsync);
        admin.MapPost("/{id:guid}/archive", ArchiveAsync);
        admin.MapPost("/{id:guid}/versions", AddVersionAsync).DisableAntiforgery();
        return endpoints;
    }

    private static async Task<IResult> ListPublishedAsync(ApplicationDbContext db, CancellationToken ct)
    {
        var datasets = await db.Datasets.AsNoTracking().Where(x => x.Status == DatasetStatus.Published).OrderBy(x => x.Title).ToListAsync(ct);
        var ids = datasets.Select(x => x.Id).ToArray();
        var versions = await db.DatasetVersions.AsNoTracking().Where(x => ids.Contains(x.DatasetId)).GroupBy(x => x.DatasetId).Select(g => new { DatasetId = g.Key, LatestVersion = g.Max(x => x.Version) }).ToListAsync(ct);
        var versionMap = versions.ToDictionary(x => x.DatasetId, x => x.LatestVersion);
        return Results.Ok(datasets.Select(x => new { x.Id, x.Title, x.Slug, x.Description, x.Category, x.ResponsibleDepartment, x.License, x.UpdateFrequency, x.ReferencePeriod, x.LastUpdatedAt, x.NextExpectedUpdateAt, x.Source, latestVersion = versionMap.GetValueOrDefault(x.Id) }));
    }

    private static async Task<IResult> GetPublishedAsync(string slug, ApplicationDbContext db, CancellationToken ct)
    {
        var dataset = await db.Datasets.AsNoTracking().SingleOrDefaultAsync(x => x.Slug == slug && x.Status == DatasetStatus.Published, ct);
        if (dataset is null) return Results.NotFound();
        var versions = await db.DatasetVersions.AsNoTracking().Where(x => x.DatasetId == dataset.Id).OrderByDescending(x => x.Version).Select(x => new { x.Version, x.FileName, x.MimeType, x.SizeBytes, x.Sha256, x.Format, x.MetadataJson, x.PublishedAt }).ToListAsync(ct);
        return Results.Ok(new { dataset, versions });
    }

    private static async Task<IResult> DownloadVersionAsync(Guid datasetId, int version, ApplicationDbContext db, IObjectStorageProvider storage, CancellationToken ct)
    {
        var dataset = await db.Datasets.AsNoTracking().SingleOrDefaultAsync(x => x.Id == datasetId && x.Status == DatasetStatus.Published, ct);
        if (dataset is null) return Results.NotFound();
        var item = await db.DatasetVersions.AsNoTracking().SingleOrDefaultAsync(x => x.DatasetId == datasetId && x.Version == version, ct);
        if (item is null) return Results.NotFound();
        if (storage.State == "NOT_CONFIGURED") return Results.Problem(title: "Storage não configurado", statusCode: StatusCodes.Status503ServiceUnavailable);
        var bytes = await storage.ReadAsync(item.ObjectKey, ct);
        return bytes is null ? Results.NotFound() : Results.File(bytes, item.MimeType, item.FileName);
    }

    private static async Task<IResult> ListAdminAsync(ApplicationDbContext db, CancellationToken ct)
    {
        var datasets = await db.Datasets.AsNoTracking().OrderByDescending(x => x.UpdatedAt).Take(250).ToListAsync(ct);
        return Results.Ok(datasets);
    }

    private static async Task<IResult> CreateAsync(DatasetCreateRequest request, ClaimsPrincipal principal, HttpContext context, ApplicationDbContext db, TenantContext tenant, CancellationToken ct)
    {
        var slug = NormalizeSlug(request.Slug);
        if (slug is null) return Results.ValidationProblem(new Dictionary<string, string[]> { ["slug"] = ["Slug deve conter apenas letras minúsculas, números e hífens."] });
        if (await db.Datasets.AnyAsync(x => x.Slug == slug, ct)) return Results.Conflict(new { title = "Slug já cadastrado", status = 409 });
        Dataset dataset;
        try { dataset = new Dataset(tenant.RequireMunicipalityId(), request.Title, slug, request.Description, request.Category, request.ResponsibleDepartment, request.License, request.UpdateFrequency); }
        catch (ArgumentException ex) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["dataset"] = [ex.Message] }); }
        dataset.UpdateMetadata(request.Title, request.Description, request.Category, request.ResponsibleDepartment, request.License, request.UpdateFrequency, request.ReferencePeriod, request.Source, request.NextExpectedUpdateAt);
        db.Datasets.Add(dataset);
        AddAudit(db, tenant, principal, context, "dataset.created", dataset.Id, new { dataset.Title, dataset.Slug, dataset.Status });
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/admin/datasets/{dataset.Id}", dataset);
    }

    private static async Task<IResult> UpdateAsync(Guid id, DatasetUpdateRequest request, ClaimsPrincipal principal, HttpContext context, ApplicationDbContext db, TenantContext tenant, CancellationToken ct)
    {
        var dataset = await db.Datasets.SingleOrDefaultAsync(x => x.Id == id, ct); if (dataset is null) return Results.NotFound();
        try { dataset.UpdateMetadata(request.Title, request.Description, request.Category, request.ResponsibleDepartment, request.License, request.UpdateFrequency, request.ReferencePeriod, request.Source, request.NextExpectedUpdateAt); }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["dataset"] = [ex.Message] }); }
        AddAudit(db, tenant, principal, context, "dataset.updated", dataset.Id, new { dataset.Title, dataset.Category, dataset.UpdateFrequency });
        await db.SaveChangesAsync(ct); return Results.Ok(dataset);
    }

    private static async Task<IResult> PublishAsync(Guid id, ClaimsPrincipal principal, HttpContext context, ApplicationDbContext db, TenantContext tenant, CancellationToken ct)
    {
        var dataset = await db.Datasets.SingleOrDefaultAsync(x => x.Id == id, ct); if (dataset is null) return Results.NotFound();
        if (!await db.DatasetVersions.AnyAsync(x => x.DatasetId == id, ct)) return Results.Conflict(new { title = "Publique ao menos uma versão de arquivo antes do dataset." });
        try { dataset.Publish(DateTimeOffset.UtcNow); } catch (InvalidOperationException ex) { return Results.Conflict(new { title = ex.Message }); }
        AddAudit(db, tenant, principal, context, "dataset.published", dataset.Id, new { dataset.Status, dataset.PublishedAt }); await db.SaveChangesAsync(ct); return Results.Ok(dataset);
    }

    private static async Task<IResult> ArchiveAsync(Guid id, ClaimsPrincipal principal, HttpContext context, ApplicationDbContext db, TenantContext tenant, CancellationToken ct)
    {
        var dataset = await db.Datasets.SingleOrDefaultAsync(x => x.Id == id, ct); if (dataset is null) return Results.NotFound(); dataset.Archive(DateTimeOffset.UtcNow); AddAudit(db, tenant, principal, context, "dataset.archived", dataset.Id, new { dataset.Status }); await db.SaveChangesAsync(ct); return Results.Ok(dataset);
    }

    private static async Task<IResult> AddVersionAsync(Guid id, IFormFile file, ClaimsPrincipal principal, HttpContext context, ApplicationDbContext db, TenantContext tenant, IObjectStorageProvider storage, CancellationToken ct, string metadataJson = "{}")
    {
        var dataset = await db.Datasets.SingleOrDefaultAsync(x => x.Id == id, ct); if (dataset is null) return Results.NotFound();
        if (dataset.Status == DatasetStatus.Archived) return Results.Conflict(new { title = "Dataset arquivado não recebe novas versões." });
        if (storage.State == "NOT_CONFIGURED") return Results.Problem(title: "Storage não configurado", detail: storage.Description, statusCode: StatusCodes.Status503ServiceUnavailable);
        if (file.Length <= 0 || file.Length > MaxDatasetBytes) return Results.ValidationProblem(new Dictionary<string, string[]> { ["file"] = ["Arquivo deve possuir até 50 MB."] });
        try { using var _ = JsonDocument.Parse(metadataJson); } catch (JsonException) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["metadataJson"] = ["Metadados devem ser JSON válido."] }); }
        await using var input = file.OpenReadStream(); using var memory = new MemoryStream(); await input.CopyToAsync(memory, ct); var bytes = memory.ToArray();
        var detected = DetectDataset(bytes, file.FileName); if (detected is null) return Results.Problem(title: "Formato não permitido", detail: "São aceitos CSV, JSON, XLSX e PDF validados pelo conteúdo/assinatura do arquivo.", statusCode: StatusCodes.Status415UnsupportedMediaType);
        var sha = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        if (await db.DatasetVersions.AnyAsync(x => x.DatasetId == id && x.Sha256 == sha, ct)) return Results.Conflict(new { title = "Este conteúdo já foi versionado no dataset." });
        var nextVersion = (await db.DatasetVersions.Where(x => x.DatasetId == id).MaxAsync(x => (int?)x.Version, ct) ?? 0) + 1;
        var objectKey = $"datasets/{dataset.Id:N}/v{nextVersion}/{Guid.NewGuid():N}.{detected.Value.Extension}"; await storage.SaveAsync(objectKey, bytes, ct);
        var now = DateTimeOffset.UtcNow; var version = new DatasetVersion(tenant.RequireMunicipalityId(), dataset.Id, nextVersion, Path.GetFileName(file.FileName), objectKey, detected.Value.Mime, bytes.LongLength, sha, detected.Value.Format, metadataJson, now); dataset.MarkVersionPublished(now); db.DatasetVersions.Add(version);
        AddAudit(db, tenant, principal, context, "dataset.version.published", version.Id, new { datasetId = dataset.Id, version.Version, version.FileName, version.Sha256, version.Format, storage.State }); await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/admin/datasets/{dataset.Id}/versions/{version.Version}", version);
    }

    private static (string Mime, string Extension, string Format)? DetectDataset(byte[] bytes, string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (extension == ".pdf" && bytes.Length >= 5 && Encoding.ASCII.GetString(bytes, 0, 5) == "%PDF-") return ("application/pdf", "pdf", "PDF");
        if (extension == ".xlsx" && bytes.Length >= 4 && bytes[0] == 0x50 && bytes[1] == 0x4B && bytes[2] == 0x03 && bytes[3] == 0x04) return ("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "xlsx", "XLSX");
        if (extension is ".csv" or ".json")
        {
            try { var text = new UTF8Encoding(false, true).GetString(bytes); if (text.IndexOf('\0') >= 0) return null; if (extension == ".json") using (JsonDocument.Parse(text)) { } return extension == ".json" ? ("application/json", "json", "JSON") : ("text/csv; charset=utf-8", "csv", "CSV"); }
            catch (Exception ex) when (ex is DecoderFallbackException or JsonException) { return null; }
        }
        return null;
    }

    private static string? NormalizeSlug(string value) { var normalized = value.Trim().ToLowerInvariant(); return SlugRegex().IsMatch(normalized) ? normalized : null; }
    private static void AddAudit(ApplicationDbContext db, TenantContext tenant, ClaimsPrincipal principal, HttpContext context, string action, Guid resourceId, object diff) => db.AuditEvents.Add(new AuditEvent(tenant.RequireMunicipalityId(), RequireActor(principal), action, "Dataset", resourceId.ToString(), JsonSerializer.Serialize(diff), context.TraceIdentifier));
    private static Guid RequireActor(ClaimsPrincipal p) => Guid.TryParse(p.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : throw new InvalidOperationException("Sessão inválida.");
    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant)] private static partial Regex SlugRegex();

    public sealed record DatasetCreateRequest(string Title,string Slug,string Description,string Category,string ResponsibleDepartment,string License,string UpdateFrequency,string? ReferencePeriod,string? Source,DateTimeOffset? NextExpectedUpdateAt);
    public sealed record DatasetUpdateRequest(string Title,string Description,string Category,string ResponsibleDepartment,string License,string UpdateFrequency,string? ReferencePeriod,string? Source,DateTimeOffset? NextExpectedUpdateAt);
}
