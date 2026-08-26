namespace MunicipalPlatform.Api.Modules.Migration.Services;

public static class LegacyContentClassifier
{
    public static string Classify(string? mediaType, int statusCode, bool hasRedirect)
    {
        if (hasRedirect && statusCode is >= 300 and <= 399) return "REDIRECT";
        if (statusCode >= 400) return "IGNORE_WITH_REASON";
        if (string.IsNullOrWhiteSpace(mediaType)) return "IGNORE_WITH_REASON";
        if (mediaType.Equals("text/html", StringComparison.OrdinalIgnoreCase)) return "MIGRATE";
        if (mediaType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase)) return "MIGRATE";
        if (mediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)) return "MIGRATE";
        if (mediaType.Contains("json", StringComparison.OrdinalIgnoreCase)) return "INTEGRATE";
        if (mediaType.StartsWith("text/", StringComparison.OrdinalIgnoreCase)) return "MIGRATE";
        if (mediaType.Contains("officedocument", StringComparison.OrdinalIgnoreCase)
            || mediaType.Contains("msword", StringComparison.OrdinalIgnoreCase)
            || mediaType.Contains("spreadsheet", StringComparison.OrdinalIgnoreCase)) return "MIGRATE";
        return "IGNORE_WITH_REASON";
    }
}
