using MunicipalPlatform.Api.Modules.Content.Domain;

namespace MunicipalPlatform.Api.Tests.Content;

public sealed class PortalResourceTests
{
    [Fact]
    public void UpdateIncrementsVersionForOptimisticConcurrency()
    {
        var actor = Guid.NewGuid();
        var resource = new PortalResource(Guid.NewGuid(), "PAGE", "sobre", "Sobre", "Resumo", "{}", 1, actor);
        var initialVersion = resource.Version;
        resource.Update("Sobre a Prefeitura", "Resumo atualizado", "{\"section\":1}", 2, null, null, actor, DateTimeOffset.UtcNow);
        Assert.Equal(initialVersion + 1, resource.Version);
        resource.Publish(actor, DateTimeOffset.UtcNow);
        Assert.Equal("PUBLISHED", resource.Status);
    }

    [Fact]
    public void PagePayloadAcceptsGovernedStructuredBlocks()
    {
        var payload = """
            {"blocks":[{"id":"banner-1","type":"Banner","title":"Vacinação","reference":"/servicos/vacinacao","imageUrl":"/api/v1/media/11111111-1111-1111-1111-111111111111","imageAlt":"Equipe de vacinação","items":[],"enabled":true}]}
            """;

        var resource = new PortalResource(Guid.NewGuid(), "PAGE", "saude", "Saúde", "Resumo", payload, 1, Guid.NewGuid());

        Assert.Equal(payload, resource.PayloadJson);
    }

    [Theory]
    [InlineData("{\"blocks\":[{\"type\":\"ArbitraryHtml\"}]}")]
    [InlineData("{\"blocks\":[{\"type\":\"Banner\",\"reference\":\"javascript:alert(1)\"}]}")]
    [InlineData("{\"blocks\":[{\"type\":\"Gallery\",\"items\":[{\"mediaUrl\":\"https://images.example.test/photo.jpg\",\"mediaAlt\":\"Externa\"}]}]}")]
    [InlineData("{\"blocks\":[{\"type\":\"Banner\",\"imageUrl\":\"/api/v1/media/11111111-1111-1111-1111-111111111111\"}]}")]
    public void PagePayloadRejectsUnsafeOrIncompleteBlocks(string payload)
    {
        var exception = Assert.Throws<ArgumentException>(() => new PortalResource(
            Guid.NewGuid(), "PAGE", "conteudo", "Conteúdo", "Resumo", payload, 1, Guid.NewGuid()));

        Assert.Contains("bloco", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
