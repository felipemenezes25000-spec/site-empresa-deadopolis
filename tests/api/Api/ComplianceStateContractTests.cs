using System.Net.Http.Json;
using System.Text.Json;

namespace MunicipalPlatform.Api.Tests.Api;

public sealed class ComplianceStateContractTests : IClassFixture<MunicipalApiFactory>
{
    private static readonly string[] Vocabulary = ["CONFIGURED", "DEGRADED", "UNAVAILABLE", "NOT_CONFIGURED"];
    private static readonly string[] HonestProviderStates = ["DEMO_ONLY", "NOT_CONFIGURED", "UNAVAILABLE", "DEGRADED", "DEVELOPMENT_ONLY"];
    private readonly MunicipalApiFactory _factory;

    public ComplianceStateContractTests(MunicipalApiFactory factory) => _factory = factory;

    [Theory]
    [InlineData("/api/v1/admin/integrations")]
    [InlineData("/api/v1/admin/dashboard")]
    [InlineData("/api/v1/admin/compliance")]
    public async Task EveryAdministrativeSurfacePublishesTheSameIntegrationVocabulary(string route)
    {
        await _factory.SeedAsync();
        using var client = await CreateAuthenticatedClientAsync();

        var payload = await client.GetFromJsonAsync<JsonElement>(new Uri(route, UriKind.Relative));
        var integrations = payload.ValueKind == JsonValueKind.Array ? payload : payload.GetProperty("integrations");

        Assert.NotEmpty(integrations.EnumerateArray());
        foreach (var integration in integrations.EnumerateArray())
        {
            var state = integration.GetProperty("state");
            // A raw enum serializes as a number and renders as "3" on the compliance screen.
            Assert.Equal(JsonValueKind.String, state.ValueKind);
            Assert.Contains(state.GetString(), Vocabulary);
        }
    }

    [Fact]
    public async Task IntegrationListingDoesNotLeakTenantIdentifiers()
    {
        await _factory.SeedAsync();
        using var client = await CreateAuthenticatedClientAsync();

        using var response = await client.GetAsync(new Uri("/api/v1/admin/integrations", UriKind.Relative));
        var body = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("municipalityId", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("NOT_CONFIGURED", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ComplianceNeverReportsAConfiguredProviderThatIsOnlyDemonstration()
    {
        await _factory.SeedAsync();
        using var client = await CreateAuthenticatedClientAsync();

        var payload = await client.GetFromJsonAsync<JsonElement>(new Uri("/api/v1/admin/compliance", UriKind.Relative));
        var providers = payload.GetProperty("providers");

        foreach (var name in new[] { "digitalSignature", "institutionalEmail", "malwareScanner" })
        {
            var state = providers.GetProperty(name).GetProperty("state").GetString();
            Assert.NotEqual("CONFIGURED", state);
            Assert.Contains(state, HonestProviderStates);
        }
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Municipality", "deodapolis");
        using var login = await client.PostAsJsonAsync(
            new Uri("/api/v1/auth/login", UriKind.Relative),
            new { username = "admin.demo", password = "Demo-Local-2026!" });
        login.EnsureSuccessStatusCode();
        return client;
    }
}
