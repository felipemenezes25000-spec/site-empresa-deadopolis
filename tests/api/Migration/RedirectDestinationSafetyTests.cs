using MunicipalPlatform.Api.Modules.Migration.Domain;

namespace MunicipalPlatform.Api.Tests.Migration;

public sealed class RedirectDestinationSafetyTests
{
    [Theory]
    [InlineData("//example.test/evil")]
    [InlineData("///example.test")]
    [InlineData("/\\example.test/evil")]
    [InlineData("https://example.test/evil")]
    [InlineData("javascript:alert(1)")]
    [InlineData("//example.test\r\nSet-Cookie: a=b")]
    [InlineData("servicos")]
    [InlineData("")]
    [InlineData("   ")]
    public void RedirectNeverAcceptsADestinationThatLeavesTheMunicipalDomain(string destination)
    {
        Assert.False(RedirectRule.IsInternalDestination(destination));
        Assert.Throws<ArgumentException>(() => new RedirectRule(Guid.NewGuid(), "/portal-antigo", destination, permanent: true));
    }

    [Theory]
    [InlineData("/servicos")]
    [InlineData("/servicos/emitir-guia-iptu")]
    [InlineData("/dados-abertos/painel?ano=2026")]
    [InlineData("/noticias#destaque")]
    public void RedirectAcceptsGovernedInternalDestinations(string destination)
    {
        var rule = new RedirectRule(Guid.NewGuid(), "/portal-antigo/pagina.php?id=7", destination, permanent: true);

        Assert.True(RedirectRule.IsInternalDestination(destination));
        Assert.Equal(destination, rule.DestinationPath);
        Assert.Equal(301, rule.StatusCode);
        Assert.True(rule.IsActive);
    }

    [Fact]
    public void TemporaryRedirectKeepsTheNonPermanentStatus()
    {
        var rule = new RedirectRule(Guid.NewGuid(), "/portal-antigo/aviso", "/noticias", permanent: false);

        Assert.Equal(302, rule.StatusCode);
    }
}
