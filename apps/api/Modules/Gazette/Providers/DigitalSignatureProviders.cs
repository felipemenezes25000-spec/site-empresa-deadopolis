using System.Security.Cryptography;

namespace MunicipalPlatform.Api.Modules.Gazette.Providers;

public sealed record CertificateDescriptor(string Serial, string Subject, string Issuer, DateTimeOffset ValidFrom, DateTimeOffset ValidTo, bool IsIcpBrasil);
public sealed record DigitalSignatureResult(string SignatureBase64, CertificateDescriptor Certificate, DateTimeOffset SignedAt, string Provider);
public sealed record TimestampResult(DateTimeOffset Timestamp, string Provider, string? TokenBase64);
public sealed record SignatureValidationResult(bool IsValid, string Provider, string Detail);

public interface ICertificateProvider
{
    string State { get; }
    Task<CertificateDescriptor?> GetCertificateAsync(CancellationToken cancellationToken = default);
}

public interface IDigitalSigner
{
    string State { get; }
    string Description { get; }
    Task<DigitalSignatureResult> SignHashAsync(string sha256, CancellationToken cancellationToken = default);
}

public interface ITimestampProvider
{
    string State { get; }
    Task<TimestampResult> TimestampHashAsync(string sha256, CancellationToken cancellationToken = default);
}

public interface ISignatureValidator
{
    string State { get; }
    Task<SignatureValidationResult> ValidateAsync(string sha256, string signatureBase64, CancellationToken cancellationToken = default);
}

public sealed class DemoDigitalSigner : IDigitalSigner, ICertificateProvider, ISignatureValidator, IDisposable
{
    private readonly RSA _rsa = RSA.Create(2048);
    private readonly CertificateDescriptor _certificate = new(
        "DEMO-NOT-ICP",
        "CN=Deodápolis Demonstração - NÃO ICP-Brasil",
        "CN=MunicipalPlatform Demo CA",
        DateTimeOffset.UnixEpoch,
        DateTimeOffset.Parse("2100-01-01T00:00:00Z", System.Globalization.CultureInfo.InvariantCulture),
        false);

    public string State => "DEMO_ONLY";
    public string Description => "Assinatura RSA de demonstração. Não é certificado ICP-Brasil e nunca deve ser tratada como assinatura oficial.";

    public Task<CertificateDescriptor?> GetCertificateAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<CertificateDescriptor?>(_certificate);
    }

    public Task<DigitalSignatureResult> SignHashAsync(string sha256, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var hash = ParseHash(sha256);
        var signature = _rsa.SignHash(hash, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
        return Task.FromResult(new DigitalSignatureResult(
            Convert.ToBase64String(signature),
            _certificate,
            DateTimeOffset.UtcNow,
            "DEMO_RSA"));
    }

    public Task<SignatureValidationResult> ValidateAsync(string sha256, string signatureBase64, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var valid = _rsa.VerifyHash(ParseHash(sha256), Convert.FromBase64String(signatureBase64), HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
        return Task.FromResult(new SignatureValidationResult(valid, "DEMO_RSA", valid ? "Assinatura de demonstração íntegra." : "Assinatura de demonstração inválida."));
    }

    public void Dispose() => _rsa.Dispose();

    private static byte[] ParseHash(string sha256)
    {
        try
        {
            var bytes = Convert.FromHexString(sha256);
            if (bytes.Length != 32) throw new FormatException();
            return bytes;
        }
        catch (FormatException)
        {
            throw new ArgumentException("SHA-256 inválido.", nameof(sha256));
        }
    }
}

public sealed class NotConfiguredDigitalSigner : IDigitalSigner, ICertificateProvider, ISignatureValidator
{
    public string State => "NOT_CONFIGURED";
    public string Description => "Certificado/serviço ICP-Brasil não configurado. A arquitetura está pronta, mas nenhuma assinatura oficial é simulada.";

    public Task<CertificateDescriptor?> GetCertificateAsync(CancellationToken cancellationToken = default) => Task.FromResult<CertificateDescriptor?>(null);
    public Task<DigitalSignatureResult> SignHashAsync(string sha256, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Assinatura ICP-Brasil está NOT_CONFIGURED.");
    public Task<SignatureValidationResult> ValidateAsync(string sha256, string signatureBase64, CancellationToken cancellationToken = default) =>
        Task.FromResult(new SignatureValidationResult(false, "NOT_CONFIGURED", Description));
}

public sealed class NotConfiguredTimestampProvider : ITimestampProvider
{
    public string State => "NOT_CONFIGURED";
    public Task<TimestampResult> TimestampHashAsync(string sha256, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Carimbo do tempo está NOT_CONFIGURED.");
}
