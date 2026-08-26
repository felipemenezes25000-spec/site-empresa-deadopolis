using MunicipalPlatform.Api.Platform.Tenancy;

namespace MunicipalPlatform.Api.Modules.Media.Domain;

public sealed class MediaAsset : ITenantEntity
{
    private MediaAsset()
    {
    }

    public MediaAsset(
        Guid municipalityId,
        string objectKey,
        string originalFileName,
        string mimeType,
        long sizeBytes,
        string sha256,
        Guid uploadedBy)
    {
        Id = Guid.NewGuid();
        MunicipalityId = municipalityId;
        ObjectKey = objectKey.Trim();
        OriginalFileName = originalFileName.Trim();
        MimeType = mimeType.Trim().ToLowerInvariant();
        SizeBytes = sizeBytes;
        Sha256 = sha256.Trim().ToLowerInvariant();
        UploadedBy = uploadedBy;
        UploadedAt = DateTimeOffset.UtcNow;
        Status = "QUARANTINED";
    }

    public Guid Id { get; private set; }
    public Guid MunicipalityId { get; private set; }
    public string ObjectKey { get; private set; } = string.Empty;
    public string OriginalFileName { get; private set; } = string.Empty;
    public string MimeType { get; private set; } = string.Empty;
    public long SizeBytes { get; private set; }
    public string Sha256 { get; private set; } = string.Empty;
    public string Status { get; private set; } = string.Empty;
    public string AltText { get; private set; } = string.Empty;
    public string Caption { get; private set; } = string.Empty;
    public string Credit { get; private set; } = string.Empty;
    public Guid UploadedBy { get; private set; }
    public DateTimeOffset UploadedAt { get; private set; }
}
