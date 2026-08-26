using System.Security.Cryptography;
using MunicipalPlatform.Api.Modules.Gazette.Domain;
using MunicipalPlatform.Api.Modules.Gazette.Services;

namespace MunicipalPlatform.Api.Tests.Gazette;

public sealed class GazetteDocumentServiceTests
{
    [Fact]
    public void Generate_is_deterministic_for_same_approved_snapshot()
    {
        var actor = Guid.NewGuid();
        var edition = GazetteEdition.Create(Guid.NewGuid(), 12, 2026, GazetteEditionType.Ordinary, new DateOnly(2026, 8, 26), actor);
        var service = new GazetteDocumentService();
        edition.SetComposition(service.NormalizeComposition(new GazetteComposition([
            new GazetteSectionInput("Secretaria Municipal de Administração", [
                new GazetteActInput("Portaria de demonstração", "Conteúdo sintético sem valor de ato oficial.", "Administração", "DEMONSTRAÇÃO")
            ])
        ])), actor, DateTimeOffset.UnixEpoch);
        edition.SubmitForReview(actor, DateTimeOffset.UnixEpoch.AddMinutes(1));
        edition.Approve(actor, DateTimeOffset.UnixEpoch.AddMinutes(2));

        var first = service.Generate(edition, "https://demo.example.test");
        var second = service.Generate(edition, "https://demo.example.test");

        Assert.Equal(first.PdfBytes, second.PdfBytes);
        Assert.Equal(first.Sha256, second.Sha256);
        Assert.Equal(Convert.ToHexString(SHA256.HashData(first.PdfBytes)).ToLowerInvariant(), first.Sha256);
        Assert.StartsWith("%PDF-1.4", System.Text.Encoding.Latin1.GetString(first.PdfBytes));
    }
}
