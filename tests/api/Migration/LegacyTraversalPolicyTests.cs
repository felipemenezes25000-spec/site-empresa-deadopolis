using MunicipalPlatform.Api.Modules.Migration.Services;

namespace MunicipalPlatform.Api.Tests.Migration;

public sealed class LegacyTraversalPolicyTests
{
    [Fact]
    public void PaginationOnSameLegacyPathKeepsStructuralDepth()
    {
        var current = new Uri("https://www.deodapolis.ms.gov.br/noticias.php?page=10");
        var candidate = new Uri("https://www.deodapolis.ms.gov.br/noticias.php?page=11");

        Assert.Equal(4, LegacyTraversalPolicy.GetNextDepth(current, candidate, 4));
    }

    [Fact]
    public void DifferentLegacyPathIncrementsStructuralDepth()
    {
        var current = new Uri("https://www.deodapolis.ms.gov.br/noticias.php?page=10");
        var candidate = new Uri("https://www.deodapolis.ms.gov.br/exibe23.php?id=1469");

        Assert.Equal(5, LegacyTraversalPolicy.GetNextDepth(current, candidate, 4));
    }

    [Fact]
    public void SamePathAtMaximumDepthCanStillTraverseQueryPagination()
    {
        var current = new Uri("https://www.deodapolis.ms.gov.br/e-sic/editais_licitacoes.php?tipo=1&page=70");
        var candidate = new Uri("https://www.deodapolis.ms.gov.br/e-sic/editais_licitacoes.php?tipo=1&page=71");

        Assert.Equal(10, LegacyTraversalPolicy.GetNextDepth(current, candidate, 10));
    }

    [Fact]
    public void EmptyLegacyNewsPageStopsTheUnboundedNextPageChain()
    {
        var current = new Uri("https://www.deodapolis.ms.gov.br/noticias.php?q=&tipo=all&page=1655");
        const string emptyPage = "<nav><a href='noticias.php?q=&tipo=all&page=1656'>Próxima</a></nav>";
        const string contentPage = "<a href='exibe23.php?id=1469'>Notícia</a><a href='noticias.php?page=2'>Próxima</a>";

        Assert.False(LegacyTraversalPolicy.ShouldContinuePagination(current, emptyPage));
        Assert.False(LegacyTraversalPolicy.ShouldContinuePagination(
            new Uri("https://www.deodapolis.ms.gov.br/noticias25.php?tipo=17&pagina=99"),
            emptyPage));
        Assert.True(LegacyTraversalPolicy.ShouldContinuePagination(current, contentPage));
        Assert.True(LegacyTraversalPolicy.ShouldContinuePagination(
            new Uri("https://www.deodapolis.ms.gov.br/e-sic/editais_licitacoes.php?pagina=70"),
            emptyPage));
    }
}
