using MunicipalPlatform.Api.Modules.Gazette.Domain;

namespace MunicipalPlatform.Api.Tests.Gazette;

public sealed class GazetteIntegrityRecordTests
{
    [Fact]
    public void SignatureRejectsSigningOutsideCertificateValidity()
    {
        var from = DateTimeOffset.UtcNow.AddDays(-1);
        var to = DateTimeOffset.UtcNow.AddDays(1);

        Assert.Throws<ArgumentOutOfRangeException>(() => new GazetteSignature(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "DEMO",
            Convert.ToBase64String([1, 2, 3]),
            "SERIAL",
            "CN=Subject",
            "CN=Issuer",
            from,
            to,
            false,
            to.AddMinutes(1),
            "VALID"));
    }

    [Fact]
    public void PublicationNormalizesHashAndRequiresAbsoluteHttpUrl()
    {
        var record = new GazettePublication(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            new string('A', 64),
            "DEO-2026-0001",
            "https://deodapolis.ms.gov.br/api/v1/gazette/edition/document");

        Assert.Equal(new string('a', 64), record.Sha256);
        Assert.StartsWith("https://", record.PublicUrl, StringComparison.Ordinal);
        Assert.Throws<ArgumentException>(() => new GazettePublication(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            new string('a', 64),
            "DEO-2026-0002",
            "/diario/arquivo.pdf"));
    }

    [Fact]
    public void CorrectionRequiresDistinctEditionsAndMeaningfulReason()
    {
        var editionId = Guid.NewGuid();

        Assert.Throws<ArgumentException>(() => new GazetteCorrection(
            Guid.NewGuid(),
            editionId,
            editionId,
            "Correção administrativa necessária.",
            Guid.NewGuid()));

        Assert.Throws<ArgumentException>(() => new GazetteCorrection(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "curta",
            Guid.NewGuid()));
    }
}
