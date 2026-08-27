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
        group.MapGet("/", ListAsync).RequireAuthorization(p => p.RequireClaim("capability", "content.write"));
        group.MapGet("/{id:guid}", GetAsync).RequireAuthorization(p => p.RequireClaim("capability", "content.write"));
        group.MapPost("/", CreateAsync).RequireAuthorization(p => p.RequireClaim("capability", "content.write"));
        group.MapPut("/{id:guid}", UpdateAsync).RequireAuthorization(p => p.RequireClaim("capability", "content.write"));
        group.MapPost("/{id:guid}/submit", SubmitAsync).RequireAuthorization(p => p.RequireClaim("capability", "content.write"));
        group.MapPost("/{id:guid}/approve", ApproveAsync).RequireAuthorization(p => p.RequireClaim("capability", "content.review"));
        group.MapPost("/{id:guid}/schedule", ScheduleAsync).RequireAuthorization(p => p.RequireClaim("capability", "content.publish"));
        group.MapPost("/{id:guid}/publish", PublishAsync).RequireAuthorization(p => p.RequireClaim("capability", "content.publish"));
        group.MapGet("/{id:guid}/revisions", RevisionsAsync).RequireAuthorization(p => p.RequireClaim("capability", "content.write"));
        return endpoints;
    }

    private static async Task<IResult> ListAsync(ApplicationDbContext database, CancellationToken cancellationToken)
    {
        var items = await database.NewsArticles.AsNoTracking().OrderByDescending(article => article.UpdatedAt).ToListAsync(cancellationToken);
        return Results.Ok(items.Select(ToResponse));
    }

    private static async Task<IResult> GetAsync(Guid id, ApplicationDbContext database, CancellationToken cancellationToken)
    {
        var article = await database.NewsArticles.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        return article is null ? Results.NotFound() : Results.Ok(ToResponse(article));
    }

    private static async Task<IResult> CreateAsync(NewsDraftRequest request, ClaimsPrincipal principal, HttpContext context, ApplicationDbContext database, TenantContext tenant, CancellationToken cancellationToken)
    {
        var errors = Validate(request.Title, request.Slug, request.Summary, request.Body, request.CoverImageUrl, request.CoverImageAlt, request.Category);
        await ValidateCoverAsync(request.CoverImageUrl, database, errors, cancellationToken);
        if (errors.Count > 0) return Results.ValidationProblem(errors);
        var normalizedSlug = request.Slug.Trim().ToLowerInvariant();
        if (await database.NewsArticles.AnyAsync(article => article.Slug == normalizedSlug, cancellationToken)) return Results.Conflict(new { title = "Slug já utilizado", status = 409 });
        var actor = RequireActor(principal);
        var article = NewsArticle.Create(tenant.RequireMunicipalityId(), request.Title, request.Slug, actor);
        article.UpdateDraft(request.Title, request.Summary, request.Body, request.CoverImageUrl, request.CoverImageAlt, request.Category, request.IsFeatured, actor, DateTimeOffset.UtcNow);
        database.NewsArticles.Add(article);
        AddRevision(database, tenant.RequireMunicipalityId(), article, actor);
        AddAudit(database, tenant, actor, "content.news.created", article.Id, context.TraceIdentifier, new { article.Title, article.Slug, article.Version });
        await database.SaveChangesAsync(cancellationToken);
        return Results.Created($"/api/v1/admin/news/{article.Id}", ToResponse(article));
    }

    private static async Task<IResult> UpdateAsync(Guid id, NewsUpdateRequest request, ClaimsPrincipal principal, HttpContext context, ApplicationDbContext database, TenantContext tenant, CancellationToken cancellationToken)
    {
        var article = await database.NewsArticles.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (article is null) return Results.NotFound();
        if (article.Version != request.ExpectedVersion) return Results.Conflict(new { title = "Notícia alterada por outra pessoa", currentVersion = article.Version, status = 409 });
        var errors = Validate(request.Title, article.Slug, request.Summary, request.Body, request.CoverImageUrl, request.CoverImageAlt, request.Category);
        await ValidateCoverAsync(request.CoverImageUrl, database, errors, cancellationToken);
        if (errors.Count > 0) return Results.ValidationProblem(errors);
        var actor = RequireActor(principal);
        try
        {
            AddRevision(database, tenant.RequireMunicipalityId(), article, actor);
            article.UpdateDraft(request.Title, request.Summary, request.Body, request.CoverImageUrl, request.CoverImageAlt, request.Category, request.IsFeatured, actor, DateTimeOffset.UtcNow);
            AddAudit(database, tenant, actor, "content.news.updated", article.Id, context.TraceIdentifier, new { article.Version });
            await database.SaveChangesAsync(cancellationToken);
            return Results.Ok(ToResponse(article));
        }
        catch (EditorialTransitionException exception) { return Results.Conflict(new { title = "Notícia não editável", detail = exception.Message, status = 409 }); }
    }

    private static Task<IResult> SubmitAsync(Guid id, ClaimsPrincipal principal, HttpContext context, ApplicationDbContext database, TenantContext tenant, CancellationToken cancellationToken) => TransitionAsync(id, "content.news.submitted", principal, context, database, tenant, (article, actor, at) => article.SubmitForReview(actor, at), false, cancellationToken);
    private static Task<IResult> ApproveAsync(Guid id, ClaimsPrincipal principal, HttpContext context, ApplicationDbContext database, TenantContext tenant, CancellationToken cancellationToken) => TransitionAsync(id, "content.news.approved", principal, context, database, tenant, (article, actor, at) => article.Approve(actor, at), false, cancellationToken);
    private static Task<IResult> PublishAsync(Guid id, ClaimsPrincipal principal, HttpContext context, ApplicationDbContext database, TenantContext tenant, CancellationToken cancellationToken) => TransitionAsync(id, "content.news.published", principal, context, database, tenant, (article, actor, at) => article.Publish(actor, at), true, cancellationToken);

    private static async Task<IResult> ScheduleAsync(Guid id, ScheduleRequest request, ClaimsPrincipal principal, HttpContext context, ApplicationDbContext database, TenantContext tenant, CancellationToken cancellationToken)
    {
        var article = await database.NewsArticles.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (article is null) return Results.NotFound();
        var actor = RequireActor(principal);
        try
        {
            AddRevision(database, tenant.RequireMunicipalityId(), article, actor);
            article.Schedule(request.PublishAt, actor, DateTimeOffset.UtcNow);
            database.OutboxMessages.Add(new OutboxMessage(tenant.RequireMunicipalityId(), "content.news.scheduled", JsonSerializer.Serialize(new { article.Id, article.Slug, request.PublishAt })));
            AddAudit(database, tenant, actor, "content.news.scheduled", article.Id, context.TraceIdentifier, new { request.PublishAt, article.Version });
            await database.SaveChangesAsync(cancellationToken);
            return Results.Ok(ToResponse(article));
        }
        catch (Exception exception) when (exception is EditorialTransitionException or ArgumentOutOfRangeException) { return Results.Conflict(new { title = "Agendamento inválido", detail = exception.Message, status = 409 }); }
    }

    private static async Task<IResult> TransitionAsync(Guid id, string action, ClaimsPrincipal principal, HttpContext context, ApplicationDbContext database, TenantContext tenant, Action<NewsArticle, Guid, DateTimeOffset> transition, bool enqueuePublished, CancellationToken cancellationToken)
    {
        var article = await database.NewsArticles.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (article is null) return Results.NotFound();
        var actor = RequireActor(principal);
        try
        {
            AddRevision(database, tenant.RequireMunicipalityId(), article, actor);
            transition(article, actor, DateTimeOffset.UtcNow);
            if (enqueuePublished) database.OutboxMessages.Add(new OutboxMessage(tenant.RequireMunicipalityId(), "content.news.published", JsonSerializer.Serialize(new { article.Id, article.Slug, article.Version })));
            AddAudit(database, tenant, actor, action, article.Id, context.TraceIdentifier, new { status = ToWireStatus(article.Status), article.Version });
            await database.SaveChangesAsync(cancellationToken);
            return Results.Ok(ToResponse(article));
        }
        catch (EditorialTransitionException exception) { return Results.Conflict(new { title = "Transição editorial inválida", detail = exception.Message, status = 409 }); }
    }

    private static async Task<IResult> RevisionsAsync(Guid id, ApplicationDbContext database, CancellationToken cancellationToken)
    {
        var revisions = await database.ContentRevisions.AsNoTracking().Where(item => item.ResourceKind == "NEWS" && item.ResourceId == id).OrderByDescending(item => item.CreatedAt).Select(item => new { item.Id, item.Version, item.SnapshotJson, item.CreatedBy, item.CreatedAt }).ToListAsync(cancellationToken);
        return Results.Ok(revisions);
    }

    private static Dictionary<string, string[]> Validate(string title, string slug, string summary, string body, string? coverImageUrl, string? coverImageAlt, string? category)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length > 180) errors["title"] = ["Informe um título com até 180 caracteres."];
        if (string.IsNullOrWhiteSpace(slug) || slug.Any(character => !char.IsAsciiLetterOrDigit(character) && character != '-')) errors["slug"] = ["Use somente letras sem acento, números e hífen no slug."];
        if (string.IsNullOrWhiteSpace(summary) || summary.Trim().Length > 320) errors["summary"] = ["Informe uma linha fina com até 320 caracteres."];
        if (string.IsNullOrWhiteSpace(body) || body.Trim().Length > 100_000) errors["body"] = ["Informe o conteúdo da notícia."];
        if (!string.IsNullOrWhiteSpace(coverImageUrl) && string.IsNullOrWhiteSpace(coverImageAlt)) errors["coverImageAlt"] = ["Texto alternativo é obrigatório para imagem de capa."];
        if (!string.IsNullOrWhiteSpace(category) && (category.Trim().Length > 80 || category.Trim().Any(character => !char.IsAsciiLetterOrDigit(character) && character != '_'))) errors["category"] = ["Use somente letras sem acento, números e sublinhado na categoria."];
        return errors;
    }

    private static async Task ValidateCoverAsync(string? coverImageUrl, ApplicationDbContext database, Dictionary<string, string[]> errors, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(coverImageUrl)) return;
        const string prefix = "/api/v1/media/";
        if (!coverImageUrl.StartsWith(prefix, StringComparison.Ordinal)
            || !Guid.TryParseExact(coverImageUrl[prefix.Length..], "D", out var mediaId))
        {
            errors["coverImageUrl"] = ["Selecione uma imagem aprovada da biblioteca de mídia."];
            return;
        }

        var approvedImageExists = await database.MediaAssets.AsNoTracking().AnyAsync(
            asset => asset.Id == mediaId && asset.Status == "APPROVED" && asset.MimeType.StartsWith("image/"),
            cancellationToken);
        if (!approvedImageExists) errors["coverImageUrl"] = ["A capa deve referenciar uma imagem aprovada da biblioteca de mídia."];
    }

    private static Guid RequireActor(ClaimsPrincipal principal) => Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var actor) ? actor : throw new InvalidOperationException("Sessão sem identificador de usuário.");
    private static void AddAudit(ApplicationDbContext database, TenantContext tenant, Guid actor, string action, Guid id, string correlationId, object diff) => database.AuditEvents.Add(new AuditEvent(tenant.RequireMunicipalityId(), actor, action, "NewsArticle", id.ToString(), JsonSerializer.Serialize(diff), correlationId));
    private static void AddRevision(ApplicationDbContext database, Guid municipalityId, NewsArticle article, Guid actor) => database.ContentRevisions.Add(new ContentRevision(municipalityId, "NEWS", article.Id, article.Version, JsonSerializer.Serialize(ToResponse(article)), actor));
    private static object ToResponse(NewsArticle article) => new { article.Id, article.Title, article.Slug, article.Summary, article.Body, article.CoverImageUrl, article.CoverImageAlt, article.Category, Status = ToWireStatus(article.Status), article.Version, article.IsFeatured, article.UpdatedAt, article.ScheduledFor, article.PublishedAt };
    private static string ToWireStatus(EditorialStatus status) => status switch { EditorialStatus.Draft => "DRAFT", EditorialStatus.InReview => "IN_REVIEW", EditorialStatus.Approved => "APPROVED", EditorialStatus.Scheduled => "SCHEDULED", EditorialStatus.Published => "PUBLISHED", EditorialStatus.Archived => "ARCHIVED", _ => throw new ArgumentOutOfRangeException(nameof(status)) };

    public sealed record NewsDraftRequest(string Title, string Slug, string Summary, string Body, string? CoverImageUrl, string? CoverImageAlt, string? Category, bool IsFeatured);
    public sealed record NewsUpdateRequest(string Title, string Summary, string Body, string? CoverImageUrl, string? CoverImageAlt, string? Category, bool IsFeatured, int ExpectedVersion);
    public sealed record ScheduleRequest(DateTimeOffset PublishAt);
}
