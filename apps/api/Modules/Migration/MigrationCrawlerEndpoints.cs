using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MunicipalPlatform.Api.Infrastructure.Persistence;
using MunicipalPlatform.Api.Modules.Migration.Services;
using MunicipalPlatform.Api.Modules.Operations.Domain;
using MunicipalPlatform.Api.Platform.Tenancy;

namespace MunicipalPlatform.Api.Modules.Migration;

public static class MigrationCrawlerEndpoints
{
    public static IEndpointRouteBuilder MapMigrationCrawlerEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/admin/migration/jobs/{id:guid}/run-dry-run", RunDryRunAsync)
            .WithTags("Admin", "Migration")
            .RequireAuthorization(p => p.RequireClaim("capability", "migration.manage"));
        return endpoints;
    }

    private static async Task<IResult> RunDryRunAsync(
        Guid id,
        ClaimsPrincipal principal,
        HttpContext context,
        ApplicationDbContext database,
        TenantContext tenant,
        LegacyCrawlerService crawler,
        CancellationToken cancellationToken)
    {
        var job = await database.MigrationJobs.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (job is null)
            return Results.NotFound();

        var actor = RequireActor(principal);
        database.AuditEvents.Add(new AuditEvent(
            tenant.RequireMunicipalityId(),
            actor,
            "migration.dryrun.started",
            "MigrationJob",
            job.Id.ToString(),
            JsonSerializer.Serialize(new { job.SourceBaseUrl, job.AllowedHost, job.MaxDepth, job.MaxPages }),
            context.TraceIdentifier));
        await database.SaveChangesAsync(cancellationToken);

        var summary = await crawler.RunDryRunAsync(job, database, cancellationToken);
        database.AuditEvents.Add(new AuditEvent(
            tenant.RequireMunicipalityId(),
            actor,
            "migration.dryrun.completed",
            "MigrationJob",
            job.Id.ToString(),
            JsonSerializer.Serialize(summary),
            context.TraceIdentifier));
        await database.SaveChangesAsync(cancellationToken);
        return Results.Ok(new { job.Id, job.State, summary });
    }

    private static Guid RequireActor(ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? id
            : throw new InvalidOperationException("Sessão inválida.");
}
