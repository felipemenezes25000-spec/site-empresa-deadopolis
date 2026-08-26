using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace MunicipalPlatform.Api.Modules.Migration.Services;

public static partial class LegacyPageExtractor
{
    public const int MaxExtractedCharacters = 700_000;

    public static LegacyPageExtraction Extract(byte[] body)
    {
        ArgumentNullException.ThrowIfNull(body);
        var html = Decode(body);
        var titleMatch = TitleRegex().Match(html);
        var title = titleMatch.Success ? NormalizeInline(WebUtility.HtmlDecode(StripTags(titleMatch.Groups["title"].Value))) : string.Empty;

        var withoutNoise = NoiseRegex().Replace(html, string.Empty);
        var withBreaks = BlockBreakRegex().Replace(withoutNoise, "\n");
        var text = WebUtility.HtmlDecode(StripTags(withBreaks));
        text = NormalizeMultiline(text);
        if (text.Length > MaxExtractedCharacters)
            throw new LegacyImportValidationException($"Conteúdo textual excede o limite seguro de {MaxExtractedCharacters} caracteres para importação no CMS.");

        return new LegacyPageExtraction(title, text);
    }

    private static string Decode(byte[] body)
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

    private static string StripTags(string value) => TagRegex().Replace(value, " ");

    private static string NormalizeInline(string value) => InlineWhitespaceRegex().Replace(value, " ").Trim();

    private static string NormalizeMultiline(string value)
    {
        var normalizedNewlines = value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        var lines = normalizedNewlines.Split('\n')
            .Select(line => InlineWhitespaceRegex().Replace(line, " ").Trim())
            .Where(line => line.Length > 0);
        return string.Join("\n", lines);
    }

    [GeneratedRegex("<title\\b[^>]*>(?<title>.*?)</title>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline)]
    private static partial Regex TitleRegex();

    [GeneratedRegex("<(script|style|noscript)\\b[^>]*>.*?</\\1\\s*>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline)]
    private static partial Regex NoiseRegex();

    [GeneratedRegex("<\\s*(?:br\\s*/?|/p|/div|/li|/h[1-6]|/tr|/section|/article|/header|/footer)\\s*>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex BlockBreakRegex();

    [GeneratedRegex("<[^>]+>", RegexOptions.CultureInvariant)]
    private static partial Regex TagRegex();

    [GeneratedRegex("[\\t\\f\\v \\u00a0]+", RegexOptions.CultureInvariant)]
    private static partial Regex InlineWhitespaceRegex();
}

public sealed record LegacyPageExtraction(string Title, string Text);
