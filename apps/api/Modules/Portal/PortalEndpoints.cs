using Microsoft.EntityFrameworkCore;
using MunicipalPlatform.Api.Infrastructure.Persistence;
using MunicipalPlatform.Api.Modules.Content.Domain;
using MunicipalPlatform.Api.Modules.Operations.Domain;
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

        endpoints.MapGet("/api/v1/news", GetNewsAsync)
            .AllowAnonymous()
            .WithName("GetNews")
            .WithTags("News");

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
        CancellationToken cancellationToken)
    {
        var result = await database.NewsArticles
            .AsNoTracking()
            .Where(article => article.Status == EditorialStatus.Published)
            .OrderByDescending(article => article.PublishedAt)
            .Select(article => new
            {
                article.Title,
                article.Slug,
                article.Summary,
                article.PublishedAt,
                article.CoverImageUrl,
                article.CoverImageAlt
            })
            .ToListAsync(cancellationToken);
        return Results.Ok(result);
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
