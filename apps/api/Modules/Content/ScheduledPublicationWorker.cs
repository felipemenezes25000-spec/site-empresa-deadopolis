using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MunicipalPlatform.Api.Infrastructure.Persistence;
using MunicipalPlatform.Api.Modules.Content.Domain;
using MunicipalPlatform.Api.Modules.Operations.Domain;
using MunicipalPlatform.Api.Platform.Tenancy;

namespace MunicipalPlatform.Api.Modules.Content;

public sealed class ScheduledPublicationWorker(IServiceScopeFactory scopeFactory, ILogger<ScheduledPublicationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await PublishDueAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception) { logger.LogError(exception, "Falha no scheduler editorial; o job será tentado novamente."); }
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }

    private async Task PublishDueAsync(CancellationToken cancellationToken)
    {
        List<(Guid Id, string Slug)> municipalities;
        await using (var scope = scopeFactory.CreateAsyncScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            municipalities = await database.Municipalities.AsNoTracking().Select(item => new ValueTuple<Guid, string>(item.Id, item.Slug)).ToListAsync(cancellationToken);
        }

        foreach (var (municipalityId, slug) in municipalities)
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var tenant = scope.ServiceProvider.GetRequiredService<TenantContext>();
            tenant.SetMunicipality(municipalityId, slug);
            var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var now = DateTimeOffset.UtcNow;
            var due = await database.NewsArticles.Where(item => item.Status == EditorialStatus.Scheduled && item.ScheduledFor <= now).ToListAsync(cancellationToken);
            foreach (var article in due)
            {
                var actor = article.UpdatedBy;
                article.Publish(actor, now);
                database.OutboxMessages.Add(new OutboxMessage(municipalityId, "content.news.published", JsonSerializer.Serialize(new { article.Id, article.Slug, article.Version, scheduled = true })));
                database.AuditEvents.Add(new AuditEvent(municipalityId, actor, "content.news.published.scheduler", "NewsArticle", article.Id.ToString(), JsonSerializer.Serialize(new { article.Slug, article.ScheduledFor, article.PublishedAt }), "scheduler"));
            }
            if (due.Count > 0) await database.SaveChangesAsync(cancellationToken);
        }
    }
}
