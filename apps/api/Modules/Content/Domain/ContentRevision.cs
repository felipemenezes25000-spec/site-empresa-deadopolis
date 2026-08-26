using MunicipalPlatform.Api.Platform.Tenancy;

namespace MunicipalPlatform.Api.Modules.Content.Domain;

public sealed class ContentRevision : ITenantEntity
{
    private ContentRevision() { }

    public ContentRevision(Guid municipalityId, string resourceKind, Guid resourceId, int version, string snapshotJson, Guid createdBy)
    {
        Id = Guid.NewGuid();
        MunicipalityId = municipalityId;
        ResourceKind = resourceKind.Trim().ToUpperInvariant();
        ResourceId = resourceId;
        Version = version;
        SnapshotJson = snapshotJson;
        CreatedBy = createdBy;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid MunicipalityId { get; private set; }
    public string ResourceKind { get; private set; } = string.Empty;
    public Guid ResourceId { get; private set; }
    public int Version { get; private set; }
    public string SnapshotJson { get; private set; } = "{}";
    public Guid CreatedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
}
