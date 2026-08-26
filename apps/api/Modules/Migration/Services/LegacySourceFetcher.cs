using System.Collections.Concurrent;

namespace MunicipalPlatform.Api.Modules.Migration.Services;

public interface ILegacySourceFetcher
{
    Task<LegacyFetchResult> FetchAsync(Uri uri, string allowedHost, CancellationToken cancellationToken);
}

public sealed class SafeLegacySourceFetcher : ILegacySourceFetcher, IDisposable
{
    private readonly ConcurrentDictionary<string, HttpClient> _clients = new(StringComparer.OrdinalIgnoreCase);

    public Task<LegacyFetchResult> FetchAsync(Uri uri, string allowedHost, CancellationToken cancellationToken)
    {
        var normalizedHost = allowedHost.Trim().ToLowerInvariant();
        var client = _clients.GetOrAdd(normalizedHost, SafeHttpFetcher.CreateClient);
        return SafeHttpFetcher.FetchAsync(client, uri, normalizedHost, cancellationToken);
    }

    public void Dispose()
    {
        foreach (var client in _clients.Values)
            client.Dispose();
        _clients.Clear();
    }
}
