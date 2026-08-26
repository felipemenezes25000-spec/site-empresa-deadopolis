using MunicipalPlatform.Api.Modules.Search.Domain;

namespace MunicipalPlatform.Api.Tests.Search;

public sealed class SearchNormalizerTests
{
    [Theory]
    [InlineData("  Emissão de Nota Fiscal  ", "emissao de nota fiscal")]
    [InlineData("SAÚDE   da família", "saude da familia")]
    [InlineData("Licenciamento\tAmbiental", "licenciamento ambiental")]
    public void NormalizeRemovesAccentsCaseAndRepeatedWhitespace(string input, string expected)
    {
        Assert.Equal(expected, SearchNormalizer.Normalize(input));
    }
}
