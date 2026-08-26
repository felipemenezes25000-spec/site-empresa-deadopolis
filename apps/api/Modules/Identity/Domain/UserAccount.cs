using MunicipalPlatform.Api.Platform.Tenancy;

namespace MunicipalPlatform.Api.Modules.Identity.Domain;

public sealed class UserAccount : ITenantEntity
{
    private UserAccount()
    {
    }

    public UserAccount(
        Guid municipalityId,
        string username,
        string displayName,
        string role,
        string passwordHash)
    {
        Id = Guid.NewGuid();
        MunicipalityId = municipalityId;
        Username = username.Trim().ToLowerInvariant();
        DisplayName = displayName.Trim();
        Role = role.Trim().ToUpperInvariant();
        PasswordHash = passwordHash;
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid MunicipalityId { get; private set; }
    public string Username { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string Role { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? LastLoginAt { get; private set; }

    public void RecordLogin(DateTimeOffset loginAt) => LastLoginAt = loginAt;
}
