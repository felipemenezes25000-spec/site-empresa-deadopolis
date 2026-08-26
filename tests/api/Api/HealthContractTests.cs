using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace MunicipalPlatform.Api.Tests.Api;

public sealed class HealthContractTests : IClassFixture<MunicipalApiFactory>
{
    private readonly HttpClient _client;

    public HealthContractTests(MunicipalApiFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task LiveHealthReturnsMinimalNonSensitivePayload()
    {
        using var response = await _client.GetAsync(new Uri("/health/live", UriKind.Relative));
        var payload = await response.Content.ReadFromJsonAsync<HealthPayload>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal("healthy", payload.Status);
        Assert.False(string.IsNullOrWhiteSpace(payload.CorrelationId));
    }

    private sealed record HealthPayload(string Status, string CorrelationId);
}
