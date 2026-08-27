using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

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
    public async Task AuthenticatedEditorCanReadAndUpdateDraftWithExpectedVersion()
    {
        await _factory.SeedAsync();
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Municipality", "deodapolis");
        await LoginAsync(client);

        using var createResponse = await client.PostAsJsonAsync(
            new Uri("/api/v1/admin/news", UriKind.Relative),
            new
            {
                title = "[DEMONSTRAÇÃO] Rascunho editável",
                slug = $"rascunho-editavel-{Guid.NewGuid():N}",
                summary = "Resumo original para o contrato de edição.",
                body = "Corpo original suficientemente descritivo.",
                category = "GERAL",
                coverImageUrl = (string?)null,
                coverImageAlt = (string?)null,
                isFeatured = false
            });
        var created = await createResponse.Content.ReadFromJsonAsync<EditableArticle>();
        Assert.NotNull(created);

        using var readResponse = await client.GetAsync(new Uri($"/api/v1/admin/news/{created.Id}", UriKind.Relative));
        var read = await readResponse.Content.ReadFromJsonAsync<EditableArticle>();
        Assert.Equal(HttpStatusCode.OK, readResponse.StatusCode);
        Assert.Equal(created.Id, read?.Id);

        using var updateResponse = await client.PutAsJsonAsync(
            new Uri($"/api/v1/admin/news/{created.Id}", UriKind.Relative),
            new
            {
                title = "[DEMONSTRAÇÃO] Rascunho atualizado",
                summary = "Resumo atualizado pelo contrato autenticado.",
                body = "Corpo atualizado sem editar banco manualmente.",
                category = "SAUDE",
                coverImageUrl = (string?)null,
                coverImageAlt = (string?)null,
                isFeatured = true,
                expectedVersion = created.Version
            });
        var updated = await updateResponse.Content.ReadFromJsonAsync<EditableArticle>();

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        Assert.Equal("[DEMONSTRAÇÃO] Rascunho atualizado", updated?.Title);
        Assert.Equal(created.Version + 1, updated?.Version);
        Assert.True(updated?.IsFeatured);
    }

    [Fact]
    public async Task NewsCoverMustReferenceAnApprovedGovernedImage()
    {
        await _factory.SeedAsync();
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Municipality", "deodapolis");
        await LoginAsync(client);

        using var response = await client.PostAsJsonAsync(
            new Uri("/api/v1/admin/news", UriKind.Relative),
            new
            {
                title = "[DEMONSTRAÇÃO] Capa externa rejeitada",
                slug = $"capa-externa-{Guid.NewGuid():N}",
                summary = "A capa precisa vir da biblioteca governada.",
                body = "Conteúdo sintético para validar a regra de mídia.",
                category = "GERAL",
                coverImageUrl = "https://images.example.test/capa.jpg",
                coverImageAlt = "Imagem externa",
                isFeatured = false
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("biblioteca de mídia", await response.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ComplianceCenterReportsRuntimeStatesAndPersistedEvidence()
    {
        await _factory.SeedAsync();
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Municipality", "deodapolis");
        await LoginAsync(client);

        using var response = await client.GetAsync(new Uri("/api/v1/admin/compliance", UriKind.Relative));
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(body.RootElement.GetProperty("readiness").GetProperty("databaseReady").GetBoolean());
        Assert.False(string.IsNullOrWhiteSpace(body.RootElement.GetProperty("providers").GetProperty("storage").GetProperty("state").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(body.RootElement.GetProperty("providers").GetProperty("mediaVariants").GetProperty("webp").GetProperty("state").GetString()));
        Assert.Equal(JsonValueKind.Array, body.RootElement.GetProperty("externalDependencies").ValueKind);
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
    private sealed record EditableArticle(Guid Id, string Title, int Version, bool IsFeatured);
    private sealed record CreatedTicket(string Protocol, string Status, DateTimeOffset ResolutionDueAt);
}
