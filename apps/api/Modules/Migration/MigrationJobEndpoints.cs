using System.Globalization;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MunicipalPlatform.Api.Infrastructure.Persistence;
using MunicipalPlatform.Api.Modules.Migration.Domain;
using MunicipalPlatform.Api.Modules.Operations.Domain;
using MunicipalPlatform.Api.Platform.Tenancy;

namespace MunicipalPlatform.Api.Modules.Migration;

public static class MigrationJobEndpoints
{
    public static IEndpointRouteBuilder MapMigrationJobEndpoints(this IEndpointRouteBuilder endpoints) { var group = endpoints.MapGroup("/api/v1/admin/migration/jobs").WithTags("Admin", "Migration").RequireAuthorization(p => p.RequireClaim("capability", "migration.manage")); group.MapGet("/", ListAsync); group.MapGet("/{id:guid}", GetAsync); group.MapGet("/{id:guid}/urls", ListUrlsAsync); group.MapGet("/{id:guid}/report.csv", ExportReportAsync); group.MapPost("/", CreateAsync); group.MapPost("/{id:guid}/begin-dry-run", BeginDryRunAsync); group.MapPost("/{id:guid}/evidence", AddEvidenceAsync); return endpoints; }
    private static async Task<IResult> ListAsync(ApplicationDbContext db, CancellationToken ct) => Results.Ok(await db.MigrationJobs.AsNoTracking().OrderByDescending(x => x.CreatedAt).Take(100).ToListAsync(ct));
    private static async Task<IResult> GetAsync(Guid id, ApplicationDbContext db, CancellationToken ct) { var job = await db.MigrationJobs.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct); if (job is null) return Results.NotFound(); var urlCount = await db.LegacyUrls.AsNoTracking().CountAsync(x => x.MigrationJobId == id, ct); var evidence = await db.MigrationEvidences.AsNoTracking().Where(x => x.MigrationJobId == id).OrderByDescending(x => x.CreatedAt).Take(100).ToListAsync(ct); return Results.Ok(new { job, urlCount, evidence }); }
    private static async Task<IResult> ListUrlsAsync(Guid id, int? page, int? pageSize, string? q, string? classification, string? state, string? kind, ApplicationDbContext db, CancellationToken ct)
    {
        var selectedPage = page ?? 1;
        var selectedPageSize = pageSize ?? 100;
        if (selectedPage < 1 || selectedPageSize is < 1 or > 500)
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["pagination"] = ["Page deve ser maior que zero e pageSize deve estar entre 1 e 500."] });
        if (!await db.MigrationJobs.AsNoTracking().AnyAsync(x => x.Id == id, ct)) return Results.NotFound();

        var query = db.LegacyUrls.AsNoTracking().Where(x => x.MigrationJobId == id);
        var search = q?.Trim();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search}%";
            query = string.Equals(db.Database.ProviderName, "Microsoft.EntityFrameworkCore.InMemory", StringComparison.Ordinal)
                ? query.Where(x => x.Url.Contains(search) || x.NormalizedPath.Contains(search))
                : query.Where(x => EF.Functions.ILike(x.Url, pattern) || EF.Functions.ILike(x.NormalizedPath, pattern));
        }
        var selectedClassification = classification?.Trim().ToUpperInvariant();
        if (!string.IsNullOrWhiteSpace(selectedClassification))
            query = query.Where(x => x.Classification == selectedClassification);
        var selectedState = state?.Trim().ToUpperInvariant();
        if (!string.IsNullOrWhiteSpace(selectedState))
            query = query.Where(x => x.State == selectedState);
        var selectedKind = kind?.Trim().ToUpperInvariant();
        query = selectedKind switch
        {
            null or "" or "ALL" => query,
            "FAILURE" => query.Where(x => x.State == "FAILED"),
            "HTML" => query.Where(x => x.ContentType == "text/html" || x.ContentType == "application/xhtml+xml"),
            "IMAGE" => query.Where(x => x.ContentType != null && x.ContentType.StartsWith("image/")),
            "DOCUMENT" => query.Where(x => x.ContentType == "application/pdf"
                || x.ContentType != null && (x.ContentType.Contains("msword") || x.ContentType.Contains("officedocument") || x.ContentType.Contains("spreadsheet"))),
            _ => null!
        };
        if (query is null)
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["kind"] = ["Kind deve ser ALL, FAILURE, HTML, IMAGE ou DOCUMENT."] });

        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(x => x.NormalizedPath).ThenBy(x => x.Id)
            .Skip((selectedPage - 1) * selectedPageSize).Take(selectedPageSize).ToListAsync(ct);
        var totalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)selectedPageSize);
        return Results.Ok(new { page = selectedPage, pageSize = selectedPageSize, total, totalPages, items });
    }
    private static async Task<IResult> ExportReportAsync(Guid id, ApplicationDbContext db, CancellationToken ct)
    {
        if (!await db.MigrationJobs.AsNoTracking().AnyAsync(x => x.Id == id, ct)) return Results.NotFound();
        var items = await db.LegacyUrls.AsNoTracking().Where(x => x.MigrationJobId == id)
            .OrderBy(x => x.NormalizedPath).ThenBy(x => x.Id).ToListAsync(ct);
        var csv = new StringBuilder("url,normalizedPath,depth,contentType,contentLength,sha256,classification,state,failureReason,discoveredAt\r\n");
        foreach (var item in items)
        {
            csv.AppendJoin(',',
                CsvCell(item.Url),
                CsvCell(item.NormalizedPath),
                CsvCell(item.Depth.ToString(CultureInfo.InvariantCulture)),
                CsvCell(item.ContentType),
                CsvCell(item.ContentLength?.ToString(CultureInfo.InvariantCulture)),
                CsvCell(item.Sha256),
                CsvCell(item.Classification),
                CsvCell(item.State),
                CsvCell(item.FailureReason),
                CsvCell(item.DiscoveredAt.ToString("O", CultureInfo.InvariantCulture)));
            csv.Append("\r\n");
        }
        return Results.File(
            Encoding.UTF8.GetBytes(csv.ToString()),
            "text/csv; charset=utf-8",
            $"migration-{id:N}-inventory.csv");
    }

    private static string CsvCell(string? input)
    {
        var value = input ?? string.Empty;
        if (value.Length > 0 && value[0] is '=' or '+' or '-' or '@' or '\t' or '\r') value = $"'{value}";
        return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }
    private static async Task<IResult> CreateAsync(CreateMigrationJobRequest request, ClaimsPrincipal principal, HttpContext context, ApplicationDbContext db, TenantContext tenant, CancellationToken ct) { if (!Uri.TryCreate(request.SourceBaseUrl, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https")) return Results.ValidationProblem(new Dictionary<string, string[]> { { "sourceBaseUrl", ["URL HTTP/HTTPS absoluta obrigatória."] } }); var allowed = request.AllowedHost.Trim().ToLowerInvariant(); if (!string.Equals(uri.Host, allowed, StringComparison.OrdinalIgnoreCase)) return Results.ValidationProblem(new Dictionary<string, string[]> { { "allowedHost", ["AllowedHost deve ser exatamente o host da URL de origem."] } }); if (request.MaxDepth is < 0 or > 10 || request.MaxPages is < 1 or > 20000) return Results.ValidationProblem(new Dictionary<string, string[]> { { "limits", ["MaxDepth 0-10 e MaxPages 1-20000."] } }); var job = new MigrationJob(tenant.RequireMunicipalityId(), uri.ToString(), allowed, request.MaxDepth, request.MaxPages); db.MigrationJobs.Add(job); AddAudit(db, tenant, principal, context, "migration.job.created", job.Id, new { job.SourceBaseUrl, job.AllowedHost, job.MaxDepth, job.MaxPages }); await db.SaveChangesAsync(ct); return Results.Created($"/api/v1/admin/migration/jobs/{job.Id}", job); }
    private static async Task<IResult> BeginDryRunAsync(Guid id, ClaimsPrincipal principal, HttpContext context, ApplicationDbContext db, TenantContext tenant, CancellationToken ct) { var job = await db.MigrationJobs.SingleOrDefaultAsync(x => x.Id == id, ct); if (job is null) return Results.NotFound(); if (job.State is MigrationJobState.Importing or MigrationJobState.Validating) return Results.Conflict(new { title = "Job já está em execução." }); job.Transition(MigrationJobState.DryRun, job.DiscoveredCount, job.ImportedCount, job.FailedCount); AddAudit(db, tenant, principal, context, "migration.dryrun.started", job.Id, new { job.State, job.DiscoveredCount }); await db.SaveChangesAsync(ct); return Results.Accepted($"/api/v1/admin/migration/jobs/{job.Id}", new { job, detail = "Estado de dry-run persistido. O crawler/importer só deve operar no host explicitamente autorizado." }); }
    private static async Task<IResult> AddEvidenceAsync(Guid id, EvidenceRequest request, ClaimsPrincipal principal, HttpContext context, ApplicationDbContext db, TenantContext tenant, CancellationToken ct) { if (!await db.MigrationJobs.AnyAsync(x => x.Id == id, ct)) return Results.NotFound(); try { using var _ = JsonDocument.Parse(request.PayloadJson); } catch (JsonException) { return Results.ValidationProblem(new Dictionary<string, string[]> { { "payloadJson", ["JSON inválido."] } }); } var evidence = new MigrationEvidence(tenant.RequireMunicipalityId(), id, request.Kind, request.Reference, request.PayloadJson); db.MigrationEvidences.Add(evidence); AddAudit(db, tenant, principal, context, "migration.evidence.added", evidence.Id, new { id, evidence.Kind, evidence.Reference }); await db.SaveChangesAsync(ct); return Results.Created($"/api/v1/admin/migration/jobs/{id}/evidence/{evidence.Id}", evidence); }
    private static void AddAudit(ApplicationDbContext db, TenantContext tenant, ClaimsPrincipal principal, HttpContext context, string action, Guid id, object diff) => db.AuditEvents.Add(new AuditEvent(tenant.RequireMunicipalityId(), RequireActor(principal), action, "MigrationJob", id.ToString(), JsonSerializer.Serialize(diff), context.TraceIdentifier));
    private static Guid RequireActor(ClaimsPrincipal p) => Guid.TryParse(p.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : throw new InvalidOperationException("Sessão inválida.");
    public sealed record CreateMigrationJobRequest(string SourceBaseUrl, string AllowedHost, int MaxDepth, int MaxPages); public sealed record EvidenceRequest(string Kind, string Reference, string PayloadJson);
}
