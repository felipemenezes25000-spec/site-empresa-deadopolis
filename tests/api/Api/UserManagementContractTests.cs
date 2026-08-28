using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MunicipalPlatform.Api.Infrastructure.Persistence;
using MunicipalPlatform.Api.Platform.Tenancy;

namespace MunicipalPlatform.Api.Tests.Api;

public sealed class UserManagementContractTests : IClassFixture<MunicipalApiFactory>
{
    private readonly MunicipalApiFactory _factory;

    public UserManagementContractTests(MunicipalApiFactory factory) => _factory = factory;

    [Fact]
    public async Task AdministratorCanGovernUserLifecycleAndInvalidateEveryPriorSession()
    {
        await _factory.SeedAsync();
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Municipality", "deodapolis");
        await LoginAsync(client);
        var username = $"editor.{Guid.NewGuid():N}";
        const string temporaryPassword = "Temporary-Strong-2026!";

        using var createResponse = await client.PostAsJsonAsync(
            new Uri("/api/v1/admin/users", UriKind.Relative),
            new { username, displayName = "Editor de contrato", role = "COMMUNICATION", temporaryPassword });
        var created = await createResponse.Content.ReadFromJsonAsync<UserResponse>();
        var createBody = await createResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.NotNull(created);
        Assert.DoesNotContain(temporaryPassword, createBody, StringComparison.Ordinal);
        Assert.DoesNotContain("passwordHash", createBody, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, created.SessionVersion);

        using var listResponse = await client.GetAsync(new Uri("/api/v1/admin/users", UriKind.Relative));
        var users = await listResponse.Content.ReadFromJsonAsync<UserResponse[]>();
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        Assert.Contains(users!, user => user.Id == created.Id && user.Role == "COMMUNICATION");

        using var governedClient = _factory.CreateClient();
        governedClient.DefaultRequestHeaders.Add("X-Municipality", "deodapolis");
        await LoginAsync(governedClient, username, temporaryPassword);

        using var roleResponse = await client.PutAsJsonAsync(
            new Uri($"/api/v1/admin/users/{created.Id}/role", UriKind.Relative),
            new { role = "SUPER_ADMIN" });
        var roleChanged = await roleResponse.Content.ReadFromJsonAsync<UserResponse>();
        Assert.Equal(HttpStatusCode.OK, roleResponse.StatusCode);
        Assert.Equal("SUPER_ADMIN", roleChanged?.Role);
        Assert.Equal(1, roleChanged?.SessionVersion);
        Assert.Equal(HttpStatusCode.Unauthorized, (await governedClient.GetAsync(new Uri("/api/v1/auth/me", UriKind.Relative))).StatusCode);

        await LoginAsync(governedClient, username, temporaryPassword);
        using var revokeResponse = await client.PostAsync(
            new Uri($"/api/v1/admin/users/{created.Id}/sessions/revoke", UriKind.Relative),
            null);
        var revoked = await revokeResponse.Content.ReadFromJsonAsync<UserResponse>();
        Assert.Equal(HttpStatusCode.OK, revokeResponse.StatusCode);
        Assert.Equal(2, revoked?.SessionVersion);
        Assert.Equal(HttpStatusCode.Unauthorized, (await governedClient.GetAsync(new Uri("/api/v1/auth/me", UriKind.Relative))).StatusCode);

        await LoginAsync(governedClient, username, temporaryPassword);

        using var deactivateResponse = await client.PostAsJsonAsync(
            new Uri($"/api/v1/admin/users/{created.Id}/state", UriKind.Relative),
            new { active = false });
        var deactivated = await deactivateResponse.Content.ReadFromJsonAsync<UserResponse>();
        Assert.Equal(HttpStatusCode.OK, deactivateResponse.StatusCode);
        Assert.False(deactivated?.IsActive);
        Assert.Equal(3, deactivated?.SessionVersion);
        Assert.Equal(HttpStatusCode.Unauthorized, (await governedClient.GetAsync(new Uri("/api/v1/auth/me", UriKind.Relative))).StatusCode);

        await using var scope = _factory.Services.CreateAsyncScope();
        var tenant = scope.ServiceProvider.GetRequiredService<TenantContext>();
        tenant.SetMunicipality(MunicipalApiFactory.MunicipalityId, "deodapolis");
        var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var actions = await database.AuditEvents
            .Where(item => item.ResourceId == created.Id.ToString())
            .Select(item => item.Action)
            .ToListAsync();
        Assert.Contains("identity.user.created", actions);
        Assert.Contains("identity.user.role.assigned", actions);
        Assert.Contains("identity.user.sessions.revoked", actions);
        Assert.Contains("identity.user.state.changed", actions);
    }

    [Fact]
    public async Task AdministrativeRouteCannotSilentlyDestroyCurrentSession()
    {
        await _factory.SeedAsync();
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Municipality", "deodapolis");
        await LoginAsync(client);
        var current = await client.GetFromJsonAsync<CurrentUserResponse>(new Uri("/api/v1/auth/me", UriKind.Relative));
        Assert.NotNull(current);

        using var roleResponse = await client.PutAsJsonAsync(
            new Uri($"/api/v1/admin/users/{current.Id}/role", UriKind.Relative),
            new { role = "COMMUNICATION" });
        using var stateResponse = await client.PostAsJsonAsync(
            new Uri($"/api/v1/admin/users/{current.Id}/state", UriKind.Relative),
            new { active = false });
        using var revokeResponse = await client.PostAsync(
            new Uri($"/api/v1/admin/users/{current.Id}/sessions/revoke", UriKind.Relative),
            null);

        Assert.Equal(HttpStatusCode.Conflict, roleResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, stateResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, revokeResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(new Uri("/api/v1/auth/me", UriKind.Relative))).StatusCode);
    }

    private static async Task LoginAsync(HttpClient client)
        => await LoginAsync(client, "admin.demo", "Demo-Local-2026!");

    private static async Task LoginAsync(HttpClient client, string username, string password)
    {
        using var response = await client.PostAsJsonAsync(
            new Uri("/api/v1/auth/login", UriKind.Relative),
            new { username, password });
        response.EnsureSuccessStatusCode();
    }

    private sealed record UserResponse(Guid Id, string Username, string DisplayName, string Role, bool IsActive, int SessionVersion);
    private sealed record CurrentUserResponse(Guid Id);
}
