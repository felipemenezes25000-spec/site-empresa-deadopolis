using MunicipalPlatform.Api.Platform.Tenancy;

namespace MunicipalPlatform.Api.Modules.Identity.Domain;

public sealed class RoleCapability : ITenantEntity
{
    private RoleCapability()
    {
    }

    public RoleCapability(Guid municipalityId, string role, string capability)
    {
        Id = Guid.NewGuid();
        MunicipalityId = municipalityId;
        Role = role.Trim().ToUpperInvariant();
        Capability = capability.Trim().ToLowerInvariant();
    }

    public Guid Id { get; private set; }
    public Guid MunicipalityId { get; private set; }
    public string Role { get; private set; } = string.Empty;
    public string Capability { get; private set; } = string.Empty;
}
