using MunicipalPlatform.Api.Platform.Tenancy;

namespace MunicipalPlatform.Api.Modules.Migration.Domain;

public sealed class PublicDocument : ITenantEntity
{
    private PublicDocument() { }

    public PublicDocument(
        Guid municipalityId, Guid legacyUrlId, Guid migrationJobId, Guid mediaAssetId,
        string category, string? subcategory, string title, string? description,
        string? documentNumber, string? processNumber, string? referencePeriod,
        DateOnly? publicationDate, string? responsibleDepartment, string documentType,
        string sourceUrl, string normalizedLegacyPath, string originalFileName,
        string mimeType, long sizeBytes, string sha256)
    {
        if (municipalityId == Guid.Empty || legacyUrlId == Guid.Empty || migrationJobId == Guid.Empty || mediaAssetId == Guid.Empty)
            throw new ArgumentException("Município e relacionamentos de migração são obrigatórios.");
        Id = Guid.NewGuid(); MunicipalityId = municipalityId; LegacyUrlId = legacyUrlId; MigrationJobId = migrationJobId; MediaAssetId = mediaAssetId;
        Category = Require(category, 80).ToUpperInvariant(); Subcategory = Optional(subcategory, 120).ToUpperInvariant();
        Title = Require(title, 220); Description = Optional(description, 2_000); DocumentNumber = Optional(documentNumber, 120);
        ProcessNumber = Optional(processNumber, 120); ReferencePeriod = Optional(referencePeriod, 120); PublicationDate = publicationDate;
        ResponsibleDepartment = Optional(responsibleDepartment, 180); DocumentType = Require(documentType, 80).ToUpperInvariant();
        SourceUrl = Require(sourceUrl, 2_048); NormalizedLegacyPath = Require(normalizedLegacyPath, 2_048);
        OriginalFileName = Require(originalFileName, 260); MimeType = Require(mimeType, 180).ToLowerInvariant();
        SizeBytes = sizeBytes > 0 ? sizeBytes : throw new ArgumentException("Documento vazio não pode ser arquivado.");
        Sha256 = Require(sha256, 64).ToLowerInvariant(); SourceSystem = "LEGACY_PORTAL"; Status = "DRAFT";
        CreatedAt = UpdatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid MunicipalityId { get; private set; }
    public Guid LegacyUrlId { get; private set; }
    public Guid MigrationJobId { get; private set; }
    public Guid MediaAssetId { get; private set; }
    public string Category { get; private set; } = string.Empty;
    public string Subcategory { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string DocumentNumber { get; private set; } = string.Empty;
    public string ProcessNumber { get; private set; } = string.Empty;
    public string ReferencePeriod { get; private set; } = string.Empty;
    public DateOnly? PublicationDate { get; private set; }
    public string ResponsibleDepartment { get; private set; } = string.Empty;
    public string DocumentType { get; private set; } = string.Empty;
    public string SourceUrl { get; private set; } = string.Empty;
    public string NormalizedLegacyPath { get; private set; } = string.Empty;
    public string OriginalFileName { get; private set; } = string.Empty;
    public string MimeType { get; private set; } = string.Empty;
    public long SizeBytes { get; private set; }
    public string Sha256 { get; private set; } = string.Empty;
    public string SourceSystem { get; private set; } = string.Empty;
    public string Status { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? PublishedAt { get; private set; }

    public void Publish(DateTimeOffset at) { if (Status == "ARCHIVED") throw new InvalidOperationException("Documento arquivado deve ser restaurado antes da publicação."); Status = "PUBLISHED"; PublishedAt ??= at; UpdatedAt = at; }
    public void Archive(DateTimeOffset at) { Status = "ARCHIVED"; UpdatedAt = at; }
    public void Restore(DateTimeOffset at) { if (Status != "ARCHIVED") throw new InvalidOperationException("Somente documento arquivado pode ser restaurado."); Status = "DRAFT"; UpdatedAt = at; }

    private static string Require(string? value, int max) { if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Campo obrigatório."); var normalized = value.Trim(); if (normalized.Length > max) throw new ArgumentException($"Campo deve possuir até {max} caracteres."); return normalized; }
    private static string Optional(string? value, int max) { if (string.IsNullOrWhiteSpace(value)) return string.Empty; var normalized = value.Trim(); if (normalized.Length > max) throw new ArgumentException($"Campo deve possuir até {max} caracteres."); return normalized; }
}
