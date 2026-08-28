using Microsoft.EntityFrameworkCore;
using MunicipalPlatform.Api.Infrastructure.Persistence;
using MunicipalPlatform.Api.Modules.Content.Domain;
using MunicipalPlatform.Api.Modules.Gazette.Domain;
using MunicipalPlatform.Api.Modules.Search.Domain;
using MunicipalPlatform.Api.Modules.Transparency.Domain;

namespace MunicipalPlatform.Api.Modules.Search;

public static class SearchEnhancementEndpoints
{
    public static IEndpointRouteBuilder MapSearchEnhancementEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/search/v2", SearchAsync)
            .AllowAnonymous()
            .WithName("RankedUniversalSearch")
            .WithTags("Search");
        endpoints.MapGet("/api/v1/search/suggest", SuggestAsync)
            .AllowAnonymous()
            .WithName("SearchSuggestions")
            .WithTags("Search");
        return endpoints;
    }

    private static async Task<IResult> SearchAsync(string? q, ApplicationDbContext database, CancellationToken cancellationToken)
    {
        var validation = ValidateQuery(q);
        if (validation is not null)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["q"] = [validation] });
        }

        var requestedQuery = q!.Trim();
        var candidates = await LoadCandidatesAsync(database, compact: false, cancellationToken);
        var ranked = Rank(requestedQuery, candidates, 40);
        return Results.Ok(new
        {
            query = requestedQuery,
            usedFuzzy = ranked.Length > 0 && !ranked[0].DirectMatch,
            results = ranked.Select(item => new
            {
                type = item.Candidate.Type,
                title = item.Candidate.Title,
                description = item.Candidate.Description,
                url = item.Candidate.Url,
                score = item.Score
            })
        });
    }

    private static async Task<IResult> SuggestAsync(string? q, ApplicationDbContext database, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
        {
            return Results.Ok(new { query = q?.Trim() ?? string.Empty, suggestions = Array.Empty<object>() });
        }

        if (q.Trim().Length > 120)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["q"] = ["O termo de busca deve possuir até 120 caracteres."] });
        }

        var requestedQuery = q.Trim();
        var candidates = await LoadCandidatesAsync(database, compact: true, cancellationToken);
        var ranked = Rank(requestedQuery, candidates, 8);
        return Results.Ok(new
        {
            query = requestedQuery,
            suggestions = ranked.Select(item => new
            {
                type = item.Candidate.Type,
                title = item.Candidate.Title,
                url = item.Candidate.Url,
                score = item.Score
            })
        });
    }

    private static string? ValidateQuery(string? query)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2) return "Digite ao menos dois caracteres.";
        return query.Trim().Length > 120 ? "O termo de busca deve possuir até 120 caracteres." : null;
    }

    private static RankedCandidate[] Rank(string query, IReadOnlyCollection<SearchCandidate> candidates, int limit)
    {
        return candidates
            .Select(candidate =>
            {
                var rankingText = $"{candidate.Description} {TypeLabel(candidate.Type)}";
                return new RankedCandidate(
                    candidate,
                    SearchNormalizer.Score(query, candidate.Title, rankingText),
                    SearchNormalizer.IsDirectMatch(query, candidate.Title, rankingText));
            })
            .Where(item => item.Score >= 160)
            .OrderByDescending(item => item.Score)
            .ThenByDescending(item => item.Candidate.Priority)
            .ThenBy(item => SearchNormalizer.Normalize(item.Candidate.Title), StringComparer.Ordinal)
            .GroupBy(item => item.Candidate.Url, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .Take(limit)
            .ToArray();
    }

    private static async Task<List<SearchCandidate>> LoadCandidatesAsync(ApplicationDbContext database, bool compact, CancellationToken cancellationToken)
    {
        var primaryLimit = compact ? 45 : 140;
        var candidates = new List<SearchCandidate>(compact ? 220 : 800);

        var services = await database.Services.AsNoTracking()
            .Where(item => item.Status == "PUBLISHED")
            .OrderByDescending(item => item.IsFeatured)
            .ThenBy(item => item.Name)
            .Take(primaryLimit)
            .Select(item => new { item.Name, item.Slug, item.Description, item.Area })
            .ToListAsync(cancellationToken);
        candidates.AddRange(services.Select(item => new SearchCandidate("SERVICE", item.Name, $"{item.Area} · {item.Description}", $"/servicos/{item.Slug}", 70)));

        var news = await database.NewsArticles.AsNoTracking()
            .Where(item => item.Status == EditorialStatus.Published)
            .OrderByDescending(item => item.PublishedAt)
            .Take(primaryLimit)
            .Select(item => new { item.Title, item.Slug, item.Summary, item.Category })
            .ToListAsync(cancellationToken);
        candidates.AddRange(news.Select(item => new SearchCandidate("NEWS", item.Title, $"{item.Category} · {item.Summary}", $"/noticias/{item.Slug}", 30)));

        var departments = await database.Departments.AsNoTracking()
            .Where(item => item.IsActive)
            .OrderBy(item => item.DisplayOrder)
            .ThenBy(item => item.Name)
            .Take(primaryLimit)
            .Select(item => new { item.Name, item.Slug, item.Acronym, item.ManagerName })
            .ToListAsync(cancellationToken);
        candidates.AddRange(departments.Select(item => new SearchCandidate("DEPARTMENT", item.Name, string.IsNullOrWhiteSpace(item.Acronym) ? $"Secretaria municipal · {item.ManagerName}" : $"{item.Acronym} · {item.ManagerName}", $"/secretarias/{item.Slug}", 60)));

        var datasets = await database.Datasets.AsNoTracking()
            .Where(item => item.Status == DatasetStatus.Published)
            .OrderByDescending(item => item.LastUpdatedAt)
            .Take(primaryLimit)
            .Select(item => new { item.Title, item.Slug, item.Description, item.Category, item.ResponsibleDepartment })
            .ToListAsync(cancellationToken);
        candidates.AddRange(datasets.Select(item => new SearchCandidate("DATASET", item.Title, $"{item.Category} · {item.ResponsibleDepartment} · {item.Description}", $"/dados-abertos/{item.Slug}", 45)));

        var now = DateTimeOffset.UtcNow;
        var pages = await database.PortalResources.AsNoTracking()
            .Where(item => item.Status == "PUBLISHED" && item.Kind == "PAGE" && (!item.StartsAt.HasValue || item.StartsAt <= now) && (!item.EndsAt.HasValue || item.EndsAt > now))
            .OrderBy(item => item.DisplayOrder)
            .ThenBy(item => item.Title)
            .Take(primaryLimit)
            .Select(item => new { item.Title, item.Slug, item.Summary })
            .ToListAsync(cancellationToken);
        foreach (var page in pages)
        {
            var url = PublicPageUrl(page.Slug);
            if (url is not null) candidates.Add(new SearchCandidate("PAGE", page.Title, page.Summary, url, 55));
        }

        if (compact) return candidates;

        var documents = await database.PublicDocuments.AsNoTracking()
            .Where(item => item.Status == "PUBLISHED")
            .OrderByDescending(item => item.PublicationDate)
            .ThenBy(item => item.Title)
            .Take(220)
            .Select(item => new { item.Id, item.Title, item.Description, item.Category, item.Subcategory, item.DocumentNumber, item.ProcessNumber, item.ReferencePeriod, item.ResponsibleDepartment })
            .ToListAsync(cancellationToken);
        candidates.AddRange(documents.Select(item => new SearchCandidate(
            "DOCUMENT",
            item.Title,
            string.Join(" · ", new[] { item.Category, item.Subcategory, item.DocumentNumber, item.ProcessNumber, item.ReferencePeriod, item.ResponsibleDepartment, item.Description }.Where(value => !string.IsNullOrWhiteSpace(value))),
            $"/api/v1/public/documents/{item.Id}/download",
            35)));

        var gazette = await database.GazetteEditions.AsNoTracking()
            .Where(item => item.Status == GazetteStatus.Published)
            .OrderByDescending(item => item.PublicationDate)
            .Take(100)
            .Select(item => new { item.Number, item.Year, item.Type, item.PublicationDate, item.VerificationCode })
            .ToListAsync(cancellationToken);
        candidates.AddRange(gazette.Select(item => new SearchCandidate(
            "GAZETTE",
            $"Diário Oficial nº {item.Number}/{item.Year}",
            $"{item.Type} · publicado em {item.PublicationDate:dd/MM/yyyy}",
            string.IsNullOrWhiteSpace(item.VerificationCode) ? "/diario-oficial" : $"/verificar/{item.VerificationCode}",
            50)));

        return candidates;
    }

    private static string TypeLabel(string type) => type switch
    {
        "SERVICE" => "serviço carta de serviços atendimento",
        "NEWS" => "notícia comunicação",
        "DEPARTMENT" => "secretaria órgão prefeitura",
        "PAGE" => "página informação institucional",
        "DATASET" => "dados abertos dataset transparência",
        "DOCUMENT" => "documento arquivo transparência",
        "GAZETTE" => "diário oficial ato publicação",
        _ => type
    };

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

    private sealed record SearchCandidate(string Type, string Title, string Description, string Url, int Priority);
    private sealed record RankedCandidate(SearchCandidate Candidate, int Score, bool DirectMatch);
}
