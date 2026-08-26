using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MunicipalPlatform.Api.Infrastructure.Persistence;
using MunicipalPlatform.Api.Modules.Migration.Domain;
using MunicipalPlatform.Api.Modules.Migration.Services;
using MunicipalPlatform.Api.Platform.Tenancy;

namespace MunicipalPlatform.Api.Tests.Migration;

public sealed class LegacyImportServiceTests
{
    private static readonly Guid MunicipalityId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ActorId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [Fact]
    public async Task PreparePageDraftCreatesDraftEvidenceAndOptionalRedirectWithoutPublishing()
    {
        var body = Encoding.UTF8.GetBytes("<html><head><title>Página histórica</title></head><body><script>alert(1)</script><h1>Serviço antigo</h1><p>Texto preservado.</p></body></html>");
        var hash = Convert.ToHexString(SHA256.HashData(body)).ToLowerInvariant();
        var (database, job, legacyUrl) = await CreateContextAsync(hash, body.LongLength);
        await using (database)
        {
            var service = new LegacyImportService(new StaticFetcher(new LegacyFetchResult(200, "text/html", body, null)));

            var result = await service.PreparePageDraftAsync(
                job,
                legacyUrl,
                new LegacyPageImportOptions("pagina-historica", null, null, "/servicos", true),
                ActorId,
                database,
                CancellationToken.None);
            await database.SaveChangesAsync();

            Assert.Equal("DRAFT", result.Resource.Status);
            Assert.Equal("PAGE", result.Resource.Kind);
            Assert.Equal("pagina-historica", result.Resource.Slug);
            Assert.Equal("Página histórica", result.Resource.Title);
            using var payload = JsonDocument.Parse(result.Resource.PayloadJson);
            var content = payload.RootElement.GetProperty("conteudo").GetString();
            Assert.NotNull(content);
            Assert.Contains("Serviço antigo", content, StringComparison.Ordinal);
            Assert.DoesNotContain("alert(1)", content, StringComparison.Ordinal);
            Assert.Equal(hash, result.ImportedContent.SourceSha256);
            Assert.NotNull(result.Redirect);
            Assert.Equal("/servicos", result.Redirect!.DestinationPath);
            Assert.Equal(301, result.Redirect.StatusCode);
            Assert.Equal(MigrationJobState.DryRun, job.State);
            Assert.Equal(1, job.ImportedCount);
            Assert.Single(await database.ContentRevisions.ToListAsync());
            Assert.Single(await database.ImportedContents.ToListAsync());
            Assert.Single(await database.RedirectRules.ToListAsync());
        }
    }

    [Fact]
    public async Task PreparePageDraftRejectsSourceWhenHashChangedAfterDryRun()
    {
        var inventoriedBody = Encoding.UTF8.GetBytes("<html><body>Versão inventariada</body></html>");
        var changedBody = Encoding.UTF8.GetBytes("<html><body>Versão alterada</body></html>");
        var hash = Convert.ToHexString(SHA256.HashData(inventoriedBody)).ToLowerInvariant();
        var (database, job, legacyUrl) = await CreateContextAsync(hash, inventoriedBody.LongLength);
        await using (database)
        {
            var service = new LegacyImportService(new StaticFetcher(new LegacyFetchResult(200, "text/html", changedBody, null)));

            var exception = await Assert.ThrowsAsync<LegacyImportConflictException>(() => service.PreparePageDraftAsync(
                job,
                legacyUrl,
                new LegacyPageImportOptions("pagina-alterada", null, null, null, false),
                ActorId,
                database,
                CancellationToken.None));

            Assert.Contains("SHA-256", exception.Message, StringComparison.Ordinal);
            Assert.Empty(database.PortalResources.Local);
            Assert.Empty(database.ImportedContents.Local);
        }
    }

    [Fact]
    public async Task PreparePageDraftIsIdempotentPerLegacyUrl()
    {
        var body = Encoding.UTF8.GetBytes("<html><body><p>Conteúdo estável</p></body></html>");
        var hash = Convert.ToHexString(SHA256.HashData(body)).ToLowerInvariant();
        var (database, job, legacyUrl) = await CreateContextAsync(hash, body.LongLength);
        await using (database)
        {
            var service = new LegacyImportService(new StaticFetcher(new LegacyFetchResult(200, "text/html", body, null)));
            var options = new LegacyPageImportOptions("conteudo-estavel", "Conteúdo estável", null, null, false);

            await service.PreparePageDraftAsync(job, legacyUrl, options, ActorId, database, CancellationToken.None);
            await database.SaveChangesAsync();

            var exception = await Assert.ThrowsAsync<LegacyImportConflictException>(() => service.PreparePageDraftAsync(
                job,
                legacyUrl,
                options,
                ActorId,
                database,
                CancellationToken.None));

            Assert.Contains("já foi importada", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Single(await database.ImportedContents.ToListAsync());
        }
    }

    private static async Task<(ApplicationDbContext Database, MigrationJob Job, LegacyUrl LegacyUrl)> CreateContextAsync(string hash, long contentLength)
    {
        var tenant = new TenantContext();
        tenant.SetMunicipality(MunicipalityId, "deodapolis");
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"legacy-import-{Guid.NewGuid():N}")
            .Options;
        var database = new ApplicationDbContext(options, tenant);
        var job = new MigrationJob(MunicipalityId, "https://legacy.example.test/", "legacy.example.test", 2, 50);
        job.Transition(MigrationJobState.DryRun, 1, 0, 0);
        var legacyUrl = new LegacyUrl(MunicipalityId, job.Id, "https://legacy.example.test/pagina", "/pagina", 0);
        legacyUrl.Classify("MIGRATE", "text/html", contentLength, hash);
        database.MigrationJobs.Add(job);
        database.LegacyUrls.Add(legacyUrl);
        await database.SaveChangesAsync();
        return (database, job, legacyUrl);
    }

    private sealed class StaticFetcher(LegacyFetchResult result) : ILegacySourceFetcher
    {
        public Task<LegacyFetchResult> FetchAsync(Uri uri, string allowedHost, CancellationToken cancellationToken)
        {
            Assert.Equal("legacy.example.test", uri.Host);
            Assert.Equal("legacy.example.test", allowedHost);
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(result);
        }
    }
}
