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
        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
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
            if ((bytes[0] & 0xFE) == 0xFC)
                return false;
            if (bytes.AsSpan(0, 8).SequenceEqual(new byte[8]))
                return false;
            if (bytes[0] == 0x01 && bytes.AsSpan(1, 7).SequenceEqual(new byte[7]))
                return false;
            if (bytes[0] == 0x20 && bytes[1] == 0x01 && bytes[2] == 0x0D && bytes[3] == 0xB8)
                return false;
            if (bytes[0] == 0x3F && bytes[1] == 0xFF && (bytes[2] & 0xF0) == 0)
                return false;
            return true;
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
        if (first == 192 && second is 0 or 2)
            return false;
        if (first == 192 && second == 88 && octets[2] == 99)
            return false;
        if (first == 198 && second is 18 or 19)
            return false;
        if (first == 198 && second == 51 && octets[2] == 100)
            return false;
        if (first == 203 && second == 0 && octets[2] == 113)
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
