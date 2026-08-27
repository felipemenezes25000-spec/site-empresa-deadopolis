using Microsoft.EntityFrameworkCore;
using MunicipalPlatform.Api.Infrastructure.Persistence;

namespace MunicipalPlatform.Api.Modules.Administration;

public static class ContentGovernanceEndpoints
{
    public static IEndpointRouteBuilder MapContentGovernanceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/admin/content-governance")
            .RequireAuthorization(policy => policy.RequireClaim("capability", "resources.manage"))
            .WithTags("Admin", "Content Governance");
        group.MapGet("/calendar", CalendarAsync);
        group.MapGet("/stale", StaleAsync);
        return endpoints;
    }

    private static async Task<IResult> CalendarAsync(DateTimeOffset? from, DateTimeOffset? to, ApplicationDbContext database, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var rangeStart = from ?? now.AddDays(-30);
        var rangeEnd = to ?? now.AddDays(180);
        if (rangeEnd <= rangeStart || rangeEnd - rangeStart > TimeSpan.FromDays(730))
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["range"] = ["Intervalo inválido. Use até 730 dias e uma data final posterior à inicial."] });

        var news = await database.NewsArticles.AsNoTracking()
            .Where(item => (item.ScheduledFor.HasValue && item.ScheduledFor >= rangeStart && item.ScheduledFor <= rangeEnd)
                || (item.PublishedAt.HasValue && item.PublishedAt >= rangeStart && item.PublishedAt <= rangeEnd))
            .Select(item => new { item.Id, item.Title, item.Slug, item.Status, item.ScheduledFor, item.PublishedAt })
            .ToListAsync(cancellationToken);

        var resources = await database.PortalResources.AsNoTracking()
            .Where(item => (item.StartsAt.HasValue && item.StartsAt >= rangeStart && item.StartsAt <= rangeEnd)
                || (item.PublishedAt.HasValue && item.PublishedAt >= rangeStart && item.PublishedAt <= rangeEnd)
                || (item.EndsAt.HasValue && item.EndsAt >= rangeStart && item.EndsAt <= rangeEnd))
            .Select(item => new { item.Id, item.Kind, item.Title, item.Slug, item.Status, item.StartsAt, item.EndsAt, item.PublishedAt })
            .ToListAsync(cancellationToken);

        var fromDate = DateOnly.FromDateTime(rangeStart.UtcDateTime);
        var toDate = DateOnly.FromDateTime(rangeEnd.UtcDateTime);
        var gazettes = await database.GazetteEditions.AsNoTracking()
            .Where(item => item.PublicationDate >= fromDate && item.PublicationDate <= toDate)
            .Select(item => new { item.Id, item.Number, item.Year, item.Type, item.Status, item.PublicationDate, item.PublishedAt })
            .ToListAsync(cancellationToken);

        var items = new List<CalendarItem>();
        items.AddRange(news.Select(item => new CalendarItem(
            item.Id,
            "NEWS",
            item.Title,
            item.Status.ToString(),
            item.ScheduledFor ?? item.PublishedAt ?? now,
            item.PublishedAt,
            null,
            $"/noticias/{item.Slug}",
            "/admin/comunicacao")));
        items.AddRange(resources.Select(item => new CalendarItem(
            item.Id,
            $"RESOURCE:{item.Kind}",
            item.Title,
            item.Status,
            item.StartsAt ?? item.PublishedAt ?? item.EndsAt ?? now,
            item.PublishedAt,
            item.EndsAt,
            item.Kind == "PAGE" ? $"/{item.Slug}" : null,
            "/admin/conteudo")));
        items.AddRange(gazettes.Select(item => new CalendarItem(
            item.Id,
            "GAZETTE",
            $"Diário Oficial {item.Number}/{item.Year}",
            item.Status.ToString(),
            new DateTimeOffset(item.PublicationDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero),
            item.PublishedAt,
            null,
            "/diario-oficial",
            "/admin/diario")));

        return Results.Ok(new
        {
            from = rangeStart,
            to = rangeEnd,
            items = items.OrderBy(item => item.ActionAt).ThenBy(item => item.Title, StringComparer.OrdinalIgnoreCase)
        });
    }

    private static async Task<IResult> StaleAsync(int? days, ApplicationDbContext database, CancellationToken cancellationToken)
    {
        var thresholdDays = days ?? 180;
        if (thresholdDays is < 30 or > 730)
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["days"] = ["Use um limite entre 30 e 730 dias."] });
        var now = DateTimeOffset.UtcNow;
        var cutoff = now.AddDays(-thresholdDays);

        var resources = await database.PortalResources.AsNoTracking()
            .Where(item => item.Status != "ARCHIVED" && item.LastReviewedAt < cutoff)
            .Select(item => new { item.Id, item.Kind, item.Title, item.Slug, item.Status, item.LastReviewedAt, item.UpdatedAt, item.UpdatedBy })
            .ToListAsync(cancellationToken);
        var services = await database.Services.AsNoTracking()
            .Where(item => item.Status == "PUBLISHED" && item.LastReviewedAt < cutoff)
            .Select(item => new { item.Id, item.Name, item.Slug, item.Status, item.LastReviewedAt })
            .ToListAsync(cancellationToken);

        var ownerIds = resources.Select(item => item.UpdatedBy).Distinct().ToArray();
        var owners = await database.Users.AsNoTracking()
            .Where(user => ownerIds.Contains(user.Id))
            .Select(user => new { user.Id, user.DisplayName })
            .ToDictionaryAsync(user => user.Id, user => user.DisplayName, cancellationToken);

        var items = resources.Select(item => new StaleItem(
                item.Id,
                $"RESOURCE:{item.Kind}",
                item.Title,
                item.Slug,
                item.Status,
                item.LastReviewedAt == default ? item.UpdatedAt : item.LastReviewedAt,
                item.UpdatedBy,
                owners.GetValueOrDefault(item.UpdatedBy)))
            .Concat(services.Select(item => new StaleItem(
                item.Id,
                "SERVICE",
                item.Name,
                item.Slug,
                item.Status,
                item.LastReviewedAt,
                null,
                null)))
            .OrderBy(item => item.LastReviewedAt)
            .ToList();

        return Results.Ok(new
        {
            thresholdDays,
            cutoff,
            count = items.Count,
            unassigned = items.Count(item => string.IsNullOrWhiteSpace(item.OwnerName)),
            items = items.Select(item => new
            {
                item.Id,
                item.Kind,
                item.Title,
                item.Slug,
                item.Status,
                item.LastReviewedAt,
                item.OwnerId,
                item.OwnerName,
                daysSinceReview = Math.Max(0, (int)Math.Floor((now - item.LastReviewedAt).TotalDays))
            })
        });
    }

    private sealed record CalendarItem(Guid Id, string Kind, string Title, string Status, DateTimeOffset ActionAt, DateTimeOffset? PublishedAt, DateTimeOffset? EndsAt, string? PublicUrl, string AdminUrl);
    private sealed record StaleItem(Guid Id, string Kind, string Title, string Slug, string Status, DateTimeOffset LastReviewedAt, Guid? OwnerId, string? OwnerName);
}
