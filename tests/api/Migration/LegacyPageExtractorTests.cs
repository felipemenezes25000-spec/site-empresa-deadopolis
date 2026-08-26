using System.Text;
using MunicipalPlatform.Api.Modules.Migration.Services;

namespace MunicipalPlatform.Api.Tests.Migration;

public sealed class LegacyPageExtractorTests
{
    [Fact]
    public void ExtractRemovesExecutableMarkupAndPreservesReadableText()
    {
        var html = """
            <html>
              <head><title>Portal &amp; Serviços</title><style>.hidden{display:none}</style></head>
              <body>
                <script>window.evil = true;</script>
                <h1>Atendimento ao cidadão</h1>
                <p>Consulte &nbsp; serviços municipais.</p>
                <noscript>fallback técnico</noscript>
              </body>
            </html>
            """;

        var result = LegacyPageExtractor.Extract(Encoding.UTF8.GetBytes(html));

        Assert.Equal("Portal & Serviços", result.Title);
        Assert.Contains("Atendimento ao cidadão", result.Text, StringComparison.Ordinal);
        Assert.Contains("Consulte serviços municipais.", result.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("window.evil", result.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("display:none", result.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("fallback técnico", result.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void ExtractNormalizesBlocksIntoStableLines()
    {
        var html = "<div>Primeira linha</div><div>Segunda <strong>linha</strong></div><br><p>Terceira</p>";

        var result = LegacyPageExtractor.Extract(Encoding.UTF8.GetBytes(html));

        Assert.Equal("Primeira linha\nSegunda linha\nTerceira", result.Text);
    }
}
