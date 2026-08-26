using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using MunicipalPlatform.Api.Modules.Migration.Security;
using MunicipalPlatform.Api.Modules.Operations.Domain;

namespace MunicipalPlatform.Api.Modules.Operations;

public sealed class LinkCheckProbeService
{
    public static bool TryNormalizeTarget(string rawUrl, out Uri uri, out string error)
    {
        uri = null!;
        error = string.Empty;
        if (!Uri.TryCreate(rawUrl.Trim(), UriKind.Absolute, out var parsed))
        {
            error = "URL HTTP/HTTPS absoluta obrigatória.";
            return false;
        }

        var host = parsed.IdnHost.Trim().ToLowerInvariant();
        if (host.Length == 0 || !ExternalUrlSafety.IsAllowedUri(parsed, host))
        {
            error = "A URL deve usar HTTP/HTTPS, sem credenciais e somente portas 80/443.";
            return false;
        }

        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            || host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase))
        {
            error = "Host local bloqueado pela política SSRF.";
            return false;
        }

        if (IPAddress.TryParse(host, out var address) && !ExternalUrlSafety.IsPublicAddress(address))
        {
            error = "Endereço IP privado, local ou reservado bloqueado pela política SSRF.";
            return false;
        }

        uri = new UriBuilder(parsed) { Fragment = string.Empty }.Uri;
        return true;
    }

    public async Task CheckAsync(LinkCheck link, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(link);
        var now = DateTimeOffset.UtcNow;
        if (!TryNormalizeTarget(link.Url, out var uri, out var validationError))
        {
            link.RecordFailure(validationError, now);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        try
        {
            using var client = CreateClient(uri.IdnHost);
            var statusCode = await SendProbeAsync(client, uri, cancellationToken);
            stopwatch.Stop();
            link.RecordSuccess(statusCode, stopwatch.ElapsedMilliseconds, DateTimeOffset.UtcNow);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            link.RecordFailure("Tempo limite excedido durante a verificação do link.", DateTimeOffset.UtcNow);
        }
        catch (Exception exception) when (exception is HttpRequestException or IOException or InvalidOperationException or SocketException)
        {
            stopwatch.Stop();
            var message = exception.Message.Length > 500 ? exception.Message[..500] : exception.Message;
            link.RecordFailure(message, DateTimeOffset.UtcNow);
        }
    }

    private static HttpClient CreateClient(string allowedHost)
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
                    throw new InvalidOperationException("Destino fora do host autorizado pelo monitor de links.");

                var addresses = await ExternalUrlSafety.ResolvePublicAddressesAsync(context.DnsEndPoint.Host, cancellationToken);
                Exception? lastFailure = null;
                foreach (var address in addresses)
                {
                    var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    try
                    {
                        await socket.ConnectAsync(address, context.DnsEndPoint.Port, cancellationToken);
                        return new NetworkStream(socket, ownsSocket: true);
                    }
                    catch (Exception exception) when (exception is SocketException or OperationCanceledException)
                    {
                        socket.Dispose();
                        lastFailure = exception;
                        if (exception is OperationCanceledException) throw;
                    }
                }

                throw new HttpRequestException("Não foi possível estabelecer conexão com os endereços públicos resolvidos.", lastFailure);
            }
        };

        return new HttpClient(handler, disposeHandler: true)
        {
            Timeout = TimeSpan.FromSeconds(12)
        };
    }

    private static async Task<int> SendProbeAsync(HttpClient client, Uri uri, CancellationToken cancellationToken)
    {
        using var headRequest = CreateRequest(HttpMethod.Head, uri);
        using var headResponse = await client.SendAsync(headRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (headResponse.StatusCode is not (HttpStatusCode.MethodNotAllowed or HttpStatusCode.NotImplemented))
            return (int)headResponse.StatusCode;

        using var getRequest = CreateRequest(HttpMethod.Get, uri);
        using var getResponse = await client.SendAsync(getRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        return (int)getResponse.StatusCode;
    }

    private static HttpRequestMessage CreateRequest(HttpMethod method, Uri uri)
    {
        var request = new HttpRequestMessage(method, uri);
        request.Headers.UserAgent.ParseAdd("MunicipalPlatform-LinkMonitor/1.0");
        request.Headers.Accept.ParseAdd("*/*");
        return request;
    }
}
