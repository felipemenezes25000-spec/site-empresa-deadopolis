using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MunicipalPlatform.Api.Infrastructure.Persistence;
using MunicipalPlatform.Api.Modules.Identity.Domain;
using MunicipalPlatform.Api.Modules.Migration.Domain;
using MunicipalPlatform.Api.Platform.Tenancy;

namespace MunicipalPlatform.Api.Tests.Api;

public sealed class MigrationInventoryContractTests : IClassFixture<MunicipalApiFactory>
{
    private readonly MunicipalApiFactory _factory;

    public MigrationInventoryContractTests(MunicipalApiFactory factory) => _factory = factory;

    [Fact]
    public async Task InventoryUrlsArePagedAndFilterableBeyondLegacyDetailLimit()
    {
        await _factory.SeedAsync();
        var jobId = await SeedInventoryAsync();
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Municipality", "deodapolis");
        await LoginAsync(client);

        using var response = await client.GetAsync(new Uri(
            $"/api/v1/admin/migration/jobs/{jobId}/urls?page=3&pageSize=100&classification=MIGRATE&state=MAPPED&q=arquivo",
            UriKind.Relative));
        var payload = await response.Content.ReadFromJsonAsync<InventoryPage>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal(620, payload.Total);
        Assert.Equal(3, payload.Page);
        Assert.Equal(100, payload.PageSize);
        Assert.Equal(7, payload.TotalPages);
        Assert.Equal(100, payload.Items.Length);
        Assert.All(payload.Items, item =>
        {
            Assert.Equal("MIGRATE", item.Classification);
            Assert.Equal("MAPPED", item.State);
            Assert.Contains("arquivo", item.NormalizedPath, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public async Task InventoryUrlsRejectInvalidPaging()
    {
        await _factory.SeedAsync();
        var jobId = await SeedInventoryAsync(1);
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Municipality", "deodapolis");
        await LoginAsync(client);

        using var response = await client.GetAsync(new Uri(
            $"/api/v1/admin/migration/jobs/{jobId}/urls?page=0&pageSize=501",
            UriKind.Relative));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task InventoryReportExportsEveryUrlWithoutSpreadsheetFormulaInjection()
    {
        await _factory.SeedAsync();
        var jobId = await SeedInventoryAsync();
        await AddFormulaLikeFailureAsync(jobId);
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Municipality", "deodapolis");
        await LoginAsync(client);

        using var response = await client.GetAsync(new Uri($"/api/v1/admin/migration/jobs/{jobId}/report.csv", UriKind.Relative));
        var csv = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(622, csv.Split("\r\n", StringSplitOptions.RemoveEmptyEntries).Length);
        Assert.Contains("arquivo-0619.pdf", csv, StringComparison.Ordinal);
        Assert.Contains("'=HYPERLINK", csv, StringComparison.Ordinal);
        Assert.DoesNotContain(",=HYPERLINK", csv, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InventoryCanIsolateFailuresForOperationalReview()
    {
        await _factory.SeedAsync();
        var jobId = await SeedInventoryAsync(3);
        await AddFormulaLikeFailureAsync(jobId);
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Municipality", "deodapolis");
        await LoginAsync(client);

        var payload = await client.GetFromJsonAsync<InventoryPage>(new Uri(
            $"/api/v1/admin/migration/jobs/{jobId}/urls?page=1&pageSize=100&kind=FAILURE",
            UriKind.Relative));

        Assert.NotNull(payload);
        Assert.Equal(1, payload.Total);
        Assert.Single(payload.Items);
        Assert.Equal("FAILED", payload.Items[0].State);
    }

    [Fact]
    public async Task JobDetailReportsInventorySizeWithoutInliningHundredsOfUrls()
    {
        await _factory.SeedAsync();
        var jobId = await SeedInventoryAsync();
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Municipality", "deodapolis");
        await LoginAsync(client);

        using var response = await client.GetAsync(new Uri($"/api/v1/admin/migration/jobs/{jobId}", UriKind.Relative));
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(620, payload.RootElement.GetProperty("urlCount").GetInt32());
        Assert.False(payload.RootElement.TryGetProperty("urls", out _));
    }

    private async Task<Guid> SeedInventoryAsync(int count = 620)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var tenant = scope.ServiceProvider.GetRequiredService<TenantContext>();
        tenant.SetMunicipality(MunicipalApiFactory.MunicipalityId, "deodapolis");
        var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        if (!await database.RoleCapabilities.AnyAsync(item => item.Role == "SUPER_ADMIN" && item.Capability == "migration.manage"))
            database.RoleCapabilities.Add(new RoleCapability(MunicipalApiFactory.MunicipalityId, "SUPER_ADMIN", "migration.manage"));

        var job = new MigrationJob(MunicipalApiFactory.MunicipalityId, "https://legacy.example.test/", "legacy.example.test", 6, 20_000);
        database.MigrationJobs.Add(job);
        for (var index = 0; index < count; index++)
        {
            var path = $"/uploads/arquivo-{index:D4}.pdf";
            var item = new LegacyUrl(MunicipalApiFactory.MunicipalityId, job.Id, $"https://legacy.example.test{path}", path, 2);
            item.Classify("MIGRATE", "application/pdf", 128, $"hash-{index:D4}");
            database.LegacyUrls.Add(item);
        }
        await database.SaveChangesAsync();
        return job.Id;
    }

    private async Task AddFormulaLikeFailureAsync(Guid jobId)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var tenant = scope.ServiceProvider.GetRequiredService<TenantContext>();
        tenant.SetMunicipality(MunicipalApiFactory.MunicipalityId, "deodapolis");
        var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var item = new LegacyUrl(
            MunicipalApiFactory.MunicipalityId,
            jobId,
            "https://legacy.example.test/falha.pdf",
            "/falha.pdf",
            1);
        item.Fail("=HYPERLINK(\"https://attacker.example\")");
        database.LegacyUrls.Add(item);
        await database.SaveChangesAsync();
    }

    private static async Task LoginAsync(HttpClient client)
    {
        using var response = await client.PostAsJsonAsync(
            new Uri("/api/v1/auth/login", UriKind.Relative),
            new { username = "admin.demo", password = "Demo-Local-2026!" });
        response.EnsureSuccessStatusCode();
    }

    private sealed record InventoryPage(int Page, int PageSize, int Total, int TotalPages, InventoryItem[] Items);
    private sealed record InventoryItem(string NormalizedPath, string Classification, string State);
}
