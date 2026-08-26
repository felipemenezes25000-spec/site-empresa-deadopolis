using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MunicipalPlatform.Api.Infrastructure.Persistence;
using MunicipalPlatform.Api.Modules.Media.Domain;
using MunicipalPlatform.Api.Modules.Media.Providers;
using MunicipalPlatform.Api.Modules.Media.Services;
using MunicipalPlatform.Api.Modules.Migration.Domain;
using MunicipalPlatform.Api.Modules.Migration.Security;
using MunicipalPlatform.Api.Platform.Storage;

namespace MunicipalPlatform.Api.Modules.Migration.Services;

public sealed class LegacyDocumentImportService(
    ILegacySourceFetcher sourceFetcher,
    IObjectStorageProvider storage,
    IMalwareScanner scanner)
{
    public async Task<LegacyDocumentImportResult> ImportAsync(
        MigrationJob job,
        LegacyUrl legacyUrl,
        LegacyDocumentImportOptions options,
        Guid actorId,
        ApplicationDbContext database,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(legacyUrl);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(database);
        if (actorId == Guid.Empty) throw new LegacyImportValidationException("Ator responsável pela importação é obrigatório.");
        if (legacyUrl.MigrationJobId != job.Id || legacyUrl.MunicipalityId != job.MunicipalityId)
            throw new LegacyImportValidationException("A URL legada não pertence ao job informado.");
        if (job.State != MigrationJobState.DryRun)
            throw new LegacyImportConflictException("O inventário precisa concluir o dry-run antes da importação.");
        if (legacyUrl.State != "MAPPED" || legacyUrl.Classification != "MIGRATE")
            throw new LegacyImportValidationException("Somente documentos mapeados e classificados para migração podem ser importados.");
        if (string.IsNullOrWhiteSpace(legacyUrl.Sha256))
            throw new LegacyImportValidationException("A URL não possui SHA-256 do dry-run.");
        if (storage.State == "NOT_CONFIGURED")
            throw new LegacyImportValidationException($"Storage não configurado: {storage.Description}");
        if (!Uri.TryCreate(legacyUrl.Url, UriKind.Absolute, out var sourceUri)
            || !ExternalUrlSafety.IsAllowedUri(sourceUri, job.AllowedHost))
            throw new LegacyImportValidationException("A URL de origem não atende à política SSRF do job.");
        if (await database.ImportedContents.AsNoTracking().AnyAsync(item => item.LegacyUrlId == legacyUrl.Id, cancellationToken))
            throw new LegacyImportConflictException("Esta URL legada já possui uma importação registrada.");
        if (await database.PublicDocuments.AsNoTracking().AnyAsync(item => item.LegacyUrlId == legacyUrl.Id, cancellationToken))
            throw new LegacyImportConflictException("Esta URL legada já possui documento no acervo.");

        var fetched = await sourceFetcher.FetchAsync(sourceUri, job.AllowedHost, cancellationToken);
        if (fetched.RedirectLocation is not null)
            throw new LegacyImportConflictException("A origem passou a responder com redirect depois do dry-run.");
        if (fetched.StatusCode is < 200 or > 299)
            throw new LegacyImportConflictException($"A origem respondeu HTTP {fetched.StatusCode} depois do dry-run.");
        if (fetched.Body.Length == 0 || fetched.Body.LongLength > DocumentFileInspector.MaxBytes)
            throw new LegacyImportValidationException("Documento deve possuir entre 1 byte e 25 MB.");

        var originalFileName = GetOriginalFileName(sourceUri);
        var detected = DocumentFileInspector.Detect(fetched.Body, originalFileName)
            ?? throw new LegacyImportValidationException("Magic bytes e extensão do documento são incompatíveis ou não permitidos.");
        if (!DocumentFileInspector.IsDeclaredMimeCompatible(fetched.ContentType, detected.MimeType))
            throw new LegacyImportValidationException("MIME declarado pela origem é incompatível com o conteúdo real do arquivo.");

        var actualSha256 = Convert.ToHexString(SHA256.HashData(fetched.Body)).ToLowerInvariant();
        if (!string.Equals(actualSha256, legacyUrl.Sha256, StringComparison.OrdinalIgnoreCase))
            throw new LegacyImportConflictException("O SHA-256 atual não corresponde à evidência do dry-run.");

        var asset = await database.MediaAssets
            .FirstOrDefaultAsync(item => item.Sha256 == actualSha256, cancellationToken);
        var reusedAsset = asset is not null;
        MalwareScanResult? scan = null;
        if (asset is null)
        {
            scan = await scanner.ScanAsync(fetched.Body, cancellationToken);
            if (!scan.IsClean && scanner.State != "NOT_CONFIGURED")
                throw new LegacyImportValidationException("O scanner de malware recusou o documento.");
            var objectKey = $"legacy-documents/{actualSha256[..2]}/{actualSha256}.{detected.Extension}";
            await storage.SaveAsync(objectKey, fetched.Body, cancellationToken);
            asset = new MediaAsset(
                job.MunicipalityId,
                objectKey,
                originalFileName,
                detected.MimeType,
                fetched.Body.LongLength,
                actualSha256,
                actorId);
            if (scan.IsClean && scanner.State != "NOT_CONFIGURED") asset.Approve();
            database.MediaAssets.Add(asset);
        }
        else if (asset.Status == "REJECTED")
        {
            throw new LegacyImportConflictException("Conteúdo idêntico já foi recusado pelo pipeline de segurança.");
        }

        var document = new PublicDocument(
            job.MunicipalityId,
            legacyUrl.Id,
            job.Id,
            asset.Id,
            options.Category,
            options.Subcategory,
            options.Title,
            options.Description,
            options.DocumentNumber,
            options.ProcessNumber,
            options.ReferencePeriod,
            options.PublicationDate,
            options.ResponsibleDepartment,
            string.IsNullOrWhiteSpace(options.DocumentType) ? detected.DocumentType : options.DocumentType,
            legacyUrl.Url,
            legacyUrl.NormalizedPath,
            originalFileName,
            detected.MimeType,
            fetched.Body.LongLength,
            actualSha256);
        var importedAt = DateTimeOffset.UtcNow;
        var evidenceJson = JsonSerializer.Serialize(new
        {
            sourceUrl = legacyUrl.Url,
            legacyUrlId = legacyUrl.Id,
            migrationJobId = job.Id,
            dryRunSha256 = legacyUrl.Sha256,
            importedSha256 = actualSha256,
            declaredMime = fetched.ContentType,
            detectedMime = detected.MimeType,
            originalFileName,
            bytes = fetched.Body.LongLength,
            mediaAssetId = asset.Id,
            mediaStatus = asset.Status,
            reusedAsset,
            scannerState = scanner.State,
            scanDetail = scan?.Detail,
            publicDocumentId = document.Id,
            documentStatus = document.Status,
            importedAt
        });
        var imported = new ImportedContent(
            job.MunicipalityId,
            job.Id,
            legacyUrl.Id,
            "PUBLIC_DOCUMENT",
            document.Id.ToString(),
            actualSha256,
            evidenceJson);

        database.PublicDocuments.Add(document);
        database.ImportedContents.Add(imported);
        var importedCount = await database.ImportedContents.AsNoTracking()
            .CountAsync(item => item.MigrationJobId == job.Id, cancellationToken) + 1;
        job.Transition(MigrationJobState.DryRun, job.DiscoveredCount, importedCount, job.FailedCount);
        return new LegacyDocumentImportResult(imported, document, asset, evidenceJson, reusedAsset);
    }

    private static string GetOriginalFileName(Uri sourceUri)
    {
        var decoded = Uri.UnescapeDataString(sourceUri.AbsolutePath);
        var fileName = Path.GetFileName(decoded);
        if (string.IsNullOrWhiteSpace(fileName) || fileName.Length > 260)
            throw new LegacyImportValidationException("A URL legada não contém um nome de arquivo válido.");
        return fileName;
    }
}

public sealed record LegacyDocumentImportOptions(
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

public sealed record LegacyDocumentImportResult(
    ImportedContent ImportedContent,
    PublicDocument Document,
    MediaAsset Asset,
    string EvidenceJson,
    bool ReusedAsset);
