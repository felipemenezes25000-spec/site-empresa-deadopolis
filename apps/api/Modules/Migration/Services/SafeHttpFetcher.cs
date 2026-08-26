using System.Net;
using System.Net.Sockets;
using MunicipalPlatform.Api.Modules.Migration.Security;

namespace MunicipalPlatform.Api.Modules.Migration.Services;

public sealed record LegacyFetchResult(
    int StatusCode,
    string? ContentType,
    byte[] Body,
    Uri? RedirectLocation);

public static class SafeHttpFetcher
{
    public const long MaxResponseBytes = 10L * 1024 * 1024;

    public static HttpClient CreateClient(string allowedHost)
    {
        var normalizedHost = allowedHost.Trim().ToLowerInvariant();
        var handler = new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            AutomaticDecompression = DecompressionMethods.None,
            ConnectTimeout = TimeSpan.FromSeconds(5),
            MaxResponseHeadersLength = 64,
            ConnectCallback = async (context, cancellationToken) =>
            {
                if (!string.Equals(context.DnsEndPoint.Host, normalizedHost, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Destino fora do host autorizado.");

                var addresses = await ExternalUrlSafety.ResolvePublicAddressesAsync(context.DnsEndPoint.Host, cancellationToken);
                var address = addresses[0];
                var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    await socket.ConnectAsync(address, context.DnsEndPoint.Port, cancellationToken);
                    return new NetworkStream(socket, ownsSocket: true);
                }
                catch
                {
                    socket.Dispose();
                    throw;
                }
            }
        };

        return new HttpClient(handler, disposeHandler: true)
        {
            Timeout = TimeSpan.FromSeconds(12)
        };
    }

    public static async Task<LegacyFetchResult> FetchAsync(
        HttpClient client,
        Uri uri,
        string allowedHost,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(client);
        if (!ExternalUrlSafety.IsAllowedUri(uri, allowedHost))
            throw new InvalidOperationException("URL recusada pela política de segurança do crawler.");

        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.UserAgent.ParseAdd("MunicipalPlatform-MigrationAudit/1.0");
        request.Headers.Accept.ParseAdd("text/html,application/pdf,image/*,application/json,text/plain;q=0.8,*/*;q=0.2");
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        Uri? redirect = null;
        if ((int)response.StatusCode is >= 300 and <= 399 && response.Headers.Location is not null)
            redirect = response.Headers.Location.IsAbsoluteUri ? response.Headers.Location : new Uri(uri, response.Headers.Location);

        var declaredLength = response.Content.Headers.ContentLength;
        if (declaredLength > MaxResponseBytes)
            throw new InvalidOperationException($"Resposta excede o limite de {MaxResponseBytes} bytes.");

        await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var output = new MemoryStream();
        var buffer = new byte[16 * 1024];
        while (true)
        {
            var read = await input.ReadAsync(buffer.AsMemory(), cancellationToken);
            if (read == 0)
                break;
            if (output.Length + read > MaxResponseBytes)
                throw new InvalidOperationException($"Resposta excede o limite de {MaxResponseBytes} bytes.");
            output.Write(buffer, 0, read);
        }

        return new LegacyFetchResult(
            (int)response.StatusCode,
            response.Content.Headers.ContentType?.MediaType,
            output.ToArray(),
            redirect);
    }
}
