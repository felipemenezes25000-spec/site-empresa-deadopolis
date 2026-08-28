using MunicipalPlatform.Api.Platform.Tenancy;

namespace MunicipalPlatform.Api.Modules.Migration.Domain;

public sealed class RedirectRule : ITenantEntity
{
    private RedirectRule() { }
    public RedirectRule(Guid municipalityId, string legacyPath, string destinationPath, bool permanent) { Id = Guid.NewGuid(); MunicipalityId = municipalityId; LegacyPath = LegacyUrlNormalizer.Normalize(legacyPath); DestinationPath = RequireInternalDestination(destinationPath); StatusCode = permanent ? 301 : 302; CreatedAt = DateTimeOffset.UtcNow; }
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

    /// <summary>Aceita apenas destinos internos: um redirect legado nunca pode levar o cidadao para outro host.</summary>
    public static bool IsInternalDestination(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        var normalized = value.Trim();
        if (normalized.Length > 2_048) return false;
        if (normalized.Any(char.IsControl)) return false;
        if (!normalized.StartsWith('/')) return false;
        // "//host" e "/\host" sao relativos ao protocolo e saem do dominio municipal.
        return !normalized.StartsWith("//", StringComparison.Ordinal)
            && !normalized.StartsWith("/\\", StringComparison.Ordinal);
    }

    private static string RequireInternalDestination(string destinationPath)
    {
        var normalized = (destinationPath ?? string.Empty).Trim();
        return IsInternalDestination(normalized)
            ? normalized
            : throw new ArgumentException("O destino do redirect deve ser um caminho interno iniciado por '/' e nunca pode apontar para outro host.", nameof(destinationPath));
    }
}
