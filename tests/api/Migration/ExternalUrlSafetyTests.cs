using System.Net;
using MunicipalPlatform.Api.Modules.Migration.Security;
using MunicipalPlatform.Api.Modules.Migration.Services;

namespace MunicipalPlatform.Api.Tests.Migration;

public sealed class ExternalUrlSafetyTests
{
    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("10.1.2.3")]
    [InlineData("172.16.0.1")]
    [InlineData("172.31.255.255")]
    [InlineData("192.168.1.10")]
    [InlineData("169.254.1.1")]
    [InlineData("100.64.1.1")]
    [InlineData("224.0.0.1")]
    [InlineData("::1")]
    [InlineData("fc00::1")]
    [InlineData("fe80::1")]
    public void PrivateAndReservedAddressesAreRejected(string address)
    {
        Assert.False(ExternalUrlSafety.IsPublicAddress(IPAddress.Parse(address)));
    }

    [Theory]
    [InlineData("8.8.8.8")]
    [InlineData("1.1.1.1")]
    [InlineData("2606:4700:4700::1111")]
    public void PublicAddressesAreAllowed(string address)
    {
        Assert.True(ExternalUrlSafety.IsPublicAddress(IPAddress.Parse(address)));
    }

    [Fact]
    public void UriMustStayOnExactAuthorizedHost()
    {
        Assert.True(ExternalUrlSafety.IsAllowedUri(new Uri("https://www.deodapolis.ms.gov.br/noticias"), "www.deodapolis.ms.gov.br"));
        Assert.False(ExternalUrlSafety.IsAllowedUri(new Uri("https://evil.example/noticias"), "www.deodapolis.ms.gov.br"));
        Assert.False(ExternalUrlSafety.IsAllowedUri(new Uri("https://user:pass@www.deodapolis.ms.gov.br/noticias"), "www.deodapolis.ms.gov.br"));
        Assert.False(ExternalUrlSafety.IsAllowedUri(new Uri("https://www.deodapolis.ms.gov.br:8443/noticias"), "www.deodapolis.ms.gov.br"));
    }

    [Theory]
    [InlineData("text/html", 200, false, "MIGRATE")]
    [InlineData("application/pdf", 200, false, "MIGRATE")]
    [InlineData("application/json", 200, false, "INTEGRATE")]
    [InlineData("text/html", 301, true, "REDIRECT")]
    [InlineData("text/html", 404, false, "IGNORE_WITH_REASON")]
    public void ContentClassificationIsDeterministic(string mediaType, int statusCode, bool redirect, string expected)
    {
        Assert.Equal(expected, LegacyContentClassifier.Classify(mediaType, statusCode, redirect));
    }
}
