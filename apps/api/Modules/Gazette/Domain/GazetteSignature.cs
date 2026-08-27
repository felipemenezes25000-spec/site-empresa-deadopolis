using MunicipalPlatform.Api.Platform.Tenancy;

namespace MunicipalPlatform.Api.Modules.Gazette.Domain;

public sealed class GazetteSignature : ITenantEntity
{
    private GazetteSignature() { }

    public GazetteSignature(
        Guid municipalityId,
        Guid editionId,
        string provider,
        string signatureBase64,
        string certificateSerial,
        string certificateSubject,
        string certificateIssuer,
        DateTimeOffset certificateValidFrom,
        DateTimeOffset certificateValidTo,
        bool isIcpBrasil,
        DateTimeOffset signedAt,
        string validationState)
    {
        if (municipalityId == Guid.Empty || editionId == Guid.Empty)
            throw new ArgumentException("Município e edição são obrigatórios.");
        if (certificateValidTo <= certificateValidFrom)
            throw new ArgumentOutOfRangeException(nameof(certificateValidTo), "A validade final do certificado deve ser posterior à inicial.");
        if (signedAt < certificateValidFrom || signedAt > certificateValidTo)
            throw new ArgumentOutOfRangeException(nameof(signedAt), "A assinatura precisa ocorrer dentro da validade do certificado.");

        var normalizedSignature = Require(signatureBase64, nameof(signatureBase64), 64_000);
        try
        {
            _ = Convert.FromBase64String(normalizedSignature);
        }
        catch (FormatException exception)
        {
            throw new ArgumentException("A assinatura deve estar codificada em Base64 válido.", nameof(signatureBase64), exception);
        }

        Id = Guid.NewGuid();
        MunicipalityId = municipalityId;
        GazetteEditionId = editionId;
        Provider = Require(provider, nameof(provider), 120);
        SignatureBase64 = normalizedSignature;
        CertificateSerial = Require(certificateSerial, nameof(certificateSerial), 240);
        CertificateSubject = Require(certificateSubject, nameof(certificateSubject), 1_000);
        CertificateIssuer = Require(certificateIssuer, nameof(certificateIssuer), 1_000);
        CertificateValidFrom = certificateValidFrom;
        CertificateValidTo = certificateValidTo;
        IsIcpBrasil = isIcpBrasil;
        SignedAt = signedAt;
        ValidationState = Require(validationState, nameof(validationState), 1_000);
    }

    public Guid Id { get; private set; }
    public Guid MunicipalityId { get; private set; }
    public Guid GazetteEditionId { get; private set; }
    public string Provider { get; private set; } = string.Empty;
    public string SignatureBase64 { get; private set; } = string.Empty;
    public string CertificateSerial { get; private set; } = string.Empty;
    public string CertificateSubject { get; private set; } = string.Empty;
    public string CertificateIssuer { get; private set; } = string.Empty;
    public DateTimeOffset CertificateValidFrom { get; private set; }
    public DateTimeOffset CertificateValidTo { get; private set; }
    public bool IsIcpBrasil { get; private set; }
    public DateTimeOffset SignedAt { get; private set; }
    public string ValidationState { get; private set; } = string.Empty;

    private static string Require(string value, string parameterName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Campo obrigatório.", parameterName);
        var normalized = value.Trim();
        if (normalized.Length > maxLength) throw new ArgumentException($"Campo deve possuir até {maxLength} caracteres.", parameterName);
        return normalized;
    }
}
