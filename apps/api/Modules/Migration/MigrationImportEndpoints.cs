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
        group.MapPost("/{id:guid}/urls/{legacyUrlId:guid}/import-document", ImportDocumentAsync);
        return endpoints;
    }

    private static async Task<IResult> ImportDocumentAsync(
        Guid id,
        Guid legacyUrlId,
        ImportDocumentRequest request,
        ClaimsPrincipal principal,
        HttpContext context,
        ApplicationDbContext database,
        TenantContext tenant,
        LegacyDocumentImportService importer,
        CancellationToken cancellationToken)
    {
        var job = await database.MigrationJobs.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (job is null) return Results.NotFound();
        var legacyUrl = await database.LegacyUrls.SingleOrDefaultAsync(item => item.Id == legacyUrlId && item.MigrationJobId == id, cancellationToken);
        if (legacyUrl is null) return Results.NotFound();
        var actor = RequireActor(principal);
        try
        {
            var result = await importer.ImportAsync(
                job,
                legacyUrl,
                new LegacyDocumentImportOptions(
                    request.Category,
                    request.Subcategory,
                    request.Title,
                    request.Description,
                    request.DocumentNumber,
                    request.ProcessNumber,
                    request.ReferencePeriod,
                    request.PublicationDate,
                    request.ResponsibleDepartment,
                    request.DocumentType),
                actor,
                database,
                cancellationToken);
            var evidence = new MigrationEvidence(tenant.RequireMunicipalityId(), job.Id, "DOCUMENT_IMPORT", legacyUrl.Url, result.EvidenceJson);
            database.MigrationEvidences.Add(evidence);
            database.AuditEvents.Add(new AuditEvent(
                tenant.RequireMunicipalityId(), actor, "migration.document.imported", "PublicDocument", result.Document.Id.ToString(),
                JsonSerializer.Serialize(new { jobId = job.Id, legacyUrlId, documentId = result.Document.Id, mediaAssetId = result.Asset.Id, result.ReusedAsset, result.Document.Status }),
                context.TraceIdentifier));
            await database.SaveChangesAsync(cancellationToken);
            return Results.Created($"/api/v1/admin/documents/{result.Document.Id}", new
            {
                document = result.Document,
                asset = new { result.Asset.Id, result.Asset.Status, result.Asset.Sha256, result.Asset.MimeType, result.Asset.SizeBytes },
                result.ReusedAsset,
                evidenceId = evidence.Id,
                detail = "Documento criado como rascunho. O acesso público exige asset aprovado e publicação administrativa explícita."
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
            return Results.Conflict(new { title = "Conflito ao persistir documento", detail = "A URL legada ou o contexto documental já foi importado.", status = 409 });
        }
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

    public sealed record ImportDocumentRequest(
        string Category,
        string? Subcategory,
        string Title,
        string? Description,
        string? DocumentNumber,
        string? ProcessNumber,
        string? ReferencePeriod,
        DateOnly? PublicationDate,
        string? ResponsibleDepartment,
        string DocumentType);
}
