using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using MunicipalPlatform.Api.Infrastructure.Persistence;
using MunicipalPlatform.Api.Modules.Media.Domain;
using MunicipalPlatform.Api.Platform.Tenancy;

namespace MunicipalPlatform.Api.Tests.Api;

public sealed class MediaLibraryContractTests : IClassFixture<MunicipalApiFactory>
{
    private readonly MunicipalApiFactory _factory;

    public MediaLibraryContractTests(MunicipalApiFactory factory) => _factory = factory;

    [Fact]
    public async Task AdminLibraryUsesDefaultPageWhenCallerOnlySetsPageSize()
    {
        await _factory.SeedAsync();
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Municipality", "deodapolis");
        using var login = await client.PostAsJsonAsync(new Uri("/api/v1/auth/login", UriKind.Relative), new { username = "admin.demo", password = "Demo-Local-2026!" });
        using var response = await client.GetAsync(new Uri("/api/v1/admin/media?status=APPROVED&pageSize=100", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("X-Total-Count"));
    }

    [Fact]
    public async Task UploadPersistsAccessibilityMetadataFromMultipartForm()
    {
        await _factory.SeedAsync();
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Municipality", "deodapolis");
        using var login = await client.PostAsJsonAsync(new Uri("/api/v1/auth/login", UriKind.Relative), new { username = "admin.demo", password = "Demo-Local-2026!" });
        using var multipart = new MultipartFormDataContent();
        var file = new ByteArrayContent(Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII="));
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        multipart.Add(file, "file", "metadata-contract.png");
        multipart.Add(new StringContent("Descrição acessível preservada"), "altText");
        multipart.Add(new StringContent("Legenda institucional"), "caption");
        multipart.Add(new StringContent("Prefeitura de Deodápolis"), "credit");

        using var upload = await client.PostAsync(new Uri("/api/v1/admin/media/upload", UriKind.Relative), multipart);
        using var list = await client.GetAsync(new Uri("/api/v1/admin/media?page=1&pageSize=100&q=metadata-contract", UriKind.Relative));
        var items = await list.Content.ReadFromJsonAsync<MediaPayload[]>();

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.Equal(HttpStatusCode.Created, upload.StatusCode);
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.Equal("Descrição acessível preservada", Assert.Single(items ?? []).AltText);
    }

    [Fact]
    public async Task AdminLibraryPaginatesAndFiltersApprovedMedia()
    {
        await _factory.SeedAsync();
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var tenant = scope.ServiceProvider.GetRequiredService<TenantContext>();
            tenant.SetMunicipality(MunicipalApiFactory.MunicipalityId, "deodapolis");
            var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var actor = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            for (var index = 0; index < 25; index++)
            {
                var asset = new MediaAsset(
                    MunicipalApiFactory.MunicipalityId,
                    $"media/test/vacinacao-{index}.jpg",
                    $"vacinacao-{index:D2}.jpg",
                    "image/jpeg",
                    1_024,
                    index.ToString("x64", CultureInfo.InvariantCulture),
                    actor);
                asset.UpdateMetadata($"Vacinação {index}", null, null);
                asset.Approve();
                database.MediaAssets.Add(asset);
            }
            await database.SaveChangesAsync();
        }

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Municipality", "deodapolis");
        using var login = await client.PostAsJsonAsync(new Uri("/api/v1/auth/login", UriKind.Relative), new { username = "admin.demo", password = "Demo-Local-2026!" });
        using var response = await client.GetAsync(new Uri("/api/v1/admin/media?page=3&pageSize=10&q=vacinacao&status=APPROVED", UriKind.Relative));
        var items = await response.Content.ReadFromJsonAsync<MediaPayload[]>();

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("25", response.Headers.GetValues("X-Total-Count").Single());
        Assert.Equal(5, items?.Length);
        Assert.All(items ?? [], item =>
        {
            Assert.Equal("APPROVED", item.Status);
            Assert.Contains("vacinacao", item.OriginalFileName, StringComparison.OrdinalIgnoreCase);
        });
    }

    private sealed record MediaPayload(string OriginalFileName, string Status, string AltText);
}
