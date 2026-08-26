using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using MunicipalPlatform.Api.Infrastructure.Persistence;
using MunicipalPlatform.Api.Modules.Migration.Domain;
using MunicipalPlatform.Api.Modules.Migration.Security;

namespace MunicipalPlatform.Api.Modules.Migration.Services;

public sealed partial class LegacyCrawlerService(ILegacySourceFetcher fetcher)
{
    private const int ProgressBatchSize = 50;
    public static JsonSerializerOptions SummaryJsonOptions { get; } = new(JsonSerializerDefaults.Web);

    public async Task<LegacyCrawlSummary> RunDryRunAsync(
        MigrationJob job,
        ApplicationDbContext database,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(database);

        if (!Uri.TryCreate(job.SourceBaseUrl, UriKind.Absolute, out var root)
            || !ExternalUrlSafety.IsAllowedUri(root, job.AllowedHost))
            throw new InvalidOperationException("Configuração de origem do job é inválida ou não é segura.");

        var existing = await database.LegacyUrls
            .Where(x => x.MigrationJobId == job.Id)
            .ToDictionaryAsync(x => x.NormalizedPath, StringComparer.OrdinalIgnoreCase, cancellationToken);
        if (job.State == MigrationJobState.DryRun)
        {
            var persistedEvidence = await database.MigrationEvidences
                .Where(x => x.MigrationJobId == job.Id && x.Kind == "DRY_RUN_SUMMARY")
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => x.PayloadJson)
                .FirstOrDefaultAsync(cancellationToken);
            var persistedSummary = DeserializeSummary(persistedEvidence);
            if (persistedSummary is { SchemaVersion: LegacyCrawlSummary.CurrentSchemaVersion, TruncatedByLimit: false, QueueRemaining: 0 })
                return persistedSummary;
        }

        job.Transition(MigrationJobState.Discovering, existing.Count, 0, 0);
        await database.SaveChangesAsync(cancellationToken);

        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var queued = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var queue = new Queue<(Uri Uri, int Depth)>();
        Enqueue(root, 0, job, queued, queue);
        var failed = 0;
        var externalLinks = 0;
        var externalUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var redirects = 0;
        var htmlCount = 0;
        var pdfCount = 0;
        var officeCount = 0;
        var imageCount = 0;
        var statusCodes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var mimeTypes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        try
        {
            while (queue.Count > 0 && visited.Count < job.MaxPages)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var (uri, depth) = queue.Dequeue();
                var normalizedPath = LegacyUrlNormalizer.Normalize(uri.ToString());
                if (!visited.Add(normalizedPath))
                    continue;

                if (!existing.TryGetValue(normalizedPath, out var legacy))
                {
                    legacy = new LegacyUrl(job.MunicipalityId, job.Id, uri.ToString(), normalizedPath, depth);
                    existing.Add(normalizedPath, legacy);
                    database.LegacyUrls.Add(legacy);
                }

                try
                {
                    var fetched = await fetcher.FetchAsync(uri, job.AllowedHost, cancellationToken);
                    var hash = fetched.Body.Length == 0
                        ? null
                        : Convert.ToHexString(SHA256.HashData(fetched.Body)).ToLowerInvariant();
                    var classification = LegacyContentClassifier.Classify(
                        fetched.ContentType,
                        fetched.StatusCode,
                        fetched.RedirectLocation is not null);
                    var decisionReason = classification == "IGNORE_WITH_REASON"
                        ? fetched.StatusCode >= 400
                            ? $"HTTP {fetched.StatusCode}"
                            : $"MIME não suportado: {fetched.ContentType ?? "ausente"}"
                        : null;
                    legacy.Classify(classification, fetched.ContentType, fetched.Body.LongLength, hash, decisionReason);
                    Increment(statusCodes, fetched.StatusCode.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    if (!string.IsNullOrWhiteSpace(fetched.ContentType))
                        Increment(mimeTypes, fetched.ContentType.ToLowerInvariant());

                    if (fetched.StatusCode is >= 200 and <= 299)
                    {
                        if (fetched.ContentType?.Equals("text/html", StringComparison.OrdinalIgnoreCase) == true)
                            htmlCount++;
                        else if (fetched.ContentType?.Equals("application/pdf", StringComparison.OrdinalIgnoreCase) == true)
                            pdfCount++;
                        else if (IsOfficeContentType(fetched.ContentType))
                            officeCount++;
                        else if (fetched.ContentType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) == true)
                            imageCount++;
                    }

                    if (fetched.RedirectLocation is not null)
                    {
                        redirects++;
                        if (ExternalUrlSafety.IsAllowedUri(fetched.RedirectLocation, job.AllowedHost))
                            Enqueue(fetched.RedirectLocation, depth, job, queued, queue);
                        else
                        {
                            externalLinks++;
                            externalUrls.Add(fetched.RedirectLocation.AbsoluteUri);
                        }
                    }

                    if (fetched.StatusCode is >= 200 and <= 299
                        && fetched.ContentType?.Equals("text/html", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        var html = DecodeHtml(fetched.Body);
                        var continuePagination = LegacyTraversalPolicy.ShouldContinuePagination(uri, html);
                        foreach (var candidate in ExtractLinks(uri, html))
                        {
                            if (ExternalUrlSafety.IsAllowedUri(candidate, job.AllowedHost))
                            {
                                if (!continuePagination
                                    && string.Equals(uri.AbsolutePath, candidate.AbsolutePath, StringComparison.OrdinalIgnoreCase))
                                    continue;
                                var candidateDepth = LegacyTraversalPolicy.GetNextDepth(uri, candidate, depth);
                                Enqueue(candidate, candidateDepth, job, queued, queue);
                            }
                            else
                            {
                                externalLinks++;
                                externalUrls.Add(candidate.AbsoluteUri);
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex) when (ex is HttpRequestException or IOException or InvalidOperationException or SocketException)
                {
                    failed++;
                    legacy.Fail(ex.Message.Length > 500 ? ex.Message[..500] : ex.Message);
                }

                job.Transition(MigrationJobState.Discovering, existing.Count, 0, failed);
                if (visited.Count % ProgressBatchSize == 0)
                    await database.SaveChangesAsync(cancellationToken);
            }

            var classifications = existing.Values
                .GroupBy(x => x.Classification, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(x => x.Key, x => x.Count(), StringComparer.OrdinalIgnoreCase);
            var families = existing.Values
                .GroupBy(x => GetPathFamily(x.NormalizedPath), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(x => x.Key, x => x.Count(), StringComparer.OrdinalIgnoreCase);
            var depths = existing.Values
                .GroupBy(x => x.Depth)
                .ToDictionary(
                    x => x.Key.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    x => x.Count(),
                    StringComparer.OrdinalIgnoreCase);
            var duplicatesByHash = existing.Values
                .Where(x => !string.IsNullOrWhiteSpace(x.Sha256))
                .GroupBy(x => x.Sha256!, StringComparer.OrdinalIgnoreCase)
                .Sum(x => Math.Max(0, x.Count() - 1));
            var summary = new LegacyCrawlSummary(
                LegacyCrawlSummary.CurrentSchemaVersion,
                existing.Count,
                failed,
                externalLinks,
                externalUrls.Order(StringComparer.OrdinalIgnoreCase).ToArray(),
                redirects,
                htmlCount,
                pdfCount + officeCount,
                pdfCount,
                officeCount,
                imageCount,
                duplicatesByHash,
                existing.Count,
                queue.Count,
                queue.Count > 0,
                statusCodes,
                mimeTypes,
                classifications,
                families,
                depths);

            job.Transition(MigrationJobState.Mapping, existing.Count, 0, failed);
            var evidence = new MigrationEvidence(
                job.MunicipalityId,
                job.Id,
                "DRY_RUN_SUMMARY",
                job.SourceBaseUrl,
                JsonSerializer.Serialize(summary, SummaryJsonOptions));
            database.MigrationEvidences.Add(evidence);
            job.Transition(MigrationJobState.DryRun, existing.Count, 0, failed);
            await database.SaveChangesAsync(cancellationToken);
            return summary;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            job.Transition(MigrationJobState.Failed, existing.Count, 0, failed + 1, ex.Message);
            await database.SaveChangesAsync(CancellationToken.None);
            throw;
        }
    }

    private static LegacyCrawlSummary? DeserializeSummary(string? payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
            return null;
        try
        {
            return JsonSerializer.Deserialize<LegacyCrawlSummary>(payloadJson, SummaryJsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static bool IsOfficeContentType(string? contentType) =>
        contentType?.Contains("officedocument", StringComparison.OrdinalIgnoreCase) == true
        || contentType?.Contains("msword", StringComparison.OrdinalIgnoreCase) == true
        || contentType?.Contains("spreadsheet", StringComparison.OrdinalIgnoreCase) == true;

    private static void Increment(Dictionary<string, int> metrics, string key) =>
        metrics[key] = metrics.GetValueOrDefault(key) + 1;

    private static string GetPathFamily(string normalizedPath)
    {
        var path = normalizedPath.Split('?', 2)[0];
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0)
            return "/";
        if (segments[0].Equals("e-sic", StringComparison.OrdinalIgnoreCase))
        {
            if (segments.Length >= 3 && segments[1].Equals("uploads", StringComparison.OrdinalIgnoreCase))
                return $"/e-sic/uploads/{segments[2]}";
            return segments.Length >= 2 ? $"/e-sic/{segments[1]}" : "/e-sic";
        }
        if (segments[0].Equals("licitacoes", StringComparison.OrdinalIgnoreCase))
            return segments.Length >= 2 ? $"/licitacoes/{segments[1]}" : "/licitacoes";
        return $"/{segments[0]}";
    }

    private static void Enqueue(
        Uri uri,
        int depth,
        MigrationJob job,
        HashSet<string> queued,
        Queue<(Uri Uri, int Depth)> queue)
    {
        if (depth > job.MaxDepth || !ExternalUrlSafety.IsAllowedUri(uri, job.AllowedHost))
            return;
        var canonical = Canonicalize(uri);
        if (queued.Add(canonical.AbsoluteUri))
            queue.Enqueue((canonical, depth));
    }

    private static Uri Canonicalize(Uri uri)
    {
        var builder = new UriBuilder(uri) { Fragment = string.Empty };
        return builder.Uri;
    }

    private static string DecodeHtml(byte[] body)
    {
        try
        {
            return new UTF8Encoding(false, true).GetString(body);
        }
        catch (DecoderFallbackException)
        {
            return Encoding.Latin1.GetString(body);
        }
    }

    private static IEnumerable<Uri> ExtractLinks(Uri baseUri, string html)
    {
        foreach (Match match in LinkRegex().Matches(html))
        {
            var raw = WebUtility.HtmlDecode(match.Groups["url"].Value.Trim());
            if (string.IsNullOrWhiteSpace(raw)
                || raw.StartsWith('#')
                || raw.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase)
                || raw.StartsWith("tel:", StringComparison.OrdinalIgnoreCase)
                || raw.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase)
                || raw.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                continue;
            if (Uri.TryCreate(baseUri, raw, out var candidate))
                yield return Canonicalize(candidate);
        }
    }

    [GeneratedRegex("(?:href|src)\\s*=\\s*[\\\"'](?<url>[^\\\"'<>]+)[\\\"']", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex LinkRegex();
}

public sealed record LegacyCrawlSummary(
    int SchemaVersion,
    int Discovered,
    int Failed,
    int ExternalLinks,
    string[] ExternalUrls,
    int Redirects,
    int Html,
    int Documents,
    int Pdf,
    int Office,
    int Images,
    int DuplicatesByHash,
    int UniqueNormalized,
    int QueueRemaining,
    bool TruncatedByLimit,
    Dictionary<string, int> StatusCodes,
    Dictionary<string, int> MimeTypes,
    Dictionary<string, int> Classifications,
    Dictionary<string, int> Families,
    Dictionary<string, int> Depths)
{
    public const int CurrentSchemaVersion = 4;
}
