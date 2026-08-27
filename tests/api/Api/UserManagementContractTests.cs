using System.Net;
using System.Net.Http.Json;

namespace MunicipalPlatform.Api.Tests.Api;

public sealed class UserManagementContractTests : IClassFixture<MunicipalApiFactory>
{
    private readonly MunicipalApiFactory _factory;

    public UserManagementContractTests(MunicipalApiFactory factory) => _factory = factory;

    [Fact]
    public async Task AdministratorCanCreateListAndDeactivateUserWithoutExposingPassword()
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

        using var listResponse = await client.GetAsync(new Uri("/api/v1/admin/users", UriKind.Relative));
        var users = await listResponse.Content.ReadFromJsonAsync<UserResponse[]>();
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        Assert.Contains(users!, user => user.Id == created.Id && user.Role == "COMMUNICATION");

        using var deactivateResponse = await client.PostAsJsonAsync(
            new Uri($"/api/v1/admin/users/{created.Id}/state", UriKind.Relative),
            new { active = false });
        var deactivated = await deactivateResponse.Content.ReadFromJsonAsync<UserResponse>();
        Assert.Equal(HttpStatusCode.OK, deactivateResponse.StatusCode);
        Assert.False(deactivated?.IsActive);
    }

    private static async Task LoginAsync(HttpClient client)
    {
        using var response = await client.PostAsJsonAsync(
            new Uri("/api/v1/auth/login", UriKind.Relative),
            new { username = "admin.demo", password = "Demo-Local-2026!" });
        response.EnsureSuccessStatusCode();
    }

    private sealed record UserResponse(Guid Id, string Username, string DisplayName, string Role, bool IsActive);
}
