using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using MunicipalPlatform.Api.Infrastructure.Persistence;
using MunicipalPlatform.Api.Modules.Media.Providers;
using MunicipalPlatform.Api.Modules.Migration.Domain;
using MunicipalPlatform.Api.Modules.Migration.Services;
using MunicipalPlatform.Api.Platform.Storage;
using MunicipalPlatform.Api.Platform.Tenancy;

namespace MunicipalPlatform.Api.Tests.Migration;

public sealed class LegacyDocumentImportServiceTests
{
    private static readonly Guid MunicipalityId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ActorId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [Fact]
    public async Task ImportCreatesQuarantinedArchiveRecordWithTraceableEvidence()
    {
        var bytes = Pdf("official-document");
        var (database, job, legacyUrl) = await CreateContextAsync("/uploads/report.pdf", "application/pdf", bytes);
        await using (database)
        {
            var storage = new RecordingStorage();
            var scanner = new RecordingScanner(isClean: true);
            var service = new LegacyDocumentImportService(new StaticFetcher(bytes, "application/pdf"), storage, scanner);

            var result = await service.ImportAsync(
                job,
                legacyUrl,
                new LegacyDocumentImportOptions(
                    "PRESTACAO_CONTAS",
                    "RREO",
                    "Relatório oficial",
                    "Documento preservado do portal anterior.",
                    "12/2025",
                    null,
                    "2025",
                    new DateOnly(2025, 12, 31),
                    "Secretaria de Finanças",
                    "REPORT"),
                ActorId,
                database,
                CancellationToken.None);
            await database.SaveChangesAsync();

            Assert.Equal("DRAFT", result.Document.Status);
            Assert.Equal(legacyUrl.Id, result.Document.LegacyUrlId);
            Assert.Equal(job.Id, result.Document.MigrationJobId);
            Assert.Equal(result.Asset.Id, result.Document.MediaAssetId);
            Assert.Equal("APPROVED", result.Asset.Status);
            Assert.Equal("report.pdf", result.Document.OriginalFileName);
            Assert.Equal(legacyUrl.Sha256, result.Document.Sha256);
            Assert.Equal("PUBLIC_DOCUMENT", result.ImportedContent.TargetType);
            Assert.Contains(legacyUrl.Url, result.EvidenceJson, StringComparison.Ordinal);
            Assert.Equal(1, scanner.ScanCount);
            Assert.Single(storage.Saved);
            Assert.Single(await database.PublicDocuments.ToListAsync());
            Assert.Single(await database.MediaAssets.ToListAsync());
            Assert.Single(await database.ImportedContents.ToListAsync());
        }
    }

    [Fact]
    public async Task DistinctLegacyContextsWithSameHashReusePhysicalAsset()
    {
        var bytes = Pdf("shared-document");
        var (database, job, firstUrl) = await CreateContextAsync("/licitacoes/edital.pdf", "application/pdf", bytes);
        await using (database)
        {
            var secondUrl = AddLegacyUrl(database, job, "/contratos/contrato.pdf", "application/pdf", bytes);
            await database.SaveChangesAsync();
            var storage = new RecordingStorage();
            var scanner = new RecordingScanner(isClean: true);
            var service = new LegacyDocumentImportService(new StaticFetcher(bytes, "application/pdf"), storage, scanner);

            var first = await service.ImportAsync(job, firstUrl, Options("LICITACOES", "EDITAL", "Edital"), ActorId, database, CancellationToken.None);
            await database.SaveChangesAsync();
            var second = await service.ImportAsync(job, secondUrl, Options("LICITACOES", "CONTRATO", "Contrato"), ActorId, database, CancellationToken.None);
            await database.SaveChangesAsync();

            Assert.Equal(first.Asset.Id, second.Asset.Id);
            Assert.Equal(1, scanner.ScanCount);
            Assert.Single(storage.Saved);
            Assert.Equal(2, await database.PublicDocuments.CountAsync());
            Assert.Equal(2, await database.ImportedContents.CountAsync());
        }
    }

    [Fact]
    public async Task ImportRejectsContentChangedSinceInventory()
    {
        var inventoried = Pdf("inventoried");
        var changed = Pdf("changed");
        var (database, job, legacyUrl) = await CreateContextAsync("/document.pdf", "application/pdf", inventoried);
        await using (database)
        {
            var storage = new RecordingStorage();
            var service = new LegacyDocumentImportService(
                new StaticFetcher(changed, "application/pdf"),
                storage,
                new RecordingScanner(isClean: true));

            var exception = await Assert.ThrowsAsync<LegacyImportConflictException>(() => service.ImportAsync(
                job,
                legacyUrl,
                Options("DOCUMENTOS", "GERAL", "Documento"),
                ActorId,
                database,
                CancellationToken.None));

            Assert.Contains("SHA-256", exception.Message, StringComparison.Ordinal);
            Assert.Empty(storage.Saved);
            Assert.Empty(database.MediaAssets.Local);
            Assert.Empty(database.PublicDocuments.Local);
        }
    }

    [Fact]
    public async Task ImportRejectsExtensionThatContradictsMagicBytes()
    {
        var bytes = Pdf("not-an-office-file");
        var (database, job, legacyUrl) = await CreateContextAsync("/document.docx", "application/pdf", bytes);
        await using (database)
        {
            var service = new LegacyDocumentImportService(
                new StaticFetcher(bytes, "application/pdf"),
                new RecordingStorage(),
                new RecordingScanner(isClean: true));

            var exception = await Assert.ThrowsAsync<LegacyImportValidationException>(() => service.ImportAsync(
                job,
                legacyUrl,
                Options("DOCUMENTOS", "GERAL", "Documento"),
                ActorId,
                database,
                CancellationToken.None));

            Assert.Contains("extensão", exception.Message, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static LegacyDocumentImportOptions Options(string category, string subcategory, string title) =>
        new(category, subcategory, title, null, null, null, null, null, null, "DOCUMENT");

    private static async Task<(ApplicationDbContext Database, MigrationJob Job, LegacyUrl LegacyUrl)> CreateContextAsync(
        string path,
        string contentType,
        byte[] bytes)
    {
        var tenant = new TenantContext();
        tenant.SetMunicipality(MunicipalityId, "deodapolis");
        var database = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase($"legacy-document-{Guid.NewGuid():N}")
                .Options,
            tenant);
        var job = new MigrationJob(MunicipalityId, "https://legacy.example.test/", "legacy.example.test", 3, 20_000);
        job.Transition(MigrationJobState.DryRun, 1, 0, 0);
        var legacyUrl = AddLegacyUrl(database, job, path, contentType, bytes);
        database.MigrationJobs.Add(job);
        await database.SaveChangesAsync();
        return (database, job, legacyUrl);
    }

    private static LegacyUrl AddLegacyUrl(
        ApplicationDbContext database,
        MigrationJob job,
        string path,
        string contentType,
        byte[] bytes)
    {
        var url = new Uri(new Uri(job.SourceBaseUrl), path).ToString();
        var legacyUrl = new LegacyUrl(MunicipalityId, job.Id, url, path, 1);
        var hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        legacyUrl.Classify("MIGRATE", contentType, bytes.LongLength, hash);
        database.LegacyUrls.Add(legacyUrl);
        return legacyUrl;
    }

    private static byte[] Pdf(string body) => Encoding.ASCII.GetBytes($"%PDF-1.7\n{body}\n%%EOF");

    private sealed class StaticFetcher(byte[] bytes, string contentType) : ILegacySourceFetcher
    {
        public Task<LegacyFetchResult> FetchAsync(Uri uri, string allowedHost, CancellationToken cancellationToken) =>
            Task.FromResult(new LegacyFetchResult(200, contentType, bytes, null));
    }

    private sealed class RecordingStorage : IObjectStorageProvider
    {
        public string State => "DEMO_ONLY";
        public string Description => "Test storage";
        public Dictionary<string, byte[]> Saved { get; } = [];

        public Task SaveAsync(string objectKey, ReadOnlyMemory<byte> content, CancellationToken cancellationToken = default)
        {
            Saved.Add(objectKey, content.ToArray());
            return Task.CompletedTask;
        }

        public Task<byte[]?> ReadAsync(string objectKey, CancellationToken cancellationToken = default) =>
            Task.FromResult(Saved.TryGetValue(objectKey, out var bytes) ? bytes : null);
    }

    private sealed class RecordingScanner(bool isClean) : IMalwareScanner
    {
        public string State => "DEMO_ONLY";
        public string Description => "Test scanner";
        public int ScanCount { get; private set; }

        public Task<MalwareScanResult> ScanAsync(ReadOnlyMemory<byte> content, CancellationToken cancellationToken = default)
        {
            ScanCount++;
            return Task.FromResult(new MalwareScanResult(isClean, State, Description));
        }
    }
}
