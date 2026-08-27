using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using MunicipalPlatform.Api.Infrastructure.Persistence;
using MunicipalPlatform.Api.Modules.Content.Domain;
using MunicipalPlatform.Api.Modules.Migration.Domain;
using MunicipalPlatform.Api.Modules.Services.Domain;
using MunicipalPlatform.Api.Modules.Transparency.Domain;
using MunicipalPlatform.Api.Platform.Tenancy;
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
    public async Task UniversalSearchIncludesGovernedPagesDepartmentsDatasetsAndDocuments()
    {
        await _factory.SeedAsync();
        Guid documentId;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var tenant = scope.ServiceProvider.GetRequiredService<TenantContext>();
            tenant.SetMunicipality(MunicipalApiFactory.MunicipalityId, "deodapolis");
            var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var actor = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

            database.Departments.Add(new Department(MunicipalApiFactory.MunicipalityId, "Secretaria de Pesquisa Cidadã", "pesquisa-cidada", "SPC", 1));

            var dataset = new Dataset(MunicipalApiFactory.MunicipalityId, "Indicadores de Pesquisa Cidadã", "indicadores-pesquisa-cidada", "Série pública para pesquisa cidadã.", "Administração", "SPC", "Dados abertos", "Mensal");
            dataset.Publish(DateTimeOffset.UtcNow);
            database.Datasets.Add(dataset);

            var page = new PortalResource(MunicipalApiFactory.MunicipalityId, "PAGE", "acesso-a-informacao", "Pesquisa Cidadã e Acesso à Informação", "Orientações públicas para pesquisa cidadã.", "{}", 1, actor);
            page.Publish(actor, DateTimeOffset.UtcNow);
            database.PortalResources.Add(page);

            var document = new PublicDocument(
                MunicipalApiFactory.MunicipalityId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                "LEGISLACAO", "LEIS_ORDINARIAS", "Lei da Pesquisa Cidadã", "Norma pública de pesquisa cidadã.",
                "1/2026", null, "2026", new DateOnly(2026, 1, 1), "SPC", "PDF",
                "https://legacy.example.test/lei-pesquisa.pdf", "/lei-pesquisa.pdf", "lei-pesquisa.pdf",
                "application/pdf", 1024, new string('a', 64));
            document.Publish(DateTimeOffset.UtcNow);
            documentId = document.Id;
            database.PublicDocuments.Add(document);
            await database.SaveChangesAsync();
        }

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Municipality", "deodapolis");
        using var response = await client.GetAsync(new Uri("/api/v1/search?q=pesquisa%20cidada", UriKind.Relative));
        var payload = await response.Content.ReadFromJsonAsync<SearchPayload>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Contains(payload.Results, item => item.Type == "DEPARTMENT" && item.Url == "/secretarias/pesquisa-cidada");
        Assert.Contains(payload.Results, item => item.Type == "DATASET" && item.Url == "/dados-abertos/indicadores-pesquisa-cidada");
        Assert.Contains(payload.Results, item => item.Type == "PAGE" && item.Url == "/acesso-a-informacao");
        Assert.Contains(payload.Results, item => item.Type == "DOCUMENT" && item.Url.EndsWith($"/{documentId}/download", StringComparison.Ordinal));
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
    private sealed record SearchPayload(SearchItemPayload[] Results);
    private sealed record SearchItemPayload(string Type, string Title, string Description, string Url);
}
