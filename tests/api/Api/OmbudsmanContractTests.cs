using System.Net;
using System.Net.Http.Json;

namespace MunicipalPlatform.Api.Tests.Api;

public sealed class OmbudsmanContractTests : IClassFixture<MunicipalApiFactory>
{
    private readonly MunicipalApiFactory _factory;

    public OmbudsmanContractTests(MunicipalApiFactory factory) => _factory = factory;

    [Fact]
    public async Task CitizenTracksOnlyItsOwnManifestationAndSeesPublicAnswersOnly()
    {
        await _factory.SeedAsync();
        using var citizen = CreateClient();
        var opened = await OpenTicketAsync(citizen, "Reclamação");

        using var wrongCodeResponse = await citizen.GetAsync(new Uri($"/api/v1/tickets/{opened.Protocol}?code=00000000000000000000000000000000", UriKind.Relative));
        using var missingCodeResponse = await citizen.GetAsync(new Uri($"/api/v1/tickets/{opened.Protocol}", UriKind.Relative));
        Assert.Equal(HttpStatusCode.NotFound, wrongCodeResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, missingCodeResponse.StatusCode);

        using var admin = CreateClient();
        await LoginAsync(admin);
        var ticketId = await ResolveTicketIdAsync(admin, opened.Protocol);

        using var internalNote = await admin.PostAsJsonAsync(
            new Uri($"/api/v1/admin/tickets/{ticketId}/comments", UriKind.Relative),
            new { body = "Nota interna de triagem que não pode vazar ao cidadão.", @internal = true });
        using var publicAnswer = await admin.PostAsJsonAsync(
            new Uri($"/api/v1/admin/tickets/{ticketId}/comments", UriKind.Relative),
            new { body = "Resposta oficial publicada no acompanhamento.", @internal = false });
        Assert.Equal(HttpStatusCode.Created, internalNote.StatusCode);
        Assert.Equal(HttpStatusCode.Created, publicAnswer.StatusCode);
        var answerBody = await publicAnswer.Content.ReadAsStringAsync();
        Assert.DoesNotContain("municipalityId", answerBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("authorId", answerBody, StringComparison.OrdinalIgnoreCase);

        var tracked = await citizen.GetFromJsonAsync<TrackedTicket>(new Uri($"/api/v1/tickets/{opened.Protocol}?code={opened.TrackingCode}", UriKind.Relative));
        Assert.NotNull(tracked);
        Assert.Equal("IN_PROGRESS", tracked.Status);
        Assert.Single(tracked.Comments);
        Assert.Equal("Resposta oficial publicada no acompanhamento.", tracked.Comments[0].Body);
    }

    [Fact]
    public async Task AdministrativeDetailExposesInternalHistoryAndGovernsSlaLifecycle()
    {
        await _factory.SeedAsync();
        using var citizen = CreateClient();
        var opened = await OpenTicketAsync(citizen, "Solicitação");

        using var admin = CreateClient();
        await LoginAsync(admin);
        var ticketId = await ResolveTicketIdAsync(admin, opened.Protocol);

        using var noteResponse = await admin.PostAsJsonAsync(
            new Uri($"/api/v1/admin/tickets/{ticketId}/comments", UriKind.Relative),
            new { body = "Encaminhado à secretaria responsável.", @internal = true });
        noteResponse.EnsureSuccessStatusCode();

        var detail = await admin.GetFromJsonAsync<TicketDetail>(new Uri($"/api/v1/admin/tickets/{ticketId}", UriKind.Relative));
        Assert.NotNull(detail);
        Assert.Equal(opened.Protocol, detail.Protocol);
        Assert.Contains("manifestação sintética", detail.Description, StringComparison.OrdinalIgnoreCase);
        Assert.Single(detail.Comments);
        Assert.True(detail.Comments[0].IsInternal);
        Assert.Equal("Administração Demo", detail.Comments[0].Author);
        Assert.Equal("OPEN", detail.Status);
        Assert.Equal("NORMAL", detail.Priority);

        using var priorityResponse = await admin.PostAsJsonAsync(
            new Uri($"/api/v1/admin/tickets/{ticketId}/priority", UriKind.Relative),
            new { priority = "CRITICAL" });
        priorityResponse.EnsureSuccessStatusCode();
        var escalated = await admin.GetFromJsonAsync<TicketDetail>(new Uri($"/api/v1/admin/tickets/{ticketId}", UriKind.Relative));
        Assert.NotNull(escalated);
        Assert.Equal("CRITICAL", escalated.Priority);
        Assert.Equal(escalated.OpenedAt.AddHours(4), escalated.ResolutionDueAt);

        using var resolveResponse = await admin.PostAsync(new Uri($"/api/v1/admin/tickets/{ticketId}/resolve", UriKind.Relative), null);
        resolveResponse.EnsureSuccessStatusCode();
        var resolved = await admin.GetFromJsonAsync<TicketDetail>(new Uri($"/api/v1/admin/tickets/{ticketId}", UriKind.Relative));
        Assert.NotNull(resolved);
        Assert.Equal("RESOLVED", resolved.Status);
        Assert.NotNull(resolved.ResolvedAt);

        using var reopenResponse = await admin.PostAsync(new Uri($"/api/v1/admin/tickets/{ticketId}/reopen", UriKind.Relative), null);
        reopenResponse.EnsureSuccessStatusCode();
        var reopened = await admin.GetFromJsonAsync<TicketDetail>(new Uri($"/api/v1/admin/tickets/{ticketId}", UriKind.Relative));
        Assert.NotNull(reopened);
        Assert.Equal("OPEN", reopened.Status);
        Assert.Null(reopened.ResolvedAt);
    }

    [Fact]
    public async Task AnonymousCallerCannotReachAdministrativeTicketRoutes()
    {
        await _factory.SeedAsync();
        using var citizen = CreateClient();
        var opened = await OpenTicketAsync(citizen, "Denúncia");

        using var admin = CreateClient();
        await LoginAsync(admin);
        var ticketId = await ResolveTicketIdAsync(admin, opened.Protocol);

        using var listResponse = await citizen.GetAsync(new Uri("/api/v1/admin/tickets", UriKind.Relative));
        using var detailResponse = await citizen.GetAsync(new Uri($"/api/v1/admin/tickets/{ticketId}", UriKind.Relative));
        using var resolveResponse = await citizen.PostAsync(new Uri($"/api/v1/admin/tickets/{ticketId}/resolve", UriKind.Relative), null);

        Assert.Equal(HttpStatusCode.Unauthorized, listResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, detailResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, resolveResponse.StatusCode);
    }

    private HttpClient CreateClient()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Municipality", "deodapolis");
        return client;
    }

    private static async Task<OpenedTicket> OpenTicketAsync(HttpClient client, string category)
    {
        using var response = await client.PostAsJsonAsync(
            new Uri("/api/v1/tickets", UriKind.Relative),
            new
            {
                requesterName = "Pessoa de contrato",
                contact = "contrato@example.test",
                category,
                description = "Manifestação sintética criada pelo teste de contrato da Ouvidoria municipal.",
                privacyConsent = true
            });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var opened = await response.Content.ReadFromJsonAsync<OpenedTicket>();
        Assert.NotNull(opened);
        Assert.False(string.IsNullOrWhiteSpace(opened.TrackingCode));
        return opened;
    }

    private static async Task<Guid> ResolveTicketIdAsync(HttpClient admin, string protocol)
    {
        var tickets = await admin.GetFromJsonAsync<TicketSummary[]>(new Uri("/api/v1/admin/tickets", UriKind.Relative));
        Assert.NotNull(tickets);
        var ticket = tickets.Single(entry => entry.Protocol == protocol);
        Assert.Equal("NORMAL", ticket.Priority);
        return ticket.Id;
    }

    private static async Task LoginAsync(HttpClient client)
    {
        using var response = await client.PostAsJsonAsync(
            new Uri("/api/v1/auth/login", UriKind.Relative),
            new { username = "admin.demo", password = "Demo-Local-2026!" });
        response.EnsureSuccessStatusCode();
    }

    private sealed record OpenedTicket(string Protocol, string TrackingCode, string Status);
    private sealed record TicketSummary(Guid Id, string Protocol, string Priority);
    private sealed record TrackedComment(string Body, DateTimeOffset CreatedAt);
    private sealed record TrackedTicket(string Protocol, string Status, IReadOnlyList<TrackedComment> Comments);
    private sealed record DetailComment(Guid Id, string Body, bool IsInternal, DateTimeOffset CreatedAt, string Author);
    private sealed record TicketDetail(
        Guid Id,
        string Protocol,
        string Contact,
        string Description,
        string Priority,
        string Status,
        DateTimeOffset OpenedAt,
        DateTimeOffset ResolutionDueAt,
        DateTimeOffset? ResolvedAt,
        IReadOnlyList<DetailComment> Comments);
}
