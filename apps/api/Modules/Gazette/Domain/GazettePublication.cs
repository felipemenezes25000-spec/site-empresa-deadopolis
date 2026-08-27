using System.Text.RegularExpressions;
using MunicipalPlatform.Api.Platform.Tenancy;

namespace MunicipalPlatform.Api.Modules.Gazette.Domain;

public sealed partial class GazettePublication : ITenantEntity
{
    private GazettePublication() { }

    public GazettePublication(
        Guid municipalityId,
        Guid editionId,
        DateTimeOffset publishedAt,
        string sha256,
        string verificationCode,
        string publicUrl)
    {
        if (municipalityId == Guid.Empty || editionId == Guid.Empty)
            throw new ArgumentException("Município e edição são obrigatórios.");

        var normalizedHash = Require(sha256, nameof(sha256), 64).ToLowerInvariant();
        if (!Sha256Regex().IsMatch(normalizedHash))
            throw new ArgumentException("SHA-256 inválido.", nameof(sha256));

        var normalizedUrl = Require(publicUrl, nameof(publicUrl), 2_048);
        if (!Uri.TryCreate(normalizedUrl, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
            throw new ArgumentException("URL pública precisa ser HTTP ou HTTPS absoluta.", nameof(publicUrl));

        Id = Guid.NewGuid();
        MunicipalityId = municipalityId;
        GazetteEditionId = editionId;
        PublishedAt = publishedAt;
        Sha256 = normalizedHash;
        VerificationCode = Require(verificationCode, nameof(verificationCode), 128);
        PublicUrl = uri.ToString();
    }

    public Guid Id { get; private set; }
    public Guid MunicipalityId { get; private set; }
    public Guid GazetteEditionId { get; private set; }
    public DateTimeOffset PublishedAt { get; private set; }
    public string Sha256 { get; private set; } = string.Empty;
    public string VerificationCode { get; private set; } = string.Empty;
    public string PublicUrl { get; private set; } = string.Empty;

    private static string Require(string value, string parameterName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Campo obrigatório.", parameterName);
        var normalized = value.Trim();
        if (normalized.Length > maxLength) throw new ArgumentException($"Campo deve possuir até {maxLength} caracteres.", parameterName);
        return normalized;
    }

    [GeneratedRegex("^[a-f0-9]{64}$", RegexOptions.CultureInvariant)]
    private static partial Regex Sha256Regex();
}
