using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MunicipalPlatform.Api.Infrastructure.Persistence;
using MunicipalPlatform.Api.Modules.Content.Domain;
using MunicipalPlatform.Api.Modules.Operations.Domain;
using MunicipalPlatform.Api.Platform.Tenancy;

namespace MunicipalPlatform.Api.Modules.Content;

public static class ContentEndpoints
{
    public static IEndpointRouteBuilder MapContentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/admin/news").WithTags("Admin", "Content");
        group.MapGet("/", ListAsync).RequireAuthorization(policy => policy.RequireClaim("capability", "content.write"));
        group.MapPost("/", CreateAsync).RequireAuthorization(policy => policy.RequireClaim("capability", "content.write"));
        group.MapPost("/{id:guid}/submit", SubmitAsync).RequireAuthorization(policy => policy.RequireClaim("capability", "content.write"));
        group.MapPost("/{id:guid}/approve", ApproveAsync).RequireAuthorization(policy => policy.RequireClaim("capability", "content.review"));
        group.MapPost("/{id:guid}/publish", PublishAsync).RequireAuthorization(policy => policy.RequireClaim("capability", "content.publish"));
        return endpoints;
    }

    private static async Task<IResult> ListAsync(ApplicationDbContext database, CancellationToken cancellationToken)
    {
        var items = await database.NewsArticles.AsNoTracking()
            .OrderByDescending(article => article.UpdatedAt)
            .Select(article => new
            {
                article.Id,
                article.Title,
                article.Slug,
                article.Summary,
                article.Status,
                article.Version,
                article.IsFeatured,
                article.UpdatedAt,
                article.ScheduledFor,
                article.PublishedAt
            })
            .ToListAsync(cancellationToken);
        return Results.Ok(items.Select(item => new
        {
            item.Id,
            item.Title,
            item.Slug,
            item.Summary,
            Status = ToWireStatus(item.Status),
            item.Version,
            item.IsFeatured,
            item.UpdatedAt,
            item.ScheduledFor,
            item.PublishedAt
        }));
    }

    private static async Task<IResult> CreateAsync(
        NewsDraftRequest request,
        ClaimsPrincipal principal,
        HttpContext context,
        ApplicationDbContext database,
        TenantContext tenant,
        CancellationToken cancellationToken)
    {
        var errors = Validate(request);
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var normalizedSlug = request.Slug.Trim().ToLowerInvariant();
        if (await database.NewsArticles.AnyAsync(article => article.Slug == normalizedSlug, cancellationToken))
        {
            return Results.Conflict(new { title = "Slug já utilizado", status = StatusCodes.Status409Conflict });
        }

        var actorId = RequireActor(principal);
        var article = NewsArticle.Create(tenant.RequireMunicipalityId(), request.Title, request.Slug, actorId);
        article.UpdateDraft(
            request.Title,
            request.Summary,
            request.Body,
            request.CoverImageUrl,
            request.CoverImageAlt,
            request.IsFeatured,
            actorId,
            DateTimeOffset.UtcNow);
        database.NewsArticles.Add(article);
        AddAudit(database, tenant, actorId, "content.news.created", article.Id, context.TraceIdentifier, new { article.Title, article.Slug });
        await database.SaveChangesAsync(cancellationToken);
        return Results.Created($"/api/v1/admin/news/{article.Id}", ToResponse(article));
    }

    private static Task<IResult> SubmitAsync(Guid id, ClaimsPrincipal principal, HttpContext context, ApplicationDbContext database, TenantContext tenant, CancellationToken cancellationToken) =>
        TransitionAsync(id, "content.news.submitted", principal, context, database, tenant,
            (article, actor, at) => article.SubmitForReview(actor, at), cancellationToken);

    private static Task<IResult> ApproveAsync(Guid id, ClaimsPrincipal principal, HttpContext context, ApplicationDbContext database, TenantContext tenant, CancellationToken cancellationToken) =>
        TransitionAsync(id, "content.news.approved", principal, context, database, tenant,
            (article, actor, at) => article.Approve(actor, at), cancellationToken);

    private static Task<IResult> PublishAsync(Guid id, ClaimsPrincipal principal, HttpContext context, ApplicationDbContext database, TenantContext tenant, CancellationToken cancellationToken) =>
        TransitionAsync(id, "content.news.published", principal, context, database, tenant,
            (article, actor, at) => article.Publish(actor, at), cancellationToken);

    private static async Task<IResult> TransitionAsync(
        Guid id,
        string action,
        ClaimsPrincipal principal,
        HttpContext context,
        ApplicationDbContext database,
        TenantContext tenant,
        Action<NewsArticle, Guid, DateTimeOffset> transition,
        CancellationToken cancellationToken)
    {
        var article = await database.NewsArticles.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (article is null)
        {
            return Results.NotFound();
        }

        var actorId = RequireActor(principal);
        try
        {
            transition(article, actorId, DateTimeOffset.UtcNow);
        }
        catch (EditorialTransitionException exception)
        {
            return Results.Conflict(new { title = "Transição editorial inválida", detail = exception.Message, status = StatusCodes.Status409Conflict });
        }

        AddAudit(database, tenant, actorId, action, article.Id, context.TraceIdentifier, new { status = ToWireStatus(article.Status), article.Version });
        await database.SaveChangesAsync(cancellationToken);
        return Results.Ok(ToResponse(article));
    }

    private static Dictionary<string, string[]> Validate(NewsDraftRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Trim().Length > 180) errors["title"] = ["Informe um título com até 180 caracteres."];
        if (string.IsNullOrWhiteSpace(request.Slug) || request.Slug.Any(character => !char.IsAsciiLetterOrDigit(character) && character != '-')) errors["slug"] = ["Use somente letras sem acento, números e hífen no slug."];
        if (string.IsNullOrWhiteSpace(request.Summary) || request.Summary.Trim().Length > 320) errors["summary"] = ["Informe uma linha fina com até 320 caracteres."];
        if (string.IsNullOrWhiteSpace(request.Body) || request.Body.Trim().Length > 100_000) errors["body"] = ["Informe o conteúdo da notícia."];
        if (!string.IsNullOrWhiteSpace(request.CoverImageUrl) && string.IsNullOrWhiteSpace(request.CoverImageAlt)) errors["coverImageAlt"] = ["Texto alternativo é obrigatório para imagem de capa."];
        return errors;
    }

    private static Guid RequireActor(ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId)
            ? actorId
            : throw new InvalidOperationException("A sessão autenticada não possui identificador de usuário.");

    private static void AddAudit(ApplicationDbContext database, TenantContext tenant, Guid actorId, string action, Guid id, string correlationId, object diff) =>
        database.AuditEvents.Add(new AuditEvent(tenant.RequireMunicipalityId(), actorId, action, "NewsArticle", id.ToString(), JsonSerializer.Serialize(diff), correlationId));

    private static object ToResponse(NewsArticle article) => new
    {
        article.Id,
        article.Title,
        article.Slug,
        Status = ToWireStatus(article.Status),
        article.Version,
        article.UpdatedAt
    };

    private static string ToWireStatus(EditorialStatus status) => status switch
    {
        EditorialStatus.Draft => "DRAFT",
        EditorialStatus.InReview => "IN_REVIEW",
        EditorialStatus.Approved => "APPROVED",
        EditorialStatus.Scheduled => "SCHEDULED",
        EditorialStatus.Published => "PUBLISHED",
        EditorialStatus.Archived => "ARCHIVED",
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };

    public sealed record NewsDraftRequest(
        string Title,
        string Slug,
        string Summary,
        string Body,
        string? CoverImageUrl,
        string? CoverImageAlt,
        bool IsFeatured);
}
