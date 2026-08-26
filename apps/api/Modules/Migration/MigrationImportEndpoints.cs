using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MunicipalPlatform.Api.Infrastructure.Persistence;
using MunicipalPlatform.Api.Modules.Migration.Domain;
using MunicipalPlatform.Api.Modules.Migration.Services;
using MunicipalPlatform.Api.Modules.Operations.Domain;
using MunicipalPlatform.Api.Platform.Tenancy;

namespace MunicipalPlatform.Api.Modules.Migration;

public static class MigrationImportEndpoints
{
    public static IEndpointRouteBuilder MapMigrationImportEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/admin/migration/jobs")
            .WithTags("Admin", "Migration")
            .RequireAuthorization(policy => policy.RequireClaim("capability", "migration.manage"));

        group.MapGet("/{id:guid}/imports", ListImportsAsync);
        group.MapPost("/{id:guid}/urls/{legacyUrlId:guid}/import-page", ImportPageAsync);
        return endpoints;
    }

    private static async Task<IResult> ListImportsAsync(
        Guid id,
        ApplicationDbContext database,
        CancellationToken cancellationToken)
    {
        if (!await database.MigrationJobs.AsNoTracking().AnyAsync(item => item.Id == id, cancellationToken))
            return Results.NotFound();

        var items = await database.ImportedContents.AsNoTracking()
            .Where(item => item.MigrationJobId == id)
            .OrderByDescending(item => item.ImportedAt)
            .ToListAsync(cancellationToken);
        return Results.Ok(items);
    }

    private static async Task<IResult> ImportPageAsync(
        Guid id,
        Guid legacyUrlId,
        ImportPageRequest request,
        ClaimsPrincipal principal,
        HttpContext context,
        ApplicationDbContext database,
        TenantContext tenant,
        LegacyImportService importer,
        CancellationToken cancellationToken)
    {
        var job = await database.MigrationJobs.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (job is null)
            return Results.NotFound();

        var legacyUrl = await database.LegacyUrls
            .SingleOrDefaultAsync(item => item.Id == legacyUrlId && item.MigrationJobId == id, cancellationToken);
        if (legacyUrl is null)
            return Results.NotFound();

        var actor = RequireActor(principal);
        try
        {
            var result = await importer.PreparePageDraftAsync(
                job,
                legacyUrl,
                new LegacyPageImportOptions(
                    request.Slug,
                    request.Title,
                    request.Summary,
                    request.RedirectDestination,
                    request.PermanentRedirect),
                actor,
                database,
                cancellationToken);

            var migrationEvidence = new MigrationEvidence(
                tenant.RequireMunicipalityId(),
                job.Id,
                "PAGE_IMPORT",
                legacyUrl.Url,
                result.EvidenceJson);
            database.MigrationEvidences.Add(migrationEvidence);
            database.AuditEvents.Add(new AuditEvent(
                tenant.RequireMunicipalityId(),
                actor,
                "migration.content.imported",
                "ImportedContent",
                result.ImportedContent.Id.ToString(),
                JsonSerializer.Serialize(new
                {
                    jobId = job.Id,
                    legacyUrlId = legacyUrl.Id,
                    resourceId = result.Resource.Id,
                    resourceSlug = result.Resource.Slug,
                    draftOnly = true,
                    redirectId = result.Redirect?.Id,
                    redirectDestination = result.Redirect?.DestinationPath
                }),
                context.TraceIdentifier));

            await database.SaveChangesAsync(cancellationToken);
            return Results.Created(
                $"/api/v1/admin/resources/{result.Resource.Id}",
                new
                {
                    importedContent = result.ImportedContent,
                    resource = new
                    {
                        result.Resource.Id,
                        result.Resource.Kind,
                        result.Resource.Slug,
                        result.Resource.Title,
                        result.Resource.Summary,
                        result.Resource.Status,
                        result.Resource.Version
                    },
                    redirect = result.Redirect is null
                        ? null
                        : new
                        {
                            result.Redirect.Id,
                            result.Redirect.LegacyPath,
                            result.Redirect.DestinationPath,
                            result.Redirect.StatusCode
                        },
                    evidenceId = migrationEvidence.Id,
                    detail = "Rascunho CMS criado com evidência de integridade. Nenhum conteúdo foi publicado automaticamente."
                });
        }
        catch (LegacyImportConflictException exception)
        {
            return Results.Conflict(new { title = "Importação recusada", detail = exception.Message, status = 409 });
        }
        catch (Exception exception) when (exception is LegacyImportValidationException or ArgumentException or JsonException)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["import"] = [exception.Message] });
        }
        catch (DbUpdateException)
        {
            return Results.Conflict(new
            {
                title = "Conflito ao persistir importação",
                detail = "Outro processo alterou o destino, slug ou redirect durante a importação. Recarregue o inventário antes de tentar novamente.",
                status = 409
            });
        }
    }

    private static Guid RequireActor(ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? id
            : throw new InvalidOperationException("Sessão inválida.");

    public sealed record ImportPageRequest(
        string Slug,
        string? Title,
        string? Summary,
        string? RedirectDestination,
        bool PermanentRedirect);
}
