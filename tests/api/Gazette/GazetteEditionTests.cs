using MunicipalPlatform.Api.Modules.Gazette.Domain;

namespace MunicipalPlatform.Api.Tests.Gazette;

public sealed class GazetteEditionTests
{
    [Fact]
    public void RegisterGeneratedDocumentStoresSha256AndVerificationCode()
    {
        var actor = Guid.NewGuid();
        var edition = CreateApprovedEdition(actor);

        edition.RegisterGeneratedDocument(
            "gazette/2026/42.pdf",
            "D7A8FBB307D7809469CA9ABCB0082E4F8D5651E46D3CDB762D02D0BF37C9E592",
            "DEO-2026-0042-A1B2C3",
            actor,
            DateTimeOffset.UtcNow);

        Assert.Equal(GazetteStatus.Generated, edition.Status);
        Assert.Equal("d7a8fbb307d7809469ca9abcb0082e4f8d5651e46d3cdb762d02d0bf37c9e592", edition.Sha256);
        Assert.Equal("DEO-2026-0042-A1B2C3", edition.VerificationCode);
    }

    [Fact]
    public void PublishRejectsUnsignedNewEdition()
    {
        var edition = GeneratedEdition();

        var error = Assert.Throws<GazetteTransitionException>(() =>
            edition.Publish(Guid.NewGuid(), DateTimeOffset.UtcNow));

        Assert.Contains("assinada", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PublishedEditionRejectsDocumentReplacement()
    {
        var edition = GeneratedEdition();
        edition.RegisterSignature("serial-demo", "CN=Demo", "CN=Demo CA", DateTimeOffset.UtcNow, Guid.NewGuid());
        edition.Publish(Guid.NewGuid(), DateTimeOffset.UtcNow);

        Assert.Throws<GazetteImmutabilityException>(() => edition.RegisterGeneratedDocument(
            "gazette/replacement.pdf",
            new string('a', 64),
            "DEO-NEW",
            Guid.NewGuid(),
            DateTimeOffset.UtcNow));
    }

    [Fact]
    public void ReviewRejectsEmptyComposition()
    {
        var edition = GazetteEdition.Create(Guid.NewGuid(), 43, 2026, GazetteEditionType.Ordinary, new DateOnly(2026, 8, 25), Guid.NewGuid());

        var error = Assert.Throws<GazetteTransitionException>(() =>
            edition.SubmitForReview(Guid.NewGuid(), DateTimeOffset.UtcNow));

        Assert.Contains("seção", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static GazetteEdition GeneratedEdition()
    {
        var actor = Guid.NewGuid();
        var edition = CreateApprovedEdition(actor);
        edition.RegisterGeneratedDocument("gazette/42.pdf", new string('b', 64), "DEO-2026-0042-A1B2C3", actor, DateTimeOffset.UtcNow);
        return edition;
    }

    private static GazetteEdition CreateApprovedEdition(Guid actor)
    {
        var edition = GazetteEdition.Create(Guid.NewGuid(), 42, 2026, GazetteEditionType.Ordinary, new DateOnly(2026, 8, 25), actor);
        edition.SetComposition(
            "{\"sections\":[{\"title\":\"Administração\",\"acts\":[{\"title\":\"Ato de teste\",\"body\":\"Conteúdo sintético de teste.\"}]}]}",
            actor,
            DateTimeOffset.UtcNow);
        edition.SubmitForReview(actor, DateTimeOffset.UtcNow);
        edition.Approve(actor, DateTimeOffset.UtcNow);
        return edition;
    }
}
