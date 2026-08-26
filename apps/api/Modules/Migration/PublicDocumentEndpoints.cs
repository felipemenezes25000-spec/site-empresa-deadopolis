using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MunicipalPlatform.Api.Infrastructure.Persistence;
using MunicipalPlatform.Api.Modules.Operations.Domain;
using MunicipalPlatform.Api.Platform.Storage;
using MunicipalPlatform.Api.Platform.Tenancy;

namespace MunicipalPlatform.Api.Modules.Migration;

public static class PublicDocumentEndpoints
{
    public static IEndpointRouteBuilder MapPublicDocumentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var publicGroup = endpoints.MapGroup("/api/v1/public/documents").WithTags("Public", "Documents").AllowAnonymous();
        publicGroup.MapGet("/", ListPublishedAsync);
        publicGroup.MapGet("/{id:guid}", GetPublishedAsync);
        publicGroup.MapGet("/{id:guid}/download", DownloadAsync);

        var admin = endpoints.MapGroup("/api/v1/admin/documents")
            .WithTags("Admin", "Documents")
            .RequireAuthorization(policy => policy.RequireClaim("capability", "migration.manage"));
        admin.MapGet("/", ListAdminAsync);
        admin.MapPost("/{id:guid}/publish", PublishAsync);
        admin.MapPost("/{id:guid}/archive", ArchiveAsync);
        admin.MapPost("/{id:guid}/restore", RestoreAsync);
        return endpoints;
    }

    private static async Task<IResult> ListPublishedAsync(
        ApplicationDbContext database,
        CancellationToken cancellationToken,
        string? q = null,
        string? category = null,
        string? subcategory = null,
        string? type = null,
        string? department = null,
        int? year = null,
        int page = 1,
        int pageSize = 25)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = ApplyFilters(
            database.PublicDocuments.AsNoTracking().Where(item => item.Status == "PUBLISHED"),
            q, category, subcategory, type, department, year);
        var total = await query.CountAsync(cancellationToken);
        var documents = await query
            .OrderByDescending(item => item.PublicationDate)
            .ThenBy(item => item.Title)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        var items = documents.Select(ToPublicItem).ToList();
        return Results.Ok(new { page, pageSize, total, totalPages = (int)Math.Ceiling(total / (double)pageSize), items });
    }

    private static async Task<IResult> GetPublishedAsync(Guid id, ApplicationDbContext database, CancellationToken cancellationToken)
    {
        var item = await database.PublicDocuments.AsNoTracking().SingleOrDefaultAsync(document => document.Id == id && document.Status == "PUBLISHED", cancellationToken);
        return item is null ? Results.NotFound() : Results.Ok(ToPublicItem(item));
    }

    private static async Task<IResult> DownloadAsync(
        Guid id,
        ApplicationDbContext database,
        IObjectStorageProvider storage,
        CancellationToken cancellationToken)
    {
        var item = await database.PublicDocuments.AsNoTracking().SingleOrDefaultAsync(document => document.Id == id && document.Status == "PUBLISHED", cancellationToken);
        if (item is null) return Results.NotFound();
        var asset = await database.MediaAssets.AsNoTracking().SingleOrDefaultAsync(media => media.Id == item.MediaAssetId && media.Status == "APPROVED", cancellationToken);
        if (asset is null) return Results.NotFound();
        if (storage.State == "NOT_CONFIGURED")
            return Results.Problem(title: "Storage não configurado", detail: storage.Description, statusCode: StatusCodes.Status503ServiceUnavailable);
        var bytes = await storage.ReadAsync(asset.ObjectKey, cancellationToken);
        return bytes is null ? Results.NotFound() : Results.File(bytes, item.MimeType, item.OriginalFileName, enableRangeProcessing: true);
    }

    private static async Task<IResult> ListAdminAsync(
        ApplicationDbContext database,
        CancellationToken cancellationToken,
        string? status = null,
        int page = 1,
        int pageSize = 50)
    {
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);
        var query = database.PublicDocuments.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = status.Trim().ToUpperInvariant();
            query = query.Where(item => item.Status == normalizedStatus);
        }
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(item => item.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return Results.Ok(new { page, pageSize, total, items });
    }

    private static async Task<IResult> PublishAsync(Guid id, ClaimsPrincipal principal, HttpContext context, ApplicationDbContext database, TenantContext tenant, CancellationToken cancellationToken)
    {
        var document = await database.PublicDocuments.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (document is null) return Results.NotFound();
        var approved = await database.MediaAssets.AsNoTracking().AnyAsync(item => item.Id == document.MediaAssetId && item.Status == "APPROVED", cancellationToken);
        if (!approved) return Results.Conflict(new { title = "Documento ainda não foi aprovado pelo pipeline de segurança.", status = 409 });
        try { document.Publish(DateTimeOffset.UtcNow); }
        catch (InvalidOperationException exception) { return Results.Conflict(new { title = exception.Message, status = 409 }); }
        AddAudit(database, tenant, principal, context, "document.published", document);
        await database.SaveChangesAsync(cancellationToken);
        return Results.Ok(document);
    }

    private static Task<IResult> ArchiveAsync(Guid id, ClaimsPrincipal principal, HttpContext context, ApplicationDbContext database, TenantContext tenant, CancellationToken cancellationToken) =>
        ChangeStatusAsync(id, principal, context, database, tenant, "document.archived", document => document.Archive(DateTimeOffset.UtcNow), cancellationToken);

    private static Task<IResult> RestoreAsync(Guid id, ClaimsPrincipal principal, HttpContext context, ApplicationDbContext database, TenantContext tenant, CancellationToken cancellationToken) =>
        ChangeStatusAsync(id, principal, context, database, tenant, "document.restored", document => document.Restore(DateTimeOffset.UtcNow), cancellationToken);

    private static async Task<IResult> ChangeStatusAsync(Guid id, ClaimsPrincipal principal, HttpContext context, ApplicationDbContext database, TenantContext tenant, string action, Action<Domain.PublicDocument> transition, CancellationToken cancellationToken)
    {
        var document = await database.PublicDocuments.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (document is null) return Results.NotFound();
        try { transition(document); }
        catch (InvalidOperationException exception) { return Results.Conflict(new { title = exception.Message, status = 409 }); }
        AddAudit(database, tenant, principal, context, action, document);
        await database.SaveChangesAsync(cancellationToken);
        return Results.Ok(document);
    }

    private static IQueryable<Domain.PublicDocument> ApplyFilters(IQueryable<Domain.PublicDocument> query, string? q, string? category, string? subcategory, string? type, string? department, int? year)
    {
        if (!string.IsNullOrWhiteSpace(q))
        {
            var pattern = $"%{q.Trim()}%";
            query = query.Where(item => EF.Functions.ILike(item.Title, pattern)
                || EF.Functions.ILike(item.Description, pattern)
                || EF.Functions.ILike(item.DocumentNumber, pattern)
                || EF.Functions.ILike(item.ProcessNumber, pattern));
        }
        if (!string.IsNullOrWhiteSpace(category))
        {
            var normalizedCategory = category.Trim().ToUpperInvariant();
            query = query.Where(item => item.Category == normalizedCategory);
        }
        if (!string.IsNullOrWhiteSpace(subcategory))
        {
            var normalizedSubcategory = subcategory.Trim().ToUpperInvariant();
            query = query.Where(item => item.Subcategory == normalizedSubcategory);
        }
        if (!string.IsNullOrWhiteSpace(type))
        {
            var normalizedType = type.Trim().ToUpperInvariant();
            query = query.Where(item => item.DocumentType == normalizedType);
        }
        if (!string.IsNullOrWhiteSpace(department)) query = query.Where(item => item.ResponsibleDepartment == department.Trim());
        if (year.HasValue) query = query.Where(item => item.PublicationDate.HasValue && item.PublicationDate.Value.Year == year.Value);
        return query;
    }

    private static object ToPublicItem(Domain.PublicDocument item) => new
    {
        item.Id, item.Category, item.Subcategory, item.Title, item.Description, item.DocumentNumber,
        item.ProcessNumber, item.ReferencePeriod, item.PublicationDate, item.ResponsibleDepartment,
        item.DocumentType, item.SourceUrl, item.OriginalFileName, item.MimeType, item.SizeBytes,
        item.Sha256, item.SourceSystem, item.PublishedAt, downloadUrl = $"/api/v1/public/documents/{item.Id}/download"
    };

    private static void AddAudit(ApplicationDbContext database, TenantContext tenant, ClaimsPrincipal principal, HttpContext context, string action, Domain.PublicDocument document) =>
        database.AuditEvents.Add(new AuditEvent(
            tenant.RequireMunicipalityId(), RequireActor(principal), action, "PublicDocument", document.Id.ToString(),
            JsonSerializer.Serialize(new { document.Status, document.Category, document.MediaAssetId }), context.TraceIdentifier));

    private static Guid RequireActor(ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : throw new InvalidOperationException("Sessão inválida.");
}
