using Microsoft.EntityFrameworkCore;
using MunicipalPlatform.Api.Infrastructure.Persistence;
using MunicipalPlatform.Api.Modules.Content.Domain;
using MunicipalPlatform.Api.Modules.Operations.Domain;
using MunicipalPlatform.Api.Modules.Search.Domain;
using MunicipalPlatform.Api.Modules.Transparency.Domain;
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
            State = IntegrationStateVocabulary.ToExternalState(status.State),
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

        var requestedQuery = q.Trim();
        var normalizedQuery = SearchNormalizer.Normalize(requestedQuery);
        var databaseTerm = $"%{normalizedQuery}%";
        var useInMemoryPredicates = string.Equals(
            database.Database.ProviderName,
            "Microsoft.EntityFrameworkCore.InMemory",
            StringComparison.Ordinal);

        var serviceQuery = database.Services.AsNoTracking().Where(item => item.Status == "PUBLISHED");
        serviceQuery = useInMemoryPredicates
            ? serviceQuery.Where(item => SearchNormalizer.Normalize(item.Name).Contains(normalizedQuery, StringComparison.Ordinal)
                || SearchNormalizer.Normalize(item.Description).Contains(normalizedQuery, StringComparison.Ordinal)
                || SearchNormalizer.Normalize(item.Area).Contains(normalizedQuery, StringComparison.Ordinal))
            : serviceQuery.Where(item => EF.Functions.ILike(EF.Functions.Unaccent(item.Name), databaseTerm)
                || EF.Functions.ILike(EF.Functions.Unaccent(item.Description), databaseTerm)
                || EF.Functions.ILike(EF.Functions.Unaccent(item.Area), databaseTerm));
        var services = await serviceQuery
            .OrderBy(item => item.Name)
            .Take(10)
            .Select(item => new PortalSearchResult("SERVICE", item.Name, item.Description, $"/servicos/{item.Slug}"))
            .ToListAsync(cancellationToken);
        var newsQuery = database.NewsArticles.AsNoTracking().Where(item => item.Status == EditorialStatus.Published);
        newsQuery = useInMemoryPredicates
            ? newsQuery.Where(item => SearchNormalizer.Normalize(item.Title).Contains(normalizedQuery, StringComparison.Ordinal)
                || SearchNormalizer.Normalize(item.Summary).Contains(normalizedQuery, StringComparison.Ordinal)
                || SearchNormalizer.Normalize(item.Body).Contains(normalizedQuery, StringComparison.Ordinal))
            : newsQuery.Where(item => EF.Functions.ILike(EF.Functions.Unaccent(item.Title), databaseTerm)
                || EF.Functions.ILike(EF.Functions.Unaccent(item.Summary), databaseTerm)
                || EF.Functions.ILike(EF.Functions.Unaccent(item.Body), databaseTerm));
        var news = await newsQuery
            .OrderByDescending(item => item.PublishedAt)
            .Take(10)
            .Select(item => new PortalSearchResult("NEWS", item.Title, item.Summary, $"/noticias/{item.Slug}"))
            .ToListAsync(cancellationToken);
        var departmentQuery = database.Departments.AsNoTracking().Where(item => item.IsActive);
        departmentQuery = useInMemoryPredicates
            ? departmentQuery.Where(item => SearchNormalizer.Normalize(item.Name).Contains(normalizedQuery, StringComparison.Ordinal)
                || SearchNormalizer.Normalize(item.Acronym).Contains(normalizedQuery, StringComparison.Ordinal))
            : departmentQuery.Where(item => EF.Functions.ILike(EF.Functions.Unaccent(item.Name), databaseTerm)
                || EF.Functions.ILike(EF.Functions.Unaccent(item.Acronym), databaseTerm));
        var departments = await departmentQuery
            .OrderBy(item => item.DisplayOrder)
            .ThenBy(item => item.Name)
            .Take(10)
            .Select(item => new PortalSearchResult("DEPARTMENT", item.Name, string.IsNullOrWhiteSpace(item.Acronym) ? "Secretaria municipal" : item.Acronym, $"/secretarias/{item.Slug}"))
            .ToListAsync(cancellationToken);
        var datasetQuery = database.Datasets.AsNoTracking().Where(item => item.Status == DatasetStatus.Published);
        datasetQuery = useInMemoryPredicates
            ? datasetQuery.Where(item => SearchNormalizer.Normalize(item.Title).Contains(normalizedQuery, StringComparison.Ordinal)
                || SearchNormalizer.Normalize(item.Description).Contains(normalizedQuery, StringComparison.Ordinal)
                || SearchNormalizer.Normalize(item.Category).Contains(normalizedQuery, StringComparison.Ordinal)
                || SearchNormalizer.Normalize(item.ResponsibleDepartment).Contains(normalizedQuery, StringComparison.Ordinal))
            : datasetQuery.Where(item => EF.Functions.ILike(EF.Functions.Unaccent(item.Title), databaseTerm)
                || EF.Functions.ILike(EF.Functions.Unaccent(item.Description), databaseTerm)
                || EF.Functions.ILike(EF.Functions.Unaccent(item.Category), databaseTerm)
                || EF.Functions.ILike(EF.Functions.Unaccent(item.ResponsibleDepartment), databaseTerm));
        var datasets = await datasetQuery
            .OrderByDescending(item => item.LastUpdatedAt)
            .Take(10)
            .Select(item => new PortalSearchResult("DATASET", item.Title, item.Description, $"/dados-abertos/{item.Slug}"))
            .ToListAsync(cancellationToken);
        var documentQuery = database.PublicDocuments.AsNoTracking().Where(item => item.Status == "PUBLISHED");
        documentQuery = useInMemoryPredicates
            ? documentQuery.Where(item => SearchNormalizer.Normalize(item.Title).Contains(normalizedQuery, StringComparison.Ordinal)
                || SearchNormalizer.Normalize(item.Description).Contains(normalizedQuery, StringComparison.Ordinal)
                || SearchNormalizer.Normalize(item.DocumentNumber).Contains(normalizedQuery, StringComparison.Ordinal)
                || SearchNormalizer.Normalize(item.ProcessNumber).Contains(normalizedQuery, StringComparison.Ordinal)
                || SearchNormalizer.Normalize(item.ReferencePeriod).Contains(normalizedQuery, StringComparison.Ordinal)
                || SearchNormalizer.Normalize(item.ResponsibleDepartment).Contains(normalizedQuery, StringComparison.Ordinal)
                || SearchNormalizer.Normalize(item.Category).Contains(normalizedQuery, StringComparison.Ordinal)
                || SearchNormalizer.Normalize(item.Subcategory).Contains(normalizedQuery, StringComparison.Ordinal))
            : documentQuery.Where(item => EF.Functions.ILike(EF.Functions.Unaccent(item.Title), databaseTerm)
                || EF.Functions.ILike(EF.Functions.Unaccent(item.Description), databaseTerm)
                || EF.Functions.ILike(EF.Functions.Unaccent(item.DocumentNumber), databaseTerm)
                || EF.Functions.ILike(EF.Functions.Unaccent(item.ProcessNumber), databaseTerm)
                || EF.Functions.ILike(EF.Functions.Unaccent(item.ReferencePeriod), databaseTerm)
                || EF.Functions.ILike(EF.Functions.Unaccent(item.ResponsibleDepartment), databaseTerm)
                || EF.Functions.ILike(EF.Functions.Unaccent(item.Category), databaseTerm)
                || EF.Functions.ILike(EF.Functions.Unaccent(item.Subcategory), databaseTerm));
        var documents = await documentQuery
            .OrderByDescending(item => item.PublicationDate)
            .ThenBy(item => item.Title)
            .Take(10)
            .Select(item => new PortalSearchResult(
                "DOCUMENT",
                item.Title,
                string.IsNullOrWhiteSpace(item.Description) ? item.Category : item.Description,
                $"/api/v1/public/documents/{item.Id}/download"))
            .ToListAsync(cancellationToken);
        var pageQuery = database.PortalResources.AsNoTracking().Where(item => item.Status == "PUBLISHED" && item.Kind == "PAGE" && PublicPageSlugs.Contains(item.Slug));
        pageQuery = useInMemoryPredicates
            ? pageQuery.Where(item => SearchNormalizer.Normalize(item.Title).Contains(normalizedQuery, StringComparison.Ordinal)
                || SearchNormalizer.Normalize(item.Summary).Contains(normalizedQuery, StringComparison.Ordinal))
            : pageQuery.Where(item => EF.Functions.ILike(EF.Functions.Unaccent(item.Title), databaseTerm)
                || EF.Functions.ILike(EF.Functions.Unaccent(item.Summary), databaseTerm));
        var pageCandidates = await pageQuery
            .OrderBy(item => item.DisplayOrder)
            .ThenBy(item => item.Title)
            .Take(10)
            .Select(item => new { item.Slug, item.Title, item.Summary })
            .ToListAsync(cancellationToken);
        var pages = pageCandidates
            .Select(item => new PortalSearchResult("PAGE", item.Title, item.Summary, PublicPageUrl(item.Slug)))
            .Where(item => item.Url is not null)
            .Select(item => item with { Url = item.Url! });

        var results = services
            .Concat(news)
            .Concat(departments)
            .Concat(pages)
            .Concat(datasets)
            .Concat(documents)
            .Take(60)
            .ToArray();
        return Results.Ok(new { query = requestedQuery, results });
    }

    private static string? PublicPageUrl(string slug) => slug switch
    {
        "acesso-a-informacao" => "/acesso-a-informacao",
        "calendario-licitacoes" => "/licitacoes/calendario",
        "conselhos" => "/conselhos",
        "esic-estatisticas" => "/acesso-a-informacao/estatisticas",
        "esic-perguntas-frequentes" => "/acesso-a-informacao/perguntas",
        "gestao" => "/municipio/gestao",
        "municipio" => "/municipio",
        "obras" => "/obras",
        "prefeito" => "/governo/prefeito",
        "privacidade" => "/privacidade",
        "vice-prefeito" => "/governo/vice-prefeito",
        _ => null
    };

    private static readonly string[] PublicPageSlugs =
    [
        "acesso-a-informacao",
        "calendario-licitacoes",
        "conselhos",
        "esic-estatisticas",
        "esic-perguntas-frequentes",
        "gestao",
        "municipio",
        "obras",
        "prefeito",
        "privacidade",
        "vice-prefeito"
    ];

    private sealed record PortalSearchResult(string Type, string Title, string Description, string? Url);

    private static async Task<IResult> GetGazetteAsync(ApplicationDbContext database, CancellationToken cancellationToken)
    {
        var editions = await database.GazetteEditions.AsNoTracking()
            .Where(item => item.Status == Modules.Gazette.Domain.GazetteStatus.Published)
            .OrderByDescending(item => item.PublicationDate)
            // Id e TypeName são adicionados sem remover nada: o portal precisa do identificador para
            // oferecer o PDF que o hash descreve, e do nome do tipo para não exibir o ordinal do enum.
            .Select(item => new { item.Id, item.Number, item.Year, item.Type, TypeName = item.Type.ToString(), item.PublicationDate, item.VerificationCode, item.Sha256, item.DocumentObjectKey })
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
                // O identificador permite ao cidadão baixar exatamente o documento que este hash descreve.
                item.Id,
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

}
