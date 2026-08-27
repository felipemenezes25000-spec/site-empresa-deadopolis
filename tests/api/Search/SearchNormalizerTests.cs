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

    [Fact]
    public void TokenizeTreatsMunicipalDocumentPunctuationAsSeparators()
    {
        Assert.Equal(["processo", "12", "2026"], SearchNormalizer.Tokenize("Processo 12/2026"));
    }

    [Fact]
    public void ScorePrefersExactAndPrefixMatches()
    {
        var exact = SearchNormalizer.Score("IPTU", "IPTU", "Consulta de tributos municipais");
        var prefix = SearchNormalizer.Score("IPTU", "IPTU Digital", "Consulta de tributos municipais");
        var descriptionOnly = SearchNormalizer.Score("IPTU", "Tributos municipais", "Consulta e emissão de IPTU");

        Assert.True(exact > prefix);
        Assert.True(prefix > descriptionOnly);
    }

    [Theory]
    [InlineData("matricla", "Matrícula escolar")]
    [InlineData("licitacao", "Licitações e contratos")]
    [InlineData("saude", "Secretaria de Saúde")]
    public void ScoreRecoversCommonTyposAndInflections(string query, string title)
    {
        Assert.True(SearchNormalizer.Score(query, title) >= 160);
    }

    [Fact]
    public void ScoreRejectsUnrelatedTerms()
    {
        Assert.Equal(0, SearchNormalizer.Score("vacina", "Licitações e contratos", "Compras públicas"));
    }
}
