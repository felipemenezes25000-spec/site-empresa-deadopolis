using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MunicipalPlatform.Api.Infrastructure.Persistence;
using MunicipalPlatform.Api.Modules.Content.Domain;
using MunicipalPlatform.Api.Modules.Migration.Domain;
using MunicipalPlatform.Api.Modules.Migration.Security;

namespace MunicipalPlatform.Api.Modules.Migration.Services;

public sealed class LegacyImportService(ILegacySourceFetcher sourceFetcher)
{
    public async Task<LegacyPageImportResult> PreparePageDraftAsync(
        MigrationJob job,
        LegacyUrl legacyUrl,
        LegacyPageImportOptions options,
        Guid actorId,
        ApplicationDbContext database,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(legacyUrl);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(database);

        if (actorId == Guid.Empty)
            throw new LegacyImportValidationException("Ator responsável pela importação é obrigatório.");
        if (legacyUrl.MigrationJobId != job.Id || legacyUrl.MunicipalityId != job.MunicipalityId)
            throw new LegacyImportValidationException("A URL legada não pertence ao job informado.");
        if (job.State != MigrationJobState.DryRun)
            throw new LegacyImportConflictException("O inventário precisa concluir o dry-run antes da importação.");
        if (!string.Equals(legacyUrl.State, "MAPPED", StringComparison.OrdinalIgnoreCase)
            || !string.Equals(legacyUrl.Classification, "MIGRATE", StringComparison.OrdinalIgnoreCase))
            throw new LegacyImportValidationException("Somente URLs mapeadas e classificadas para migração podem gerar rascunho CMS.");
        if (string.IsNullOrWhiteSpace(legacyUrl.Sha256))
            throw new LegacyImportValidationException("A URL não possui SHA-256 do dry-run e não pode ser importada com integridade verificável.");
        if (!Uri.TryCreate(legacyUrl.Url, UriKind.Absolute, out var sourceUri)
            || !ExternalUrlSafety.IsAllowedUri(sourceUri, job.AllowedHost))
            throw new LegacyImportValidationException("A URL de origem não atende à política SSRF do job.");

        var existingImport = await database.ImportedContents.AsNoTracking()
            .SingleOrDefaultAsync(item => item.LegacyUrlId == legacyUrl.Id, cancellationToken);
        if (existingImport is not null)
            throw new LegacyImportConflictException($"Esta URL já foi importada para {existingImport.TargetType} {existingImport.TargetReference}.");

        var slug = NormalizeSlug(options.Slug);
        if (await database.PortalResources.AnyAsync(item => item.Kind == "PAGE" && item.Slug == slug, cancellationToken))
            throw new LegacyImportConflictException("Já existe uma página CMS com o slug informado.");

        RedirectRule? redirect = null;
        if (!string.IsNullOrWhiteSpace(options.RedirectDestination))
        {
            var destination = options.RedirectDestination.Trim();
            if (!RedirectRule.IsInternalDestination(destination))
                throw new LegacyImportValidationException("O destino do redirect deve ser um caminho interno iniciado por '/' e nunca pode apontar para outro host.");
            if (string.Equals(legacyUrl.NormalizedPath, destination, StringComparison.OrdinalIgnoreCase))
                throw new LegacyImportValidationException("O redirect não pode apontar para a própria URL legada.");
            if (await database.RedirectRules.AnyAsync(item => item.LegacyPath == legacyUrl.NormalizedPath, cancellationToken))
                throw new LegacyImportConflictException("A URL legada já possui uma regra de redirect registrada.");
            redirect = new RedirectRule(job.MunicipalityId, legacyUrl.NormalizedPath, destination, options.PermanentRedirect);
        }

        var fetched = await sourceFetcher.FetchAsync(sourceUri, job.AllowedHost, cancellationToken);
        if (fetched.RedirectLocation is not null)
            throw new LegacyImportConflictException("A origem passou a responder com redirect depois do dry-run. Execute um novo inventário antes de importar.");
        if (fetched.StatusCode is < 200 or > 299)
            throw new LegacyImportConflictException($"A origem respondeu HTTP {fetched.StatusCode} depois do dry-run. Execute um novo inventário antes de importar.");
        if (!string.Equals(fetched.ContentType, "text/html", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(fetched.ContentType, "application/xhtml+xml", StringComparison.OrdinalIgnoreCase))
            throw new LegacyImportValidationException("A importação para PAGE aceita somente conteúdo HTML. Documentos e mídias devem seguir o fluxo de mídia.");
        if (fetched.Body.Length == 0)
            throw new LegacyImportValidationException("A origem retornou conteúdo vazio.");

        var actualSha256 = Convert.ToHexString(SHA256.HashData(fetched.Body)).ToLowerInvariant();
        if (!string.Equals(actualSha256, legacyUrl.Sha256, StringComparison.OrdinalIgnoreCase))
            throw new LegacyImportConflictException("O conteúdo de origem mudou desde o dry-run: o SHA-256 atual não corresponde à evidência inventariada.");

        var extraction = LegacyPageExtractor.Extract(fetched.Body);
        if (string.IsNullOrWhiteSpace(extraction.Text))
            throw new LegacyImportValidationException("Nenhum conteúdo textual útil foi extraído da página legada.");

        var title = NormalizeTitle(options.Title, extraction.Title, slug);
        var summary = string.IsNullOrWhiteSpace(options.Summary)
            ? "Rascunho importado do portal legado para revisão editorial."
            : options.Summary.Trim();
        var importedAt = DateTimeOffset.UtcNow;
        var payloadJson = JsonSerializer.Serialize(new
        {
            conteudo = extraction.Text,
            origemLegada = new
            {
                url = legacyUrl.Url,
                caminho = legacyUrl.NormalizedPath,
                sha256 = actualSha256,
                contentType = fetched.ContentType,
                bytes = fetched.Body.LongLength,
                migrationJobId = job.Id,
                legacyUrlId = legacyUrl.Id,
                importedAt
            }
        });

        var resource = new PortalResource(job.MunicipalityId, "PAGE", slug, title, summary, payloadJson, 0, actorId);
        resource.Update(title, summary, payloadJson, 0, null, null, actorId, importedAt);
        var snapshotJson = JsonSerializer.Serialize(new
        {
            resource.Id,
            resource.Kind,
            resource.Slug,
            resource.Title,
            resource.Summary,
            resource.PayloadJson,
            resource.Status,
            resource.DisplayOrder,
            resource.Version,
            resource.UpdatedAt,
            resource.UpdatedBy
        });
        var revision = new ContentRevision(job.MunicipalityId, resource.Kind, resource.Id, resource.Version, snapshotJson, actorId);

        var evidenceJson = JsonSerializer.Serialize(new
        {
            sourceUrl = legacyUrl.Url,
            legacyPath = legacyUrl.NormalizedPath,
            dryRunSha256 = legacyUrl.Sha256,
            importedSha256 = actualSha256,
            contentType = fetched.ContentType,
            bytes = fetched.Body.LongLength,
            targetType = "PAGE",
            targetResourceId = resource.Id,
            targetSlug = resource.Slug,
            redirectDestination = redirect?.DestinationPath,
            redirectStatusCode = redirect?.StatusCode,
            draftOnly = true,
            importedAt
        });
        var imported = new ImportedContent(
            job.MunicipalityId,
            job.Id,
            legacyUrl.Id,
            "PAGE",
            resource.Id.ToString(),
            actualSha256,
            evidenceJson);

        database.PortalResources.Add(resource);
        database.ContentRevisions.Add(revision);
        database.ImportedContents.Add(imported);
        if (redirect is not null)
            database.RedirectRules.Add(redirect);

        var importedCount = await database.ImportedContents.AsNoTracking()
            .CountAsync(item => item.MigrationJobId == job.Id, cancellationToken) + 1;
        job.Transition(MigrationJobState.DryRun, job.DiscoveredCount, importedCount, job.FailedCount);

        return new LegacyPageImportResult(imported, resource, redirect, evidenceJson);
    }

    private static string NormalizeSlug(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new LegacyImportValidationException("Slug do rascunho é obrigatório.");
        var normalized = value.Trim().ToLowerInvariant();
        if (normalized.Length > 180 || normalized.Any(character => !char.IsAsciiLetterOrDigit(character) && character != '-'))
            throw new LegacyImportValidationException("Slug deve ter até 180 caracteres e conter apenas letras sem acento, números e hífen.");
        return normalized;
    }

    private static string NormalizeTitle(string? requestedTitle, string extractedTitle, string slug)
    {
        var title = !string.IsNullOrWhiteSpace(requestedTitle)
            ? requestedTitle.Trim()
            : !string.IsNullOrWhiteSpace(extractedTitle)
                ? extractedTitle.Trim()
                : slug.Replace('-', ' ');
        return title.Length <= 220 ? title : title[..220].TrimEnd();
    }
}

public sealed record LegacyPageImportOptions(
    string Slug,
    string? Title,
    string? Summary,
    string? RedirectDestination,
    bool PermanentRedirect);

public sealed record LegacyPageImportResult(
    ImportedContent ImportedContent,
    PortalResource Resource,
    RedirectRule? Redirect,
    string EvidenceJson);

public sealed class LegacyImportValidationException(string message) : InvalidOperationException(message);

public sealed class LegacyImportConflictException(string message) : InvalidOperationException(message);
