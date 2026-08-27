using Microsoft.EntityFrameworkCore;
using MunicipalPlatform.Api.Infrastructure.Persistence;
using MunicipalPlatform.Api.Modules.Content.Domain;
using MunicipalPlatform.Api.Modules.Operations.Domain;
using MunicipalPlatform.Api.Modules.Search.Domain;
using MunicipalPlatform.Api.Platform.Tenancy;

namespace MunicipalPlatform.Api.Modules.Portal;

public static class PortalEndpoints
{
    public static IEndpointRouteBuilder MapPortalEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/portal/home", GetHomeAsync)
            .AllowAnonymous()
            .WithName("GetPortalHome")
            .WithTags("Portal");

        endpoints.MapGet("/api/v1/services", GetServicesAsync)
            .AllowAnonymous()
            .WithName("GetServices")
            .WithTags("Services");

        endpoints.MapGet("/api/v1/services/{slug}", GetServiceAsync)
            .AllowAnonymous()
            .WithName("GetService")
            .WithTags("Services");

        endpoints.MapGet("/api/v1/news", GetNewsAsync)
            .AllowAnonymous()
            .WithName("GetNews")
            .WithTags("News");

        endpoints.MapGet("/api/v1/news/{slug}", GetArticleAsync)
            .AllowAnonymous()
            .WithName("GetArticle")
            .WithTags("News");

        endpoints.MapGet("/api/v1/departments", GetDepartmentsAsync)
            .AllowAnonymous()
            .WithName("GetDepartments")
            .WithTags("Departments");

        endpoints.MapGet("/api/v1/transparency", GetTransparencyAsync)
            .AllowAnonymous()
            .WithName("GetTransparency")
            .WithTags("Transparency");

        endpoints.MapGet("/api/v1/search", SearchAsync)
            .AllowAnonymous()
            .WithName("UniversalSearch")
            .WithTags("Search");

        endpoints.MapGet("/api/v1/gazette", GetGazetteAsync)
            .AllowAnonymous()
            .WithName("GetGazette")
            .WithTags("Gazette");

        endpoints.MapGet("/api/v1/gazette/verify/{code}", VerifyGazetteAsync)
            .AllowAnonymous()
            .WithName("VerifyGazette")
            .WithTags("Gazette");

        endpoints.MapGet("/api/v1/admin/audit", GetAuditAsync)
            .RequireAuthorization(policy => policy.RequireClaim("capability", "audit.read"))
            .WithName("GetAudit")
            .WithTags("Admin", "Audit");

        return endpoints;
    }

    private static async Task<IResult> GetHomeAsync(
        ApplicationDbContext database,
        TenantContext tenant,
        CancellationToken cancellationToken)
    {
        var municipalityId = tenant.RequireMunicipalityId();
        var municipality = await database.Municipalities
            .AsNoTracking()
            .Where(item => item.Id == municipalityId)
            .Select(item => new
            {
                item.Name,
                item.Slug,
                item.StateCode,
                item.PrimaryColor,
                item.LogoObjectKey
            })
            .SingleAsync(cancellationToken);

        var featuredServices = await database.Services
            .AsNoTracking()
            .Where(service => service.Status == "PUBLISHED")
            .OrderByDescending(service => service.IsFeatured)
            .ThenBy(service => service.Name)
            .Take(8)
            .Select(service => new
            {
                service.Name,
                service.Slug,
                service.Description,
                service.Area,
                service.IsOnline,
                service.OnlineUrl
            })
            .ToListAsync(cancellationToken);

        var latestNews = await database.NewsArticles
            .AsNoTracking()
            .Where(article => article.Status == EditorialStatus.Published)
            .OrderByDescending(article => article.PublishedAt)
            .Take(6)
            .Select(article => new
            {
                article.Title,
                article.Slug,
                article.Summary,
                article.CoverImageUrl,
                article.CoverImageAlt,
                article.Category,
                article.IsFeatured,
                article.PublishedAt
            })
            .ToListAsync(cancellationToken);

        var transparencyLinks = await database.TransparencyLinks
            .AsNoTracking()
            .Where(link => link.IsActive)
            .OrderBy(link => link.DisplayOrder)
            .Select(link => new { link.Title, link.Category, link.Url, link.Description })
            .ToListAsync(cancellationToken);

        var integrationRecords = await database.IntegrationStatuses
            .AsNoTracking()
            .OrderBy(status => status.Provider)
            .Select(status => new
            {
                status.Provider,
                status.State,
                status.Message,
                status.LastCheckedAt
            })
            .ToListAsync(cancellationToken);
        var integrations = integrationRecords.Select(status => new
        {
            status.Provider,
            State = ToExternalState(status.State),
            status.Message,
            status.LastCheckedAt
        });

        return Results.Ok(new
        {
            municipality,
            featuredServices,
            latestNews,
            transparencyLinks,
            integrations
        });
    }

    private static async Task<IResult> GetServicesAsync(
        string? query,
        string? area,
        ApplicationDbContext database,
        CancellationToken cancellationToken)
    {
        var services = database.Services.AsNoTracking().Where(service => service.Status == "PUBLISHED");
        if (!string.IsNullOrWhiteSpace(area))
        {
            services = services.Where(service => service.Area == area);
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = $"%{query.Trim()}%";
            services = services.Where(service =>
                EF.Functions.ILike(service.Name, term)
                || EF.Functions.ILike(service.Description, term));
        }

        var result = await services
            .OrderBy(service => service.Name)
            .Select(service => new
            {
                service.Name,
                service.Slug,
                service.Description,
                service.Area,
                service.Audience,
                service.IsOnline,
                service.OnlineUrl,
                service.ExpectedDuration,
                service.Cost
            })
            .ToListAsync(cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetNewsAsync(
        ApplicationDbContext database,
        CancellationToken cancellationToken,
        string? category = null)
    {
        var query = database.NewsArticles
            .AsNoTracking()
            .Where(article => article.Status == EditorialStatus.Published);
        if (!string.IsNullOrWhiteSpace(category))
        {
            var normalizedCategory = category.Trim().ToUpperInvariant();
            query = query.Where(article => article.Category == normalizedCategory);
        }
        var result = await query
            .OrderByDescending(article => article.PublishedAt)
            .Select(article => new
            {
                article.Title,
                article.Slug,
                article.Summary,
                article.Category,
                article.PublishedAt,
                article.CoverImageUrl,
                article.CoverImageAlt
            })
            .ToListAsync(cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetServiceAsync(
        string slug,
        ApplicationDbContext database,
        CancellationToken cancellationToken)
    {
        var normalizedSlug = slug.ToLowerInvariant();
        var service = await database.Services.AsNoTracking()
            .Where(item => item.Slug == normalizedSlug && item.Status == "PUBLISHED")
            .Select(item => new
            {
                item.Name,
                item.Slug,
                item.Description,
                item.Area,
                item.Audience,
                item.Requirements,
                item.Documents,
                item.Steps,
                item.ExpectedDuration,
                item.Cost,
                item.Channels,
                item.IsOnline,
                item.OnlineUrl,
                item.Phone,
                item.Address,
                item.OpeningHours,
                item.LegalBasis,
                item.LastReviewedAt
            })
            .SingleOrDefaultAsync(cancellationToken);
        return service is null ? Results.NotFound() : Results.Ok(service);
    }

    private static async Task<IResult> GetArticleAsync(
        string slug,
        ApplicationDbContext database,
        CancellationToken cancellationToken)
    {
        var normalizedSlug = slug.ToLowerInvariant();
        var article = await database.NewsArticles.AsNoTracking()
            .Where(item => item.Slug == normalizedSlug && item.Status == EditorialStatus.Published)
            .Select(item => new
            {
                item.Title,
                item.Slug,
                item.Summary,
                item.Body,
                item.CoverImageUrl,
                item.CoverImageAlt,
                item.Category,
                item.PublishedAt,
                item.UpdatedAt
            })
            .SingleOrDefaultAsync(cancellationToken);
        return article is null ? Results.NotFound() : Results.Ok(article);
    }

    private static async Task<IResult> GetDepartmentsAsync(ApplicationDbContext database, CancellationToken cancellationToken)
    {
        var items = await database.Departments.AsNoTracking()
            .Where(item => item.IsActive)
            .OrderBy(item => item.DisplayOrder)
            .Select(item => new { item.Name, item.Slug, item.Acronym, item.ManagerName, item.Phone, item.Email, item.Address, item.OpeningHours })
            .ToListAsync(cancellationToken);
        return Results.Ok(items);
    }

    private static async Task<IResult> GetTransparencyAsync(ApplicationDbContext database, CancellationToken cancellationToken)
    {
        var items = await database.TransparencyLinks.AsNoTracking()
            .Where(item => item.IsActive)
            .OrderBy(item => item.Category)
            .ThenBy(item => item.DisplayOrder)
            .Select(item => new { item.Title, item.Category, item.Url, item.Description, item.IsExternal })
            .ToListAsync(cancellationToken);
        return Results.Ok(items);
    }

    private static async Task<IResult> SearchAsync(string? q, ApplicationDbContext database, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["q"] = ["Digite ao menos dois caracteres."] });
        }

        var normalized = SearchNormalizer.Normalize(q);
        var services = await database.Services.AsNoTracking()
            .Where(item => item.Status == "PUBLISHED")
            .OrderBy(item => item.Name)
            .Take(500)
            .Select(item => new { item.Name, item.Slug, item.Description, item.Area })
            .ToListAsync(cancellationToken);
        var news = await database.NewsArticles.AsNoTracking()
            .Where(item => item.Status == EditorialStatus.Published)
            .OrderByDescending(item => item.PublishedAt)
            .Take(500)
            .Select(item => new { item.Title, item.Slug, item.Summary, item.PublishedAt })
            .ToListAsync(cancellationToken);

        var serviceResults = services
            .Where(item => SearchNormalizer.Normalize($"{item.Name} {item.Description} {item.Area}").Contains(normalized, StringComparison.Ordinal))
            .Take(20)
            .Select(item => new { type = "SERVICE", title = item.Name, description = item.Description, url = $"/servicos/{item.Slug}" });
        var newsResults = news
            .Where(item => SearchNormalizer.Normalize($"{item.Title} {item.Summary}").Contains(normalized, StringComparison.Ordinal))
            .Take(20)
            .Select(item => new { type = "NEWS", title = item.Title, description = item.Summary, url = $"/noticias/{item.Slug}" });
        return Results.Ok(new { query = q.Trim(), results = serviceResults.Concat(newsResults) });
    }

    private static async Task<IResult> GetGazetteAsync(ApplicationDbContext database, CancellationToken cancellationToken)
    {
        var editions = await database.GazetteEditions.AsNoTracking()
            .Where(item => item.Status == Modules.Gazette.Domain.GazetteStatus.Published)
            .OrderByDescending(item => item.PublicationDate)
            .Select(item => new { item.Number, item.Year, item.Type, item.PublicationDate, item.VerificationCode, item.Sha256, item.DocumentObjectKey })
            .ToListAsync(cancellationToken);
        return Results.Ok(editions);
    }

    private static async Task<IResult> VerifyGazetteAsync(
        string code,
        ApplicationDbContext database,
        CancellationToken cancellationToken)
    {
        var edition = await database.GazetteEditions
            .AsNoTracking()
            .Where(item => item.VerificationCode == code && item.Status == Modules.Gazette.Domain.GazetteStatus.Published)
            .Select(item => new
            {
                item.Number,
                item.Year,
                item.PublicationDate,
                item.Sha256,
                item.VerificationCode,
                item.CertificateSubject,
                item.CertificateIssuer,
                item.SignedAt,
                Status = "PUBLISHED"
            })
            .SingleOrDefaultAsync(cancellationToken);

        return edition is null ? Results.NotFound() : Results.Ok(edition);
    }

    private static async Task<IResult> GetAuditAsync(
        ApplicationDbContext database,
        CancellationToken cancellationToken)
    {
        var events = await database.AuditEvents
            .AsNoTracking()
            .OrderByDescending(item => item.OccurredAt)
            .Take(100)
            .Select(item => new
            {
                item.ActorId,
                item.Action,
                item.Resource,
                item.ResourceId,
                item.SemanticDiff,
                item.CorrelationId,
                item.OccurredAt
            })
            .ToListAsync(cancellationToken);
        return Results.Ok(events);
    }

    private static string ToExternalState(IntegrationState state) => state switch
    {
        IntegrationState.Configured => "CONFIGURED",
        IntegrationState.Degraded => "DEGRADED",
        IntegrationState.Unavailable => "UNAVAILABLE",
        IntegrationState.NotConfigured => "NOT_CONFIGURED",
        _ => throw new ArgumentOutOfRangeException(nameof(state))
    };
}
