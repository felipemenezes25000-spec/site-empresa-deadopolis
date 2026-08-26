using MunicipalPlatform.Api.Platform.Tenancy;

namespace MunicipalPlatform.Api.Modules.Operations.Domain;

public sealed class IntegrationStatus : ITenantEntity
{
    private IntegrationStatus()
    {
    }

    public IntegrationStatus(Guid municipalityId, string provider, IntegrationState state, string message)
    {
        Id = Guid.NewGuid();
        MunicipalityId = municipalityId;
        Provider = provider.Trim();
        State = state;
        Message = message.Trim();
        LastCheckedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid MunicipalityId { get; private set; }
    public string Provider { get; private set; } = string.Empty;
    public IntegrationState State { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public string? LastErrorCode { get; private set; }
    public DateTimeOffset LastCheckedAt { get; private set; }
}
