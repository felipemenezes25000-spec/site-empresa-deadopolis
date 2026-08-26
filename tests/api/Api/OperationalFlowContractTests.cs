using System.Net;
using System.Net.Http.Json;

namespace MunicipalPlatform.Api.Tests.Api;

public sealed class OperationalFlowContractTests : IClassFixture<MunicipalApiFactory>
{
    private readonly MunicipalApiFactory _factory;

    public OperationalFlowContractTests(MunicipalApiFactory factory) => _factory = factory;

    [Fact]
    public async Task AuthenticatedEditorCanCreateDraftAndActionIsAudited()
    {
        await _factory.SeedAsync();
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Municipality", "deodapolis");
        await LoginAsync(client);

        using var createResponse = await client.PostAsJsonAsync(
            new Uri("/api/v1/admin/news", UriKind.Relative),
            new
            {
                title = "[DEMONSTRAÇÃO] Nova ação municipal",
                slug = "demonstracao-nova-acao-municipal",
                summary = "Conteúdo sintético para validar o fluxo editorial.",
                body = "Texto de demonstração sem valor de comunicado oficial.",
                coverImageUrl = (string?)null,
                coverImageAlt = (string?)null,
                isFeatured = false
            });
        var created = await createResponse.Content.ReadFromJsonAsync<CreatedArticle>();
        using var auditResponse = await client.GetAsync(new Uri("/api/v1/admin/audit", UriKind.Relative));
        var auditBody = await auditResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.NotNull(created);
        Assert.Equal("DRAFT", created.Status);
        Assert.Equal(HttpStatusCode.OK, auditResponse.StatusCode);
        Assert.Contains("content.news.created", auditBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AnonymousCitizenCanOpenTicketWithConsentAndReceivesProtocol()
    {
        await _factory.SeedAsync();
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Municipality", "deodapolis");

        using var response = await client.PostAsJsonAsync(
            new Uri("/api/v1/tickets", UriKind.Relative),
            new
            {
                requesterName = "Cidadão de demonstração",
                contact = "cidadao@example.test",
                category = "SOLICITACAO",
                description = "Solicitação sintética para demonstrar a geração de protocolo.",
                privacyConsent = true
            });
        var created = await response.Content.ReadFromJsonAsync<CreatedTicket>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(created);
        Assert.StartsWith("DEO-", created.Protocol, StringComparison.Ordinal);
        Assert.Equal("OPEN", created.Status);
        Assert.True(created.ResolutionDueAt > DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task TicketWithoutPrivacyConsentIsRejected()
    {
        await _factory.SeedAsync();
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Municipality", "deodapolis");

        using var response = await client.PostAsJsonAsync(
            new Uri("/api/v1/tickets", UriKind.Relative),
            new
            {
                requesterName = "Teste",
                contact = "teste@example.test",
                category = "SOLICITACAO",
                description = "Descrição suficientemente detalhada para validação.",
                privacyConsent = false
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task LoginAsync(HttpClient client)
    {
        using var response = await client.PostAsJsonAsync(
            new Uri("/api/v1/auth/login", UriKind.Relative),
            new { username = "admin.demo", password = "Demo-Local-2026!" });
        response.EnsureSuccessStatusCode();
    }

    private sealed record CreatedArticle(Guid Id, string Status);
    private sealed record CreatedTicket(string Protocol, string Status, DateTimeOffset ResolutionDueAt);
}
