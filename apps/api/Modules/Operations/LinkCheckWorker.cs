using Microsoft.EntityFrameworkCore;
using MunicipalPlatform.Api.Infrastructure.Persistence;
using MunicipalPlatform.Api.Platform.Tenancy;

namespace MunicipalPlatform.Api.Modules.Operations;

public sealed class LinkCheckWorker(
    IServiceScopeFactory scopeFactory,
    LinkCheckProbeService probeService,
    IConfiguration configuration,
    ILogger<LinkCheckWorker> logger) : BackgroundService
{
    private static readonly Action<ILogger, Exception?> LogWorkerFailure = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(2001, nameof(LinkCheckWorker)),
        "Falha no monitor periódico de links; o ciclo será tentado novamente.");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalMinutes = Math.Clamp(
            configuration.GetValue<int?>("Operations:LinkCheckIntervalMinutes") ?? 15,
            1,
            1440);
        var interval = TimeSpan.FromMinutes(intervalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAllMunicipalitiesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                LogWorkerFailure(logger, exception);
            }

            await Task.Delay(interval, stoppingToken);
        }
    }

    private async Task CheckAllMunicipalitiesAsync(CancellationToken cancellationToken)
    {
        List<(Guid Id, string Slug)> municipalities;
        await using (var scope = scopeFactory.CreateAsyncScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            municipalities = await database.Municipalities
                .AsNoTracking()
                .Select(item => new ValueTuple<Guid, string>(item.Id, item.Slug))
                .ToListAsync(cancellationToken);
        }

        foreach (var (municipalityId, slug) in municipalities)
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var tenant = scope.ServiceProvider.GetRequiredService<TenantContext>();
            tenant.SetMunicipality(municipalityId, slug);
            var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var links = await database.LinkChecks.OrderBy(item => item.CreatedAt).ToListAsync(cancellationToken);
            foreach (var link in links)
            {
                await probeService.CheckAsync(link, cancellationToken);
                await database.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
