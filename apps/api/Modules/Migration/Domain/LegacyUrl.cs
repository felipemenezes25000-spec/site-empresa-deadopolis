using MunicipalPlatform.Api.Platform.Tenancy;

namespace MunicipalPlatform.Api.Modules.Migration.Domain;

public sealed class LegacyUrl : ITenantEntity
{
    private LegacyUrl() { }

    public LegacyUrl(Guid municipalityId, Guid migrationJobId, string url, string normalizedPath, int depth)
    {
        Id = Guid.NewGuid();
        MunicipalityId = municipalityId;
        MigrationJobId = migrationJobId;
        Url = url.Trim();
        NormalizedPath = normalizedPath.Trim();
        Depth = depth;
        Classification = "UNCLASSIFIED";
        State = "DISCOVERED";
        DiscoveredAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid MunicipalityId { get; private set; }
    public Guid MigrationJobId { get; private set; }
    public string Url { get; private set; } = string.Empty;
    public string NormalizedPath { get; private set; } = string.Empty;
    public int Depth { get; private set; }
    public string? ContentType { get; private set; }
    public long? ContentLength { get; private set; }
    public string? Sha256 { get; private set; }
    public string Classification { get; private set; } = string.Empty;
    public string State { get; private set; } = string.Empty;
    public string? FailureReason { get; private set; }
    public DateTimeOffset DiscoveredAt { get; private set; }

    public void Classify(
        string classification,
        string? contentType,
        long? contentLength,
        string? sha256,
        string? decisionReason = null)
    {
        Classification = classification;
        ContentType = contentType;
        ContentLength = contentLength;
        Sha256 = sha256;
        FailureReason = decisionReason;
        State = "MAPPED";
    }

    public void Fail(string reason)
    {
        State = "FAILED";
        FailureReason = reason;
    }
}
