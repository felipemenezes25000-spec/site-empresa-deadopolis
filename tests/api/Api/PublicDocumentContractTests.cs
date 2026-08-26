using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using MunicipalPlatform.Api.Infrastructure.Persistence;
using MunicipalPlatform.Api.Modules.Media.Domain;
using MunicipalPlatform.Api.Modules.Migration.Domain;
using MunicipalPlatform.Api.Platform.Storage;
using MunicipalPlatform.Api.Platform.Tenancy;

namespace MunicipalPlatform.Api.Tests.Api;

public sealed class PublicDocumentContractTests : IClassFixture<MunicipalApiFactory>
{
    private readonly MunicipalApiFactory _factory;

    public PublicDocumentContractTests(MunicipalApiFactory factory) => _factory = factory;

    [Fact]
    public async Task PublicArchiveListsOnlyPublishedDocumentsAndServesApprovedAsset()
    {
        await _factory.SeedAsync();
        var published = await SeedDocumentAsync(published: true, "RREO publicado");
        await SeedDocumentAsync(published: false, "RREO em revisão");
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Municipality", "deodapolis");

        using var listResponse = await client.GetAsync(new Uri("/api/v1/public/documents?category=prestacao_contas&page=1&pageSize=10", UriKind.Relative));
        var list = await listResponse.Content.ReadFromJsonAsync<DocumentPage>();
        using var downloadResponse = await client.GetAsync(new Uri($"/api/v1/public/documents/{published.Id}/download", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        Assert.NotNull(list);
        Assert.Equal(1, list.Total);
        Assert.Single(list.Items);
        Assert.Equal("RREO publicado", list.Items[0].Title);
        Assert.Equal(HttpStatusCode.OK, downloadResponse.StatusCode);
        Assert.Equal("application/pdf", downloadResponse.Content.Headers.ContentType?.MediaType);
        Assert.Equal(published.Bytes, await downloadResponse.Content.ReadAsByteArrayAsync());
    }

    private async Task<(Guid Id, byte[] Bytes)> SeedDocumentAsync(bool published, string title)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var tenant = scope.ServiceProvider.GetRequiredService<TenantContext>();
        tenant.SetMunicipality(MunicipalApiFactory.MunicipalityId, "deodapolis");
        var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IObjectStorageProvider>();
        var bytes = Encoding.ASCII.GetBytes($"%PDF-1.7\n{title}\n%%EOF");
        var sha = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        var actor = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var job = new MigrationJob(MunicipalApiFactory.MunicipalityId, "https://legacy.example.test/", "legacy.example.test", 3, 20_000);
        job.Transition(MigrationJobState.DryRun, 1, 0, 0);
        var legacyUrl = new LegacyUrl(MunicipalApiFactory.MunicipalityId, job.Id, $"https://legacy.example.test/{Guid.NewGuid():N}.pdf", $"/{Guid.NewGuid():N}.pdf", 1);
        legacyUrl.Classify("MIGRATE", "application/pdf", bytes.LongLength, sha);
        var objectKey = $"contract-tests/{Guid.NewGuid():N}.pdf";
        await storage.SaveAsync(objectKey, bytes);
        var asset = new MediaAsset(MunicipalApiFactory.MunicipalityId, objectKey, "report.pdf", "application/pdf", bytes.LongLength, sha, actor);
        asset.Approve();
        var document = new PublicDocument(
            MunicipalApiFactory.MunicipalityId, legacyUrl.Id, job.Id, asset.Id,
            "PRESTACAO_CONTAS", "RREO", title, "Documento de teste contratual.", null, null, "2025",
            new DateOnly(2025, 12, 31), "Finanças", "REPORT", legacyUrl.Url, legacyUrl.NormalizedPath,
            "report.pdf", "application/pdf", bytes.LongLength, sha);
        if (published) document.Publish(DateTimeOffset.UtcNow);
        database.AddRange(job, legacyUrl, asset, document);
        await database.SaveChangesAsync();
        return (document.Id, bytes);
    }

    private sealed record DocumentPage(int Total, DocumentItem[] Items);
    private sealed record DocumentItem(Guid Id, string Title);
}
