using System.Text.Json;

namespace MunicipalPlatform.Api.Modules.Content.Domain;

internal static class PageBlockPayloadValidator
{
    private static readonly HashSet<string> AllowedTypes = new(StringComparer.Ordinal)
    {
        "Hero", "ServiceSearch", "QuickAccess", "FeaturedNews", "NewsGrid", "ServiceGrid",
        "DepartmentGrid", "Events", "Banner", "Alert", "Documents", "Statistics", "Contact",
        "Video", "Gallery", "CustomLinks"
    };

    public static void Validate(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
            throw new ArgumentException("O payload da página deve ser um objeto JSON com blocos governados.");
        if (!root.TryGetProperty("blocks", out var blocks)) return;
        if (blocks.ValueKind != JsonValueKind.Array)
            throw new ArgumentException("A coleção de blocos da página deve ser uma lista.");
        if (blocks.GetArrayLength() > 30)
            throw new ArgumentException("A página aceita no máximo 30 blocos.");

        foreach (var block in blocks.EnumerateArray()) ValidateBlock(block);
    }

    private static void ValidateBlock(JsonElement block)
    {
        if (block.ValueKind != JsonValueKind.Object)
            throw new ArgumentException("Cada bloco da página deve ser um objeto.");
        var type = String(block, "type", 32, required: true);
        if (!AllowedTypes.Contains(type))
            throw new ArgumentException($"Tipo de bloco não permitido: {type}.");

        String(block, "id", 80);
        String(block, "title", 220);
        String(block, "content", 4_000);
        String(block, "linkLabel", 120);
        var reference = String(block, "reference", 2_048);
        if (!string.IsNullOrEmpty(reference) && !IsSafeHref(reference))
            throw new ArgumentException("A referência do bloco deve usar uma rota interna ou HTTP(S).");

        var imageUrl = String(block, "imageUrl", 2_048);
        var imageAlt = String(block, "imageAlt", 500);
        if (!string.IsNullOrEmpty(imageUrl) && !IsInternalMediaUrl(imageUrl))
            throw new ArgumentException("A imagem do bloco deve referenciar mídia interna aprovada.");
        if (!string.IsNullOrEmpty(imageUrl) && string.IsNullOrEmpty(imageAlt))
            throw new ArgumentException("O bloco com imagem deve informar texto alternativo.");

        if (!block.TryGetProperty("items", out var items)) return;
        if (items.ValueKind != JsonValueKind.Array)
            throw new ArgumentException("Os itens do bloco devem formar uma lista.");
        if (items.GetArrayLength() > 24)
            throw new ArgumentException("Cada bloco aceita no máximo 24 itens.");
        foreach (var item in items.EnumerateArray()) ValidateItem(item);
    }

    private static void ValidateItem(JsonElement item)
    {
        if (item.ValueKind != JsonValueKind.Object)
            throw new ArgumentException("Cada item de bloco deve ser um objeto.");
        String(item, "id", 80);
        String(item, "label", 220);
        String(item, "description", 1_000);
        String(item, "value", 120);
        String(item, "date", 40);
        var url = String(item, "url", 2_048);
        if (!string.IsNullOrEmpty(url) && !IsSafeHref(url))
            throw new ArgumentException("O destino do item de bloco deve usar uma rota interna ou HTTP(S).");
        var mediaUrl = String(item, "mediaUrl", 2_048);
        var mediaAlt = String(item, "mediaAlt", 500);
        if (!string.IsNullOrEmpty(mediaUrl) && !IsInternalMediaUrl(mediaUrl))
            throw new ArgumentException("A mídia do item de bloco deve usar a biblioteca interna.");
        if (!string.IsNullOrEmpty(mediaUrl) && string.IsNullOrEmpty(mediaAlt))
            throw new ArgumentException("O item de bloco com imagem deve informar texto alternativo.");
    }

    private static string String(JsonElement parent, string name, int maxLength, bool required = false)
    {
        if (!parent.TryGetProperty(name, out var property))
        {
            if (required) throw new ArgumentException($"O bloco deve informar {name}.");
            return string.Empty;
        }
        if (property.ValueKind != JsonValueKind.String)
            throw new ArgumentException($"O campo {name} do bloco deve ser texto.");
        var value = property.GetString()?.Trim() ?? string.Empty;
        if (required && value.Length == 0) throw new ArgumentException($"O bloco deve informar {name}.");
        if (value.Length > maxLength) throw new ArgumentException($"O campo {name} do bloco excede {maxLength} caracteres.");
        return value;
    }

    private static bool IsSafeHref(string value)
    {
        if (value.StartsWith('/') && !value.StartsWith("//", StringComparison.Ordinal)) return true;
        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    private static bool IsInternalMediaUrl(string value)
    {
        const string prefix = "/api/v1/media/";
        if (!value.StartsWith(prefix, StringComparison.Ordinal)) return false;
        var identifier = value[prefix.Length..].Split('?', 2)[0];
        return Guid.TryParseExact(identifier, "D", out _);
    }
}
