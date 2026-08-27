using System.Net;
using System.Net.Http.Json;
using Xunit.Abstractions;

namespace MunicipalPlatform.Api.Tests.Api;

public sealed class PortalContractTests : IClassFixture<MunicipalApiFactory>
{
    private readonly MunicipalApiFactory _factory;
    private readonly ITestOutputHelper _output;

    public PortalContractTests(MunicipalApiFactory factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
    }

    [Fact]
    public async Task HomeReturnsPublishedTenantScopedContentAndHonestIntegrationState()
    {
        await _factory.SeedAsync();
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Municipality", "deodapolis");

        using var response = await client.GetAsync(new Uri("/api/v1/portal/home", UriKind.Relative));
        _output.WriteLine(await response.Content.ReadAsStringAsync());
        var payload = await response.Content.ReadFromJsonAsync<HomePayload>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal("Prefeitura Municipal de Deodápolis", payload.Municipality.Name);
        Assert.Contains(payload.FeaturedServices, service => service.Slug == "emitir-guia-iptu");
        Assert.Contains(payload.LatestNews, article => article.Slug == "feira-de-servicos");
        Assert.Contains(payload.Integrations, integration => integration.State == "NOT_CONFIGURED");
    }

    [Fact]
    public async Task NewsCanBeFilteredByEditorialCategory()
    {
        await _factory.SeedAsync();
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Municipality", "deodapolis");

        using var matchingResponse = await client.GetAsync(new Uri("/api/v1/news?category=PREFEITURA", UriKind.Relative));
        using var otherResponse = await client.GetAsync(new Uri("/api/v1/news?category=SAUDE", UriKind.Relative));
        var matching = await matchingResponse.Content.ReadFromJsonAsync<NewsPayload[]>();
        var other = await otherResponse.Content.ReadFromJsonAsync<NewsPayload[]>();

        Assert.Equal(HttpStatusCode.OK, matchingResponse.StatusCode);
        Assert.Contains(matching ?? [], article => article.Slug == "feira-de-servicos" && article.Category == "PREFEITURA");
        Assert.Empty(other ?? []);
    }

    [Fact]
    public async Task AuditEndpointRejectsAnonymousAccess()
    {
        await _factory.SeedAsync();
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Municipality", "deodapolis");

        using var response = await client.GetAsync(new Uri("/api/v1/admin/audit", UriKind.Relative));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task LoginCreatesSessionThatCanReadAudit()
    {
        await _factory.SeedAsync();
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Municipality", "deodapolis");

        using var loginResponse = await client.PostAsJsonAsync(
            new Uri("/api/v1/auth/login", UriKind.Relative),
            new { username = "admin.demo", password = "Demo-Local-2026!" });
        using var auditResponse = await client.GetAsync(new Uri("/api/v1/admin/audit", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, auditResponse.StatusCode);
    }

    [Fact]
    public async Task LoginRejectsInvalidPasswordWithoutRevealingWhichFieldFailed()
    {
        await _factory.SeedAsync();
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Municipality", "deodapolis");

        using var response = await client.PostAsJsonAsync(
            new Uri("/api/v1/auth/login", UriKind.Relative),
            new { username = "admin.demo", password = "wrong" });
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("Credenciais inválidas", body, StringComparison.Ordinal);
        Assert.DoesNotContain("password", body, StringComparison.OrdinalIgnoreCase);
    }

    private sealed record HomePayload(
        MunicipalityPayload Municipality,
        ServicePayload[] FeaturedServices,
        NewsPayload[] LatestNews,
        IntegrationPayload[] Integrations);

    private sealed record MunicipalityPayload(string Name);
    private sealed record ServicePayload(string Slug);
    private sealed record NewsPayload(string Slug, string Category);
    private sealed record IntegrationPayload(string State);
}
