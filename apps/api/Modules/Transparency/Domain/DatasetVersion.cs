using MunicipalPlatform.Api.Platform.Tenancy;

namespace MunicipalPlatform.Api.Modules.Transparency.Domain;

public sealed class DatasetVersion : ITenantEntity
{
    private DatasetVersion() { }
    public DatasetVersion(Guid municipalityId, Guid datasetId, int version, string fileName, string objectKey, string mimeType, long sizeBytes, string sha256, string format, string metadataJson, DateTimeOffset publishedAt)
    { if(version<1)throw new ArgumentOutOfRangeException(nameof(version)); if(sizeBytes<0)throw new ArgumentOutOfRangeException(nameof(sizeBytes)); Id=Guid.NewGuid();MunicipalityId=municipalityId;DatasetId=datasetId;Version=version;FileName=fileName.Trim();ObjectKey=objectKey.Trim();MimeType=mimeType.Trim();SizeBytes=sizeBytes;Sha256=sha256.Trim().ToLowerInvariant();Format=format.Trim().ToUpperInvariant();MetadataJson=metadataJson;PublishedAt=publishedAt; }
    public Guid Id { get; private set; }
    public Guid MunicipalityId { get; private set; }
    public Guid DatasetId { get; private set; }
    public int Version { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public string ObjectKey { get; private set; } = string.Empty;
    public string MimeType { get; private set; } = string.Empty;
    public long SizeBytes { get; private set; }
    public string Sha256 { get; private set; } = string.Empty;
    public string Format { get; private set; } = string.Empty;
    public string MetadataJson { get; private set; } = "{}";
    public DateTimeOffset PublishedAt { get; private set; }
}
