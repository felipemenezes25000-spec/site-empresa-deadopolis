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
}
