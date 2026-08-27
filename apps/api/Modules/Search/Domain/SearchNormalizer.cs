using System.Globalization;
using System.Text;

namespace MunicipalPlatform.Api.Modules.Search.Domain;

public static class SearchNormalizer
{
    public static string Normalize(string input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var decomposed = input.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        var pendingSpace = false;

        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsWhiteSpace(character))
            {
                pendingSpace = builder.Length > 0;
                continue;
            }

            if (pendingSpace)
            {
                builder.Append(' ');
                pendingSpace = false;
            }

            builder.Append(character);
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    public static string[] Tokenize(string input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var normalized = Normalize(input);
        if (normalized.Length == 0) return [];

        var builder = new StringBuilder(normalized.Length);
        foreach (var character in normalized)
        {
            builder.Append(char.IsLetterOrDigit(character) ? character : ' ');
        }

        return builder.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    public static bool IsDirectMatch(string query, string title, string? description = null)
    {
        var normalizedQuery = Normalize(query);
        if (normalizedQuery.Length == 0) return false;
        var normalizedTitle = Normalize(title);
        var normalizedDescription = Normalize(description ?? string.Empty);
        if (normalizedTitle.Contains(normalizedQuery, StringComparison.Ordinal)
            || normalizedDescription.Contains(normalizedQuery, StringComparison.Ordinal))
        {
            return true;
        }

        var queryTokens = Tokenize(query);
        if (queryTokens.Length == 0) return false;
        var candidateTokens = Tokenize($"{title} {description}");
        return queryTokens.All(queryToken => candidateTokens.Contains(queryToken, StringComparer.Ordinal));
    }

    public static int Score(string query, string title, string? description = null)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(title);

        var normalizedQuery = Normalize(query);
        if (normalizedQuery.Length == 0) return 0;

        var normalizedTitle = Normalize(title);
        var normalizedDescription = Normalize(description ?? string.Empty);
        var score = 0;

        if (string.Equals(normalizedTitle, normalizedQuery, StringComparison.Ordinal)) score += 1_200;
        else if (normalizedTitle.StartsWith(normalizedQuery, StringComparison.Ordinal)) score += 900;
        else if (normalizedTitle.Contains(normalizedQuery, StringComparison.Ordinal)) score += 700;

        if (normalizedDescription.Contains(normalizedQuery, StringComparison.Ordinal)) score += 300;

        var queryTokens = Tokenize(query);
        if (queryTokens.Length == 0) return score;
        var titleTokens = Tokenize(title);
        var descriptionTokens = Tokenize(description ?? string.Empty);
        var matchedTokens = 0;

        foreach (var queryToken in queryTokens)
        {
            if (titleTokens.Contains(queryToken, StringComparer.Ordinal))
            {
                score += 160;
                matchedTokens++;
                continue;
            }

            if (descriptionTokens.Contains(queryToken, StringComparer.Ordinal))
            {
                score += 80;
                matchedTokens++;
                continue;
            }

            var distance = MinimumDistance(queryToken, titleTokens, descriptionTokens);
            var allowedDistance = queryToken.Length switch
            {
                <= 3 => 0,
                <= 5 => 1,
                _ => 2
            };
            if (distance <= allowedDistance)
            {
                score += Math.Max(25, 120 - (distance * 35));
                matchedTokens++;
            }
        }

        if (matchedTokens == queryTokens.Length) score += 150;
        return score;
    }

    public static int LevenshteinDistance(string left, string right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        if (left.Length == 0) return right.Length;
        if (right.Length == 0) return left.Length;

        var previous = new int[right.Length + 1];
        var current = new int[right.Length + 1];
        for (var column = 0; column <= right.Length; column++) previous[column] = column;

        for (var row = 1; row <= left.Length; row++)
        {
            current[0] = row;
            for (var column = 1; column <= right.Length; column++)
            {
                var substitutionCost = left[row - 1] == right[column - 1] ? 0 : 1;
                current[column] = Math.Min(
                    Math.Min(current[column - 1] + 1, previous[column] + 1),
                    previous[column - 1] + substitutionCost);
            }

            (previous, current) = (current, previous);
        }

        return previous[right.Length];
    }

    private static int MinimumDistance(string token, string[] titleTokens, string[] descriptionTokens)
    {
        var minimum = int.MaxValue;
        foreach (var candidate in titleTokens)
        {
            minimum = Math.Min(minimum, LevenshteinDistance(token, candidate));
            if (minimum == 0) return 0;
        }

        foreach (var candidate in descriptionTokens)
        {
            minimum = Math.Min(minimum, LevenshteinDistance(token, candidate));
            if (minimum == 0) return 0;
        }

        return minimum;
    }
}
