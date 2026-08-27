using MunicipalPlatform.Api.Platform.Tenancy;

namespace MunicipalPlatform.Api.Modules.Identity.Domain;

public sealed class UserAccount : ITenantEntity
{
    private UserAccount() { }

    public UserAccount(Guid municipalityId, string username, string displayName, string role, string passwordHash)
    {
        if (municipalityId == Guid.Empty) throw new ArgumentException("Município obrigatório.", nameof(municipalityId));
        Id = Guid.NewGuid();
        MunicipalityId = municipalityId;
        Username = Required(username, 100).ToLowerInvariant();
        DisplayName = Required(displayName, 160);
        Role = Required(role, 64).ToUpperInvariant();
        PasswordHash = Required(passwordHash, 512);
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
    public int FailedLoginCount { get; private set; }
    public DateTimeOffset? LockedUntil { get; private set; }
    public int SessionVersion { get; private set; }
    public bool MfaEnabled { get; private set; }
    public string? MfaSecretProtected { get; private set; }
    public string? MfaPendingSecretProtected { get; private set; }

    public bool IsLocked(DateTimeOffset now) => LockedUntil.HasValue && LockedUntil > now;

    public void RecordFailedLogin(DateTimeOffset now)
    {
        if (LockedUntil.HasValue && LockedUntil <= now)
        {
            FailedLoginCount = 0;
            LockedUntil = null;
        }
        FailedLoginCount++;
        if (FailedLoginCount >= 5)
        {
            LockedUntil = now.AddMinutes(15);
            FailedLoginCount = 0;
        }
    }

    public void RecordLogin(DateTimeOffset loginAt)
    {
        LastLoginAt = loginAt;
        FailedLoginCount = 0;
        LockedUntil = null;
    }

    public void BeginMfaEnrollment(string protectedSecret)
    {
        MfaPendingSecretProtected = Required(protectedSecret, 4096);
    }

    public void ConfirmMfaEnrollment()
    {
        if (string.IsNullOrWhiteSpace(MfaPendingSecretProtected)) throw new InvalidOperationException("Não existe cadastro MFA pendente.");
        MfaSecretProtected = MfaPendingSecretProtected;
        MfaPendingSecretProtected = null;
        MfaEnabled = true;
        SessionVersion++;
    }

    public void DisableMfa()
    {
        MfaEnabled = false;
        MfaSecretProtected = null;
        MfaPendingSecretProtected = null;
        SessionVersion++;
    }

    public void RevokeSessions() => SessionVersion++;

    public void SetActive(bool active)
    {
        if (IsActive == active) return;
        IsActive = active;
        SessionVersion++;
    }

    public void AssignRole(string role)
    {
        var normalized = Required(role, 64).ToUpperInvariant();
        if (Role == normalized) return;
        Role = normalized;
        SessionVersion++;
    }

    private static string Required(string value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Campo obrigatório.");
        var normalized = value.Trim();
        if (normalized.Length > maxLength) throw new ArgumentException($"Campo excede {maxLength} caracteres.");
        return normalized;
    }
}
