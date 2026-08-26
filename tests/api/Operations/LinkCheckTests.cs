using MunicipalPlatform.Api.Modules.Operations;
using MunicipalPlatform.Api.Modules.Operations.Domain;

namespace MunicipalPlatform.Api.Tests.Operations;

public sealed class LinkCheckTests
{
    [Theory]
    [InlineData("http://127.0.0.1/")]
    [InlineData("http://10.10.0.1/")]
    [InlineData("http://192.168.1.20/")]
    [InlineData("http://localhost/")]
    public void UnsafeLiteralOrLocalTargetsAreRejected(string url)
    {
        Assert.False(LinkCheckProbeService.TryNormalizeTarget(url, out _, out var error));
        Assert.NotEmpty(error);
    }

    [Theory]
    [InlineData("https://example.com/")]
    [InlineData("https://www.deodapolis.ms.gov.br/noticias")]
    public void PublicHttpTargetsAreStructurallyAccepted(string url)
    {
        Assert.True(LinkCheckProbeService.TryNormalizeTarget(url, out var normalized, out var error));
        Assert.NotNull(normalized);
        Assert.Empty(error);
    }

    [Fact]
    public void RepeatedHttpErrorsEscalateToUnavailableAndHealthyResponseResetsFailures()
    {
        var link = new LinkCheck(Guid.NewGuid(), "https://example.com/");
        var now = DateTimeOffset.UtcNow;

        link.RecordSuccess(404, 10, now);
        Assert.Equal("DEGRADED", link.State);
        Assert.Equal(1, link.ConsecutiveFailures);
        Assert.Equal("HTTP 404", link.FailureReason);

        link.RecordSuccess(500, 12, now.AddMinutes(1));
        link.RecordSuccess(503, 14, now.AddMinutes(2));
        Assert.Equal("UNAVAILABLE", link.State);
        Assert.Equal(3, link.ConsecutiveFailures);

        link.RecordSuccess(200, 8, now.AddMinutes(3));
        Assert.Equal("HEALTHY", link.State);
        Assert.Equal(0, link.ConsecutiveFailures);
        Assert.Null(link.FailureReason);
    }

    [Fact]
    public void TransportFailureClearsStaleHttpStatus()
    {
        var link = new LinkCheck(Guid.NewGuid(), "https://example.com/");
        var now = DateTimeOffset.UtcNow;
        link.RecordSuccess(200, 8, now);

        link.RecordFailure("DNS indisponível", now.AddMinutes(1));

        Assert.Null(link.StatusCode);
        Assert.Null(link.LatencyMilliseconds);
        Assert.Equal("DEGRADED", link.State);
        Assert.Equal(1, link.ConsecutiveFailures);
    }
}
