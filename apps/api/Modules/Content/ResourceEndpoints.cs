using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MunicipalPlatform.Api.Infrastructure.Persistence;
using MunicipalPlatform.Api.Modules.Content.Domain;
using MunicipalPlatform.Api.Modules.Operations.Domain;
using MunicipalPlatform.Api.Platform.Tenancy;

namespace MunicipalPlatform.Api.Modules.Content;

public static class ResourceEndpoints
{
    private static readonly HashSet<string> AllowedKinds = new(StringComparer.OrdinalIgnoreCase)
    {
        "PAGE", "BANNER", "EVENT", "LEGISLATION", "DATASET", "LOCATION", "CONTACT", "ALERT", "MENU", "HOME_BLOCK", "PROCUREMENT_LINK", "ESIC_LINK", "OUVIDORIA_LINK"
    };

    public static IEndpointRouteBuilder MapResourceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/resources", PublicListAsync).AllowAnonymous().WithTags("Portal", "Resources");
        endpoints.MapGet("/api/v1/resources/{kind}/{slug}", PublicGetAsync).AllowAnonymous().WithTags("Portal", "Resources");

        var admin = endpoints.MapGroup("/api/v1/admin/resources").WithTags("Admin", "CMS");
        admin.MapGet("/", AdminListAsync).RequireAuthorization(p => p.RequireClaim("capability", "resources.manage"));
        admin.MapPost("/", CreateAsync).RequireAuthorization(p => p.RequireClaim("capability", "resources.manage"));
        admin.MapPut("/{id:guid}", UpdateAsync).RequireAuthorization(p => p.RequireClaim("capability", "resources.manage"));
        admin.MapPost("/{id:guid}/publish", PublishAsync).RequireAuthorization(p => p.RequireClaim("capability", "resources.manage"));
        admin.MapPost("/{id:guid}/archive", ArchiveAsync).RequireAuthorization(p => p.RequireClaim("capability", "resources.manage"));
        admin.MapPost("/{id:guid}/restore", RestoreAsync).RequireAuthorization(p => p.RequireClaim("capability", "resources.manage"));
        admin.MapGet("/{id:guid}/revisions", RevisionsAsync).RequireAuthorization(p => p.RequireClaim("capability", "resources.manage"));
        return endpoints;
    }

    private static async Task<IResult> PublicListAsync(string? kind, string? q, ApplicationDbContext database, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var query = database.PortalResources.AsNoTracking().Where(item => item.Status == "PUBLISHED" && (!item.StartsAt.HasValue || item.StartsAt <= now) && (!item.EndsAt.HasValue || item.EndsAt > now));
        if (!string.IsNullOrWhiteSpace(kind)) query = query.Where(item => item.Kind == kind.Trim().ToUpper());
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = $"%{q.Trim()}%";
            query = query.Where(item => EF.Functions.ILike(item.Title, term) || EF.Functions.ILike(item.Summary, term));
        }
        var items = await query.OrderBy(item => item.DisplayOrder).ThenByDescending(item => item.PublishedAt).Select(item => new { item.Id, item.Kind, item.Slug, item.Title, item.Summary, item.PayloadJson, item.DisplayOrder, item.StartsAt, item.EndsAt, item.PublishedAt, item.Version }).ToListAsync(cancellationToken);
        return Results.Ok(items.Select(ToPublicResponse));
    }

    private static async Task<IResult> PublicGetAsync(string kind, string slug, ApplicationDbContext database, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var normalizedKind = kind.Trim().ToUpperInvariant();
        var normalizedSlug = slug.Trim().ToLowerInvariant();
        var item = await database.PortalResources.AsNoTracking().SingleOrDefaultAsync(resource => resource.Kind == normalizedKind && resource.Slug == normalizedSlug && resource.Status == "PUBLISHED" && (!resource.StartsAt.HasValue || resource.StartsAt <= now) && (!resource.EndsAt.HasValue || resource.EndsAt > now), cancellationToken);
        return item is null ? Results.NotFound() : Results.Ok(ToPublicResponse(item));
    }

    private static async Task<IResult> AdminListAsync(string? kind, ApplicationDbContext database, CancellationToken cancellationToken)
    {
        var query = database.PortalResources.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(kind)) query = query.Where(item => item.Kind == kind.Trim().ToUpper());
        var items = await query.OrderBy(item => item.Kind).ThenBy(item => item.DisplayOrder).ThenBy(item => item.Title).ToListAsync(cancellationToken);
        return Results.Ok(items.Select(ToAdminResponse));
    }

    private static async Task<IResult> CreateAsync(ResourceRequest request, ClaimsPrincipal principal, HttpContext context, ApplicationDbContext database, TenantContext tenant, CancellationToken cancellationToken)
    {
        var error = ValidateRequest(request);
        if (error is not null) return Results.ValidationProblem(new Dictionary<string, string[]> { ["resource"] = [error] });
        var kind = request.Kind.Trim().ToUpperInvariant();
        var slug = request.Slug.Trim().ToLowerInvariant();
        if (await database.PortalResources.AnyAsync(item => item.Kind == kind && item.Slug == slug, cancellationToken)) return Results.Conflict(new { title = "Já existe recurso com este tipo e slug.", status = 409 });
        var actor = RequireActor(principal);
        try
        {
            var resource = new PortalResource(tenant.RequireMunicipalityId(), kind, slug, request.Title, request.Summary ?? string.Empty, request.PayloadJson ?? "{}", request.DisplayOrder, actor);
            resource.Update(request.Title, request.Summary ?? string.Empty, request.PayloadJson ?? "{}", request.DisplayOrder, request.StartsAt, request.EndsAt, actor, DateTimeOffset.UtcNow);
            database.PortalResources.Add(resource);
            AddRevision(database, tenant.RequireMunicipalityId(), resource, actor);
            AddAudit(database, tenant, actor, "resource.created", resource.Id, context.TraceIdentifier, new { resource.Kind, resource.Slug, resource.Version });
            await database.SaveChangesAsync(cancellationToken);
            return Results.Created($"/api/v1/admin/resources/{resource.Id}", ToAdminResponse(resource));
        }
        catch (Exception exception) when (exception is ArgumentException or JsonException) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["resource"] = [exception.Message] }); }
    }

    private static async Task<IResult> UpdateAsync(Guid id, ResourceUpdateRequest request, ClaimsPrincipal principal, HttpContext context, ApplicationDbContext database, TenantContext tenant, CancellationToken cancellationToken)
    {
        var resource = await database.PortalResources.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (resource is null) return Results.NotFound();
        if (resource.Version != request.ExpectedVersion) return Results.Conflict(new { title = "Conteúdo foi alterado por outra pessoa", detail = "Recarregue antes de salvar para evitar sobrescrever alterações.", currentVersion = resource.Version, status = 409 });
        var actor = RequireActor(principal);
        try
        {
            AddRevision(database, tenant.RequireMunicipalityId(), resource, actor);
            resource.Update(request.Title, request.Summary ?? string.Empty, request.PayloadJson ?? "{}", request.DisplayOrder, request.StartsAt, request.EndsAt, actor, DateTimeOffset.UtcNow);
            AddAudit(database, tenant, actor, "resource.updated", resource.Id, context.TraceIdentifier, new { resource.Kind, resource.Slug, resource.Version });
            await database.SaveChangesAsync(cancellationToken);
            return Results.Ok(ToAdminResponse(resource));
        }
        catch (Exception exception) when (exception is ArgumentException or JsonException or InvalidOperationException) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["resource"] = [exception.Message] }); }
    }

    private static Task<IResult> PublishAsync(Guid id, ClaimsPrincipal principal, HttpContext context, ApplicationDbContext database, TenantContext tenant, CancellationToken cancellationToken) => ChangeStatusAsync(id, "resource.published", principal, context, database, tenant, (item, actor, at) => item.Publish(actor, at), true, cancellationToken);
    private static Task<IResult> ArchiveAsync(Guid id, ClaimsPrincipal principal, HttpContext context, ApplicationDbContext database, TenantContext tenant, CancellationToken cancellationToken) => ChangeStatusAsync(id, "resource.archived", principal, context, database, tenant, (item, actor, at) => item.Archive(actor, at), false, cancellationToken);
    private static Task<IResult> RestoreAsync(Guid id, ClaimsPrincipal principal, HttpContext context, ApplicationDbContext database, TenantContext tenant, CancellationToken cancellationToken) => ChangeStatusAsync(id, "resource.restored", principal, context, database, tenant, (item, actor, at) => item.Restore(actor, at), false, cancellationToken);

    private static async Task<IResult> ChangeStatusAsync(Guid id, string action, ClaimsPrincipal principal, HttpContext context, ApplicationDbContext database, TenantContext tenant, Action<PortalResource, Guid, DateTimeOffset> transition, bool enqueue, CancellationToken cancellationToken)
    {
        var resource = await database.PortalResources.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (resource is null) return Results.NotFound();
        var actor = RequireActor(principal);
        try
        {
            AddRevision(database, tenant.RequireMunicipalityId(), resource, actor);
            transition(resource, actor, DateTimeOffset.UtcNow);
            if (enqueue) database.OutboxMessages.Add(new OutboxMessage(tenant.RequireMunicipalityId(), "portal.resource.published", JsonSerializer.Serialize(new { resource.Id, resource.Kind, resource.Slug, resource.Version })));
            AddAudit(database, tenant, actor, action, resource.Id, context.TraceIdentifier, new { resource.Status, resource.Version });
            await database.SaveChangesAsync(cancellationToken);
            return Results.Ok(ToAdminResponse(resource));
        }
        catch (InvalidOperationException exception) { return Results.Conflict(new { title = "Transição inválida", detail = exception.Message, status = 409 }); }
    }

    private static async Task<IResult> RevisionsAsync(Guid id, ApplicationDbContext database, CancellationToken cancellationToken)
    {
        var items = await database.ContentRevisions.AsNoTracking().Where(item => item.ResourceId == id).OrderByDescending(item => item.CreatedAt).Select(item => new { item.Id, item.ResourceKind, item.Version, item.SnapshotJson, item.CreatedBy, item.CreatedAt }).ToListAsync(cancellationToken);
        return Results.Ok(items);
    }

    private static string? ValidateRequest(ResourceRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Kind) || !AllowedKinds.Contains(request.Kind.Trim())) return $"Tipo inválido. Use: {string.Join(", ", AllowedKinds.Order())}.";
        if (string.IsNullOrWhiteSpace(request.Slug) || request.Slug.Any(character => !char.IsAsciiLetterOrDigit(character) && character != '-')) return "Slug deve conter apenas letras sem acento, números e hífen.";
        if (string.IsNullOrWhiteSpace(request.Title)) return "Título obrigatório.";
        return null;
    }

    private static object ToPublicResponse(PortalResource resource) => new { resource.Id, resource.Kind, resource.Slug, resource.Title, resource.Summary, payload = JsonSerializer.Deserialize<JsonElement>(resource.PayloadJson), resource.DisplayOrder, resource.StartsAt, resource.EndsAt, resource.PublishedAt, resource.Version };
    private static object ToAdminResponse(PortalResource resource) => new { resource.Id, resource.Kind, resource.Slug, resource.Title, resource.Summary, resource.PayloadJson, resource.Status, resource.DisplayOrder, resource.StartsAt, resource.EndsAt, resource.PublishedAt, resource.Version, resource.UpdatedAt, resource.UpdatedBy };
    private static Guid RequireActor(ClaimsPrincipal principal) => Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var actor) ? actor : throw new InvalidOperationException("Sessão sem identificador de usuário.");
    private static void AddRevision(ApplicationDbContext database, Guid municipalityId, PortalResource resource, Guid actor) => database.ContentRevisions.Add(new ContentRevision(municipalityId, resource.Kind, resource.Id, resource.Version, JsonSerializer.Serialize(ToAdminResponse(resource)), actor));
    private static void AddAudit(ApplicationDbContext database, TenantContext tenant, Guid actor, string action, Guid id, string correlationId, object diff) => database.AuditEvents.Add(new AuditEvent(tenant.RequireMunicipalityId(), actor, action, "PortalResource", id.ToString(), JsonSerializer.Serialize(diff), correlationId));

    public record ResourceRequest(string Kind, string Slug, string Title, string? Summary, string? PayloadJson, int DisplayOrder, DateTimeOffset? StartsAt, DateTimeOffset? EndsAt);
    public sealed record ResourceUpdateRequest(string Title, string? Summary, string? PayloadJson, int DisplayOrder, DateTimeOffset? StartsAt, DateTimeOffset? EndsAt, int ExpectedVersion);
}
