using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using MunicipalPlatform.Api.Infrastructure.Persistence;
using MunicipalPlatform.Api.Modules.Migration.Domain;
using MunicipalPlatform.Api.Modules.Migration.Security;

namespace MunicipalPlatform.Api.Modules.Migration.Services;

public sealed partial class LegacyCrawlerService
{
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
            .Select(x => x.NormalizedPath)
            .ToListAsync(cancellationToken);
        if (job.State == MigrationJobState.DryRun && existing.Count > 0)
            return new LegacyCrawlSummary(existing.Count, 0, 0, true);

        job.Transition(MigrationJobState.Discovering, existing.Count, 0, 0);
        await database.SaveChangesAsync(cancellationToken);

        var discovered = new HashSet<string>(existing, StringComparer.OrdinalIgnoreCase);
        var queued = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var queue = new Queue<(Uri Uri, int Depth)>();
        Enqueue(root, 0, job, queued, queue);
        var failed = 0;
        var externalLinks = 0;

        using var client = SafeHttpFetcher.CreateClient(job.AllowedHost);
        try
        {
            while (queue.Count > 0 && discovered.Count < job.MaxPages)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var (uri, depth) = queue.Dequeue();
                var normalizedPath = LegacyUrlNormalizer.Normalize(uri.ToString());
                if (!discovered.Add(normalizedPath)) continue;

                var legacy = new LegacyUrl(job.MunicipalityId, job.Id, uri.ToString(), normalizedPath, depth);
                database.LegacyUrls.Add(legacy);

                try
                {
                    var fetched = await SafeHttpFetcher.FetchAsync(client, uri, job.AllowedHost, cancellationToken);
                    var hash = fetched.Body.Length == 0
                        ? null
                        : Convert.ToHexString(SHA256.HashData(fetched.Body)).ToLowerInvariant();
                    var classification = LegacyContentClassifier.Classify(
                        fetched.ContentType,
                        fetched.StatusCode,
                        fetched.RedirectLocation is not null);
                    legacy.Classify(classification, fetched.ContentType, fetched.Body.LongLength, hash);

                    if (fetched.RedirectLocation is not null)
                    {
                        if (ExternalUrlSafety.IsAllowedUri(fetched.RedirectLocation, job.AllowedHost))
                            Enqueue(fetched.RedirectLocation, depth, job, queued, queue);
                        else
                            externalLinks++;
                    }

                    if (fetched.StatusCode is >= 200 and <= 299
                        && fetched.ContentType?.Equals("text/html", StringComparison.OrdinalIgnoreCase) == true
                        && depth < job.MaxDepth)
                    {
                        var html = DecodeHtml(fetched.Body);
                        foreach (var candidate in ExtractLinks(uri, html))
                        {
                            if (ExternalUrlSafety.IsAllowedUri(candidate, job.AllowedHost))
                                Enqueue(candidate, depth + 1, job, queued, queue);
                            else
                                externalLinks++;
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

                job.Transition(MigrationJobState.Discovering, discovered.Count, 0, failed);
                await database.SaveChangesAsync(cancellationToken);
            }

            job.Transition(MigrationJobState.Mapping, discovered.Count, 0, failed);
            var evidence = new MigrationEvidence(
                job.MunicipalityId,
                job.Id,
                "DRY_RUN_SUMMARY",
                job.SourceBaseUrl,
                JsonSerializer.Serialize(new
                {
                    discovered = discovered.Count,
                    failed,
                    externalLinks,
                    maxDepth = job.MaxDepth,
                    maxPages = job.MaxPages,
                    host = job.AllowedHost,
                    readOnly = true,
                    truncatedByLimit = queue.Count > 0
                }));
            database.MigrationEvidences.Add(evidence);
            job.Transition(MigrationJobState.DryRun, discovered.Count, 0, failed);
            await database.SaveChangesAsync(cancellationToken);
            return new LegacyCrawlSummary(discovered.Count, failed, externalLinks, queue.Count > 0);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            job.Transition(MigrationJobState.Failed, discovered.Count, 0, failed + 1, ex.Message);
            await database.SaveChangesAsync(CancellationToken.None);
            throw;
        }
    }

    private static void Enqueue(
        Uri uri,
        int depth,
        MigrationJob job,
        HashSet<string> queued,
        Queue<(Uri Uri, int Depth)> queue)
    {
        if (depth > job.MaxDepth || !ExternalUrlSafety.IsAllowedUri(uri, job.AllowedHost)) return;
        var canonical = Canonicalize(uri);
        if (queued.Add(canonical.AbsoluteUri)) queue.Enqueue((canonical, depth));
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
                || raw.StartsWith("data:", StringComparison.OrdinalIgnoreCase)) continue;
            if (Uri.TryCreate(baseUri, raw, out var candidate)) yield return Canonicalize(candidate);
        }
    }

    [GeneratedRegex("(?:href|src)\\s*=\\s*[\\\"'](?<url>[^\\\"'<>]+)[\\\"']", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex LinkRegex();
}

public sealed record LegacyCrawlSummary(int Discovered, int Failed, int ExternalLinks, bool TruncatedByLimit);
