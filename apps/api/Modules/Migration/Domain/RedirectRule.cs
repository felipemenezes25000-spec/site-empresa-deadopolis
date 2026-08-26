using MunicipalPlatform.Api.Platform.Tenancy;

namespace MunicipalPlatform.Api.Modules.Migration.Domain;

public sealed class RedirectRule : ITenantEntity
{
    private RedirectRule() { }
    public RedirectRule(Guid municipalityId, string legacyPath, string destinationPath, bool permanent) { Id = Guid.NewGuid(); MunicipalityId = municipalityId; LegacyPath = LegacyUrlNormalizer.Normalize(legacyPath); DestinationPath = destinationPath.Trim(); StatusCode = permanent ? 301 : 302; CreatedAt = DateTimeOffset.UtcNow; }
    public Guid Id { get; private set; }
    public Guid MunicipalityId { get; private set; }
    public string LegacyPath { get; private set; } = string.Empty;
    public string DestinationPath { get; private set; } = string.Empty;
    public int StatusCode { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? LastValidatedAt { get; private set; }
    public void MarkValidated(DateTimeOffset at) => LastValidatedAt = at;
    public void Deactivate() => IsActive = false;
}
