namespace MunicipalPlatform.Api.Modules.Migration.Services;

public interface ILegacySourceFetcher
{
    Task<LegacyFetchResult> FetchAsync(Uri uri, string allowedHost, CancellationToken cancellationToken);
}

public sealed class SafeLegacySourceFetcher : ILegacySourceFetcher
{
    public async Task<LegacyFetchResult> FetchAsync(Uri uri, string allowedHost, CancellationToken cancellationToken)
    {
        using var client = SafeHttpFetcher.CreateClient(allowedHost);
        return await SafeHttpFetcher.FetchAsync(client, uri, allowedHost, cancellationToken);
    }
}
