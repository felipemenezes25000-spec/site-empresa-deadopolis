using MunicipalPlatform.Api.Platform.Tenancy;

namespace MunicipalPlatform.Api.Modules.Operations.Domain;

public sealed class AuditEvent : ITenantEntity
{
    private AuditEvent()
    {
    }

    public AuditEvent(
        Guid municipalityId,
        Guid? actorId,
        string action,
        string resource,
        string resourceId,
        string semanticDiff,
        string correlationId)
    {
        Id = Guid.NewGuid();
        MunicipalityId = municipalityId;
        ActorId = actorId;
        Action = action.Trim();
        Resource = resource.Trim();
        ResourceId = resourceId.Trim();
        SemanticDiff = semanticDiff;
        CorrelationId = correlationId.Trim();
        OccurredAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid MunicipalityId { get; private set; }
    public Guid? ActorId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string Resource { get; private set; } = string.Empty;
    public string ResourceId { get; private set; } = string.Empty;
    public string SemanticDiff { get; private set; } = string.Empty;
    public string CorrelationId { get; private set; } = string.Empty;
    public string? IpAddressHash { get; private set; }
    public string? UserAgent { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
}
