using System.Net;
using System.Net.Http.Json;

namespace MunicipalPlatform.Api.Tests.Api;

public sealed class SearchContractTests : IClassFixture<MunicipalApiFactory>
{
    private readonly MunicipalApiFactory _factory;

    public SearchContractTests(MunicipalApiFactory factory) => _factory = factory;

    [Theory]
    [InlineData("iptu")]
    [InlineData("IPTU")]
    [InlineData("tributos")]
    public async Task RankedSearchAnswersWithoutRequiringAnInstalledCulture(string query)
    {
        // The runtime image has no ICU, so any culture-aware comparison here would answer 500.
        await _factory.SeedAsync();
        using var client = CreateClient();

        using var response = await client.GetAsync(new Uri($"/api/v1/search/v2?q={Uri.EscapeDataString(query)}", UriKind.Relative));
        var payload = await response.Content.ReadFromJsonAsync<RankedSearchResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Contains(payload.Results, item => item.Title.Contains("IPTU", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task AccentedAndUnaccentedQueriesReachTheSameMunicipalContent()
    {
        await _factory.SeedAsync();
        using var client = CreateClient();

        var accented = await client.GetFromJsonAsync<RankedSearchResponse>(new Uri($"/api/v1/search/v2?q={Uri.EscapeDataString("serviços")}", UriKind.Relative));
        var plain = await client.GetFromJsonAsync<RankedSearchResponse>(new Uri("/api/v1/search/v2?q=servicos", UriKind.Relative));

        Assert.NotNull(accented);
        Assert.NotNull(plain);
        Assert.Equal(plain.Results.Select(item => item.Url), accented.Results.Select(item => item.Url));
    }

    [Fact]
    public async Task SuggestionsStayAnonymousAndBounded()
    {
        await _factory.SeedAsync();
        using var client = CreateClient();

        using var shortQuery = await client.GetAsync(new Uri("/api/v1/search/suggest?q=i", UriKind.Relative));
        using var longQuery = await client.GetAsync(new Uri($"/api/v1/search/suggest?q={new string('a', 121)}", UriKind.Relative));
        var suggestions = await client.GetFromJsonAsync<SuggestResponse>(new Uri("/api/v1/search/suggest?q=iptu", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, shortQuery.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, longQuery.StatusCode);
        Assert.NotNull(suggestions);
        Assert.True(suggestions.Suggestions.Count <= 8);
        Assert.Contains(suggestions.Suggestions, item => item.Title.Contains("IPTU", StringComparison.OrdinalIgnoreCase));
    }

    private HttpClient CreateClient()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Municipality", "deodapolis");
        return client;
    }

    private sealed record RankedResult(string Type, string Title, string Url, int Score);
    private sealed record RankedSearchResponse(string Query, bool UsedFuzzy, IReadOnlyList<RankedResult> Results);
    private sealed record Suggestion(string Type, string Title, string Url);
    private sealed record SuggestResponse(string Query, IReadOnlyList<Suggestion> Suggestions);
}
