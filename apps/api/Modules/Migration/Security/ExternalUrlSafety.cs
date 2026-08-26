using System.Net;
using System.Net.Sockets;

namespace MunicipalPlatform.Api.Modules.Migration.Security;

public static class ExternalUrlSafety
{
    public static bool IsAllowedUri(Uri uri, string allowedHost)
    {
        ArgumentNullException.ThrowIfNull(uri);
        if (!uri.IsAbsoluteUri)
            return false;
        if (uri.Scheme is not (Uri.UriSchemeHttp or Uri.UriSchemeHttps))
            return false;
        if (!string.IsNullOrEmpty(uri.UserInfo))
            return false;
        if (!uri.IsDefaultPort && uri.Port is not (80 or 443))
            return false;
        return string.Equals(uri.IdnHost, allowedHost.Trim().ToLowerInvariant(), StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsPublicAddress(IPAddress address)
    {
        ArgumentNullException.ThrowIfNull(address);
        if (IPAddress.IsLoopback(address) || address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any))
            return false;
        if (address.IsIPv4MappedToIPv6)
            return IsPublicAddress(address.MapToIPv4());

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.IsIPv6Multicast)
                return false;
            var bytes = address.GetAddressBytes();
            return (bytes[0] & 0xFE) != 0xFC;
        }

        if (address.AddressFamily != AddressFamily.InterNetwork)
            return false;
        var octets = address.GetAddressBytes();
        var first = octets[0];
        var second = octets[1];

        if (first is 0 or 10 or 127)
            return false;
        if (first == 100 && second is >= 64 and <= 127)
            return false;
        if (first == 169 && second == 254)
            return false;
        if (first == 172 && second is >= 16 and <= 31)
            return false;
        if (first == 192 && second == 168)
            return false;
        if (first == 198 && second is 18 or 19)
            return false;
        if (first >= 224)
            return false;
        return true;
    }

    public static async Task<IPAddress[]> ResolvePublicAddressesAsync(string host, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(host))
            throw new ArgumentException("Host obrigatório.", nameof(host));
        var addresses = await Dns.GetHostAddressesAsync(host, cancellationToken);
        if (addresses.Length == 0)
            throw new InvalidOperationException("Host não resolveu para nenhum endereço IP.");
        if (addresses.Any(address => !IsPublicAddress(address)))
            throw new InvalidOperationException("Host resolveu para endereço privado, local ou reservado; requisição bloqueada por proteção SSRF.");
        return addresses;
    }
}
