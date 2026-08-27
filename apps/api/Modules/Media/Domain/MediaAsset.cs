using MunicipalPlatform.Api.Platform.Tenancy;

namespace MunicipalPlatform.Api.Modules.Media.Domain;

public sealed class MediaAsset : ITenantEntity
{
    private MediaAsset() { }

    public MediaAsset(
        Guid municipalityId,
        string objectKey,
        string originalFileName,
        string mimeType,
        long sizeBytes,
        string sha256,
        Guid uploadedBy)
    {
        Id = Guid.NewGuid();
        MunicipalityId = municipalityId;
        ObjectKey = objectKey.Trim();
        OriginalFileName = originalFileName.Trim();
        MimeType = mimeType.Trim().ToLowerInvariant();
        SizeBytes = sizeBytes;
        Sha256 = sha256.Trim().ToLowerInvariant();
        UploadedBy = uploadedBy;
        UploadedAt = DateTimeOffset.UtcNow;
        Status = "QUARANTINED";
        FocalPointX = 0.5m;
        FocalPointY = 0.5m;
    }

    public Guid Id { get; private set; }
    public Guid MunicipalityId { get; private set; }
    public string ObjectKey { get; private set; } = string.Empty;
    public string OriginalFileName { get; private set; } = string.Empty;
    public string MimeType { get; private set; } = string.Empty;
    public long SizeBytes { get; private set; }
    public string Sha256 { get; private set; } = string.Empty;
    public string Status { get; private set; } = string.Empty;
    public string AltText { get; private set; } = string.Empty;
    public string Caption { get; private set; } = string.Empty;
    public string Credit { get; private set; } = string.Empty;
    public string TagsCsv { get; private set; } = string.Empty;
    public decimal? FocalPointX { get; private set; }
    public decimal? FocalPointY { get; private set; }
    public decimal? CropX { get; private set; }
    public decimal? CropY { get; private set; }
    public decimal? CropWidth { get; private set; }
    public decimal? CropHeight { get; private set; }
    public Guid UploadedBy { get; private set; }
    public DateTimeOffset UploadedAt { get; private set; }

    public void UpdateMetadata(string? altText, string? caption, string? credit)
    {
        AltText = Trim(altText, 500);
        Caption = Trim(caption, 1000);
        Credit = Trim(credit, 500);
    }

    public void UpdatePresentation(
        string? tags,
        decimal focalPointX,
        decimal focalPointY,
        decimal? cropX,
        decimal? cropY,
        decimal? cropWidth,
        decimal? cropHeight)
    {
        if (!MimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Ponto focal e recorte só podem ser definidos para imagens.");

        EnsureUnitInterval(focalPointX, nameof(focalPointX));
        EnsureUnitInterval(focalPointY, nameof(focalPointY));
        ValidateCrop(cropX, cropY, cropWidth, cropHeight);

        TagsCsv = NormalizeTags(tags);
        FocalPointX = focalPointX;
        FocalPointY = focalPointY;
        CropX = cropX;
        CropY = cropY;
        CropWidth = cropWidth;
        CropHeight = cropHeight;
    }

    public void Approve() => Status = "APPROVED";
    public void Reject() => Status = "REJECTED";

    private static void ValidateCrop(decimal? x, decimal? y, decimal? width, decimal? height)
    {
        var values = new[] { x, y, width, height };
        var definedCount = values.Count(value => value.HasValue);
        if (definedCount == 0) return;
        if (definedCount != values.Length)
            throw new ArgumentException("O recorte deve informar X, Y, largura e altura juntos.");

        EnsureUnitInterval(x!.Value, nameof(x));
        EnsureUnitInterval(y!.Value, nameof(y));
        if (width!.Value <= 0 || width > 1)
            throw new ArgumentOutOfRangeException(nameof(width), "A largura do recorte deve ser maior que 0 e menor ou igual a 1.");
        if (height!.Value <= 0 || height > 1)
            throw new ArgumentOutOfRangeException(nameof(height), "A altura do recorte deve ser maior que 0 e menor ou igual a 1.");
        if (x.Value + width.Value > 1 || y.Value + height.Value > 1)
            throw new ArgumentException("O recorte precisa permanecer dentro dos limites normalizados da imagem.");
    }

    private static string NormalizeTags(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        if (value.Length > 2_000) throw new ArgumentException("Lista de tags excessivamente longa.", nameof(value));

        var tags = value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(tag => tag.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (tags.Length > 20) throw new ArgumentException("Use no máximo 20 tags.", nameof(value));
        if (tags.Any(tag => tag.Length > 50 || tag.Any(char.IsControl)))
            throw new ArgumentException("Cada tag deve ter até 50 caracteres e não pode conter caracteres de controle.", nameof(value));
        return string.Join(", ", tags);
    }

    private static void EnsureUnitInterval(decimal value, string parameterName)
    {
        if (value < 0 || value > 1)
            throw new ArgumentOutOfRangeException(parameterName, "O valor deve estar entre 0 e 1.");
    }

    private static string Trim(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var normalized = value.Trim();
        if (normalized.Length > max) throw new ArgumentException($"Máximo de {max} caracteres.");
        return normalized;
    }
}
