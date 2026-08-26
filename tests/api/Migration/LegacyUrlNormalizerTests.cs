using MunicipalPlatform.Api.Modules.Migration.Domain;

namespace MunicipalPlatform.Api.Tests.Migration;

public sealed class LegacyUrlNormalizerTests
{
    [Theory]
    [InlineData("http://www.deodapolis.ms.gov.br/index.php", "/")]
    [InlineData("https://www.deodapolis.ms.gov.br/index.php?foo=bar&patt=noise", "/")]
    [InlineData("https://deodapolis.ms.gov.br/?-project-veresen-feat=raya&patt=vena-cava", "/")]
    [InlineData("https://deodapolis.ms.gov.br//e-sic/diario.php?tipo=1&utm_source=x", "/e-sic/diario.php?tipo=1")]
    [InlineData("/sec.php?tipo=10", "/sec.php?tipo=10")]
    public void NormalizePreservesMeaningfulLegacyPath(string input, string expected)
    {
        Assert.Equal(expected, LegacyUrlNormalizer.Normalize(input));
    }
}
