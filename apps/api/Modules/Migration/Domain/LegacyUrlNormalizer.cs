using System.Text.RegularExpressions;

namespace MunicipalPlatform.Api.Modules.Migration.Domain;

public static partial class LegacyUrlNormalizer
{
    private static readonly HashSet<string> TrackingParameters = new(StringComparer.OrdinalIgnoreCase)
    {
        "utm_source",
        "utm_medium",
        "utm_campaign",
        "fbclid",
        "gclid",
        "PHPSESSID"
    };

    public static string Normalize(string input)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(input);
        var trimmed = input.Trim();
        Uri uri;
        if (Uri.TryCreate(trimmed, UriKind.Absolute, out var absolute)
            && (absolute.Scheme == Uri.UriSchemeHttp || absolute.Scheme == Uri.UriSchemeHttps))
        {
            uri = absolute;
        }
        else
        {
            uri = new Uri(new Uri("https://www.deodapolis.ms.gov.br", UriKind.Absolute), trimmed);
        }

        var path = DuplicateSlashRegex().Replace(uri.AbsolutePath, "/");
        var parameters = uri.Query.TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Split('=', 2))
            .Where(parts => !TrackingParameters.Contains(Uri.UnescapeDataString(parts[0])))
            .OrderBy(parts => parts[0], StringComparer.Ordinal)
            .Select(parts => string.Join('=', parts));
        var query = string.Join('&', parameters);

        return query.Length == 0 ? path : $"{path}?{query}";
    }

    [GeneratedRegex("/{2,}", RegexOptions.CultureInvariant)]
    private static partial Regex DuplicateSlashRegex();
}
