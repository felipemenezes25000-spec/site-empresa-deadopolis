using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MunicipalPlatform.Api.Infrastructure.Persistence;
using MunicipalPlatform.Api.Modules.Migration.Domain;
using MunicipalPlatform.Api.Modules.Migration.Services;
using MunicipalPlatform.Api.Platform.Tenancy;

namespace MunicipalPlatform.Api.Tests.Migration;

public sealed class LegacyCrawlerServiceTests
{
    private static readonly Guid MunicipalityId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public async Task RunDryRunInventoriesContentFamiliesAndPersistsOperationalEvidence()
    {
        var fetcher = new RecordingFetcher(new Dictionary<string, LegacyFetchResult>
        {
            ["/"] = Html("""
                <a href="/document.pdf">PDF</a>
                <a href="/copy.pdf">PDF duplicate</a>
                <a href="/report.docx">Office</a>
                <img src="/photo.png">
                <a href="/missing">Missing</a>
                <a href="https://external.example.test/resource">External</a>
                """),
            ["/document.pdf"] = Binary("application/pdf", "same-pdf"),
            ["/copy.pdf"] = Binary("application/pdf", "same-pdf"),
            ["/report.docx"] = Binary("application/vnd.openxmlformats-officedocument.wordprocessingml.document", "office"),
            ["/photo.png"] = Binary("image/png", "image"),
            ["/missing"] = new LegacyFetchResult(404, "text/html", [], null)
        });
        await using var database = CreateDatabase();
        var job = new MigrationJob(MunicipalityId, "https://legacy.example.test/", "legacy.example.test", 2, 20);
        database.MigrationJobs.Add(job);
        await database.SaveChangesAsync();

        var summary = await new LegacyCrawlerService(fetcher)
            .RunDryRunAsync(job, database, CancellationToken.None);

        Assert.Equal(6, summary.Discovered);
        Assert.Equal(0, summary.Failed);
        Assert.Equal(1, summary.ExternalLinks);
        Assert.Equal(1, summary.Html);
        Assert.Equal(2, summary.Pdf);
        Assert.Equal(1, summary.Office);
        Assert.Equal(1, summary.Images);
        Assert.Equal(1, summary.DuplicatesByHash);
        Assert.Equal(6, summary.UniqueNormalized);
        Assert.Equal(0, summary.QueueRemaining);
        Assert.False(summary.TruncatedByLimit);
        Assert.Equal(5, summary.StatusCodes["200"]);
        Assert.Equal(1, summary.StatusCodes["404"]);
        Assert.Equal(5, summary.Classifications["MIGRATE"]);
        Assert.Equal(1, summary.Classifications["IGNORE_WITH_REASON"]);

        var evidence = await database.MigrationEvidences.SingleAsync();
        var persisted = JsonSerializer.Deserialize<LegacyCrawlSummary>(evidence.PayloadJson, LegacyCrawlerService.SummaryJsonOptions);
        Assert.NotNull(persisted);
        AssertSummaryEqual(summary, persisted!);
        Assert.Equal(MigrationJobState.DryRun, job.State);
    }

    [Fact]
    public async Task CompletedDryRunIsIdempotentAndDoesNotFetchAgain()
    {
        var fetcher = new RecordingFetcher(new Dictionary<string, LegacyFetchResult>
        {
            ["/"] = Html("<a href=\"/page\">Page</a>"),
            ["/page"] = Html("<p>Stable</p>")
        });
        await using var database = CreateDatabase();
        var job = new MigrationJob(MunicipalityId, "https://legacy.example.test/", "legacy.example.test", 2, 20);
        database.MigrationJobs.Add(job);
        await database.SaveChangesAsync();
        var crawler = new LegacyCrawlerService(fetcher);

        var first = await crawler.RunDryRunAsync(job, database, CancellationToken.None);
        var requestsAfterFirstRun = fetcher.RequestCount;
        var second = await crawler.RunDryRunAsync(job, database, CancellationToken.None);

        AssertSummaryEqual(first, second);
        Assert.Equal(requestsAfterFirstRun, fetcher.RequestCount);
        Assert.Equal(2, await database.LegacyUrls.CountAsync());
        Assert.Single(await database.MigrationEvidences.ToListAsync());
    }

    [Fact]
    public async Task TruncatedDryRunRunsAgainInsteadOfReturningFalseCompletedSummary()
    {
        var fetcher = new RecordingFetcher(new Dictionary<string, LegacyFetchResult>
        {
            ["/"] = Html("<a href=\"/a\">A</a><a href=\"/b\">B</a>"),
            ["/a"] = Html("<p>A</p>"),
            ["/b"] = Html("<p>B</p>")
        });
        await using var database = CreateDatabase();
        var job = new MigrationJob(MunicipalityId, "https://legacy.example.test/", "legacy.example.test", 2, 1);
        database.MigrationJobs.Add(job);
        await database.SaveChangesAsync();
        var crawler = new LegacyCrawlerService(fetcher);

        var first = await crawler.RunDryRunAsync(job, database, CancellationToken.None);
        var second = await crawler.RunDryRunAsync(job, database, CancellationToken.None);

        Assert.True(first.TruncatedByLimit);
        Assert.Equal(2, first.QueueRemaining);
        Assert.True(second.TruncatedByLimit);
        Assert.Equal(2, second.QueueRemaining);
        Assert.Equal(2, fetcher.RequestCount);
        Assert.Single(await database.LegacyUrls.ToListAsync());
        Assert.Equal(2, await database.MigrationEvidences.CountAsync());
    }

    private static ApplicationDbContext CreateDatabase()
    {
        var tenant = new TenantContext();
        tenant.SetMunicipality(MunicipalityId, "deodapolis");
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"legacy-crawler-{Guid.NewGuid():N}")
            .Options;
        return new ApplicationDbContext(options, tenant);
    }

    private static LegacyFetchResult Html(string body) =>
        new(200, "text/html", Encoding.UTF8.GetBytes(body), null);

    private static LegacyFetchResult Binary(string contentType, string body) =>
        new(200, contentType, Encoding.UTF8.GetBytes(body), null);

    private static void AssertSummaryEqual(LegacyCrawlSummary expected, LegacyCrawlSummary actual) =>
        Assert.Equal(
            JsonSerializer.Serialize(expected, LegacyCrawlerService.SummaryJsonOptions),
            JsonSerializer.Serialize(actual, LegacyCrawlerService.SummaryJsonOptions));

    private sealed class RecordingFetcher(IReadOnlyDictionary<string, LegacyFetchResult> responses) : ILegacySourceFetcher
    {
        public int RequestCount { get; private set; }

        public Task<LegacyFetchResult> FetchAsync(Uri uri, string allowedHost, CancellationToken cancellationToken)
        {
            Assert.Equal("legacy.example.test", uri.Host);
            Assert.Equal("legacy.example.test", allowedHost);
            cancellationToken.ThrowIfCancellationRequested();
            RequestCount++;
            return Task.FromResult(responses[uri.AbsolutePath]);
        }
    }
}
