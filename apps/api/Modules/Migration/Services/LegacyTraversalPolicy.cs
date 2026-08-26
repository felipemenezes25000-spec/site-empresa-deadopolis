namespace MunicipalPlatform.Api.Modules.Migration.Services;

public static class LegacyTraversalPolicy
{
    public static bool ShouldContinuePagination(Uri current, string html)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(html);

        var isNewsListing = current.AbsolutePath.EndsWith("/noticias.php", StringComparison.OrdinalIgnoreCase)
            || current.AbsolutePath.EndsWith("/noticias25.php", StringComparison.OrdinalIgnoreCase);
        var isPagination = current.Query.Contains("page=", StringComparison.OrdinalIgnoreCase)
            || current.Query.Contains("pagina=", StringComparison.OrdinalIgnoreCase);
        if (!isNewsListing || !isPagination)
            return true;

        return html.Contains("exibe.php?id=", StringComparison.OrdinalIgnoreCase)
            || html.Contains("exibe23.php?id=", StringComparison.OrdinalIgnoreCase);
    }

    public static int GetNextDepth(Uri current, Uri candidate, int currentDepth)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentOutOfRangeException.ThrowIfNegative(currentDepth);

        var samePageFamily = string.Equals(current.IdnHost, candidate.IdnHost, StringComparison.OrdinalIgnoreCase)
            && string.Equals(current.AbsolutePath, candidate.AbsolutePath, StringComparison.OrdinalIgnoreCase);

        // Paginação e filtros do legado vivem no query string (ex.: noticias.php?page=322).
        // Eles pertencem ao mesmo nível estrutural da página de listagem e não devem
        // consumir MaxDepth; MaxPages e a deduplicação de URI continuam limitando o crawl.
        return samePageFamily ? currentDepth : checked(currentDepth + 1);
    }
}
