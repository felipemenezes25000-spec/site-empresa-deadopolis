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

    [Theory]
    [InlineData("saúde", "saude")]
    [InlineData("SAÚDE", "saude")]
    [InlineData("Educação", "educacao")]
    [InlineData("Licitações", "licitacoes")]
    [InlineData("Órgão", "orgao")]
    [InlineData("Matrícula", "matricula")]
    public void NormalizeFoldsAccentsWithoutDependingOnUnicodeNormalization(string input, string expected)
    {
        // The Alpine runtime image starts in globalization-invariant mode, where String.Normalize
        // silently returns the input unchanged. The folding must not rely on it.
        Assert.Equal(expected, SearchNormalizer.Normalize(input));
        Assert.Equal(expected, SearchNormalizer.Normalize(input.Normalize(System.Text.NormalizationForm.FormD)));
    }

    [Fact]
    public void NormalizeTreatsAccentedAndUnaccentedQueriesAsTheSameTerm()
    {
        Assert.Equal(SearchNormalizer.Normalize("saude"), SearchNormalizer.Normalize("saúde"));
        Assert.True(SearchNormalizer.IsDirectMatch("saúde", "Secretaria Municipal de Saúde"));
        Assert.True(SearchNormalizer.IsDirectMatch("EDUCAÇÃO", "Secretaria Municipal de Educacao"));
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
