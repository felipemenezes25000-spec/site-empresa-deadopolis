using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using MunicipalPlatform.Api.Modules.Media.Domain;
using SkiaSharp;

namespace MunicipalPlatform.Api.Modules.Media.Services;

public sealed class MediaVariantService
{
    public const int MinRequestedDimension = 64;
    public const int MaxRequestedDimension = 2560;
    public const long MaxSourcePixels = 40_000_000;
    public const long MaxOutputPixels = 8_000_000;
    private const int EncodeQuality = 82;

    public MediaVariantDescriptor Describe(MediaAsset asset, MediaVariantRequest request)
    {
        ArgumentNullException.ThrowIfNull(asset);
        ArgumentNullException.ThrowIfNull(request);
        ValidateRequestedDimension(request.Width, nameof(request.Width));
        if (request.Height.HasValue) ValidateRequestedDimension(request.Height.Value, nameof(request.Height));

        var normalizedFormat = NormalizeFormat(request.Format);
        var (mimeType, extension, _) = ResolveFormat(normalizedFormat);
        var canonical = string.Join(
            '|',
            asset.Sha256,
            request.Width.ToString(CultureInfo.InvariantCulture),
            request.Height?.ToString(CultureInfo.InvariantCulture) ?? "auto",
            normalizedFormat,
            FormatDecimal(asset.FocalPointX),
            FormatDecimal(asset.FocalPointY),
            FormatDecimal(asset.CropX),
            FormatDecimal(asset.CropY),
            FormatDecimal(asset.CropWidth),
            FormatDecimal(asset.CropHeight));
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
        var sourcePrefix = asset.Sha256.Length >= 12 ? asset.Sha256[..12] : asset.Sha256;
        var cacheKey = $"media/variants/{asset.Id:N}/{sourcePrefix}/{fingerprint[..20]}.{extension}";
        return new MediaVariantDescriptor(request.Width, request.Height, normalizedFormat, mimeType, extension, cacheKey);
    }

    public MediaVariantResult Render(MediaAsset asset, byte[] sourceBytes, MediaVariantDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(asset);
        ArgumentNullException.ThrowIfNull(sourceBytes);
        ArgumentNullException.ThrowIfNull(descriptor);
        if (sourceBytes.Length == 0) throw new InvalidDataException("A imagem original está vazia.");
        if (sourceBytes.LongLength > DocumentFileInspector.MaxBytes)
            throw new InvalidDataException("A imagem original excede o limite operacional de 25 MB.");

        using var encodedSource = SKData.CreateCopy(sourceBytes);
        using var codec = SKCodec.Create(encodedSource) ?? throw new InvalidDataException("O conteúdo não pôde ser decodificado como imagem.");
        var sourceInfo = codec.Info;
        if (sourceInfo.Width <= 0 || sourceInfo.Height <= 0)
            throw new InvalidDataException("A imagem original possui dimensões inválidas.");
        if ((long)sourceInfo.Width * sourceInfo.Height > MaxSourcePixels)
            throw new InvalidDataException("A imagem original excede o limite de 40 megapixels para transformação interativa.");

        using var source = SKBitmap.Decode(codec) ?? throw new InvalidDataException("Falha ao decodificar os pixels da imagem original.");
        var sourceRect = ResolveSourceRect(asset, source.Width, source.Height, descriptor.Width, descriptor.Height);
        if (sourceRect.Width < 1f || sourceRect.Height < 1f)
            throw new InvalidDataException("O recorte configurado é pequeno demais para gerar uma variante válida.");

        var targetHeight = descriptor.Height ?? CalculateProportionalHeight(descriptor.Width, sourceRect);
        if (targetHeight <= 0 || targetHeight > MaxRequestedDimension)
            throw new ArgumentOutOfRangeException(nameof(descriptor), $"A proporção solicitada resultaria em altura fora do limite de {MaxRequestedDimension}px.");
        if ((long)descriptor.Width * targetHeight > MaxOutputPixels)
            throw new ArgumentOutOfRangeException(nameof(descriptor), "A variante excede o limite de 8 megapixels de saída.");

        using var output = new SKBitmap(new SKImageInfo(descriptor.Width, targetHeight, SKColorType.Rgba8888, SKAlphaType.Premul));
        using var canvas = new SKCanvas(output);
        canvas.Clear(SKColors.Transparent);
        var destination = new SKRect(0, 0, descriptor.Width, targetHeight);
        var sampling = new SKSamplingOptions(SKCubicResampler.Mitchell);
        canvas.DrawBitmap(source, sourceRect, destination, sampling);
        canvas.Flush();

        var (_, _, encodedFormat) = ResolveFormat(descriptor.Format);
        SKData encoded;
        try
        {
            encoded = output.Encode(encodedFormat, EncodeQuality);
        }
        catch (InvalidOperationException exception)
        {
            throw new MediaVariantFormatUnavailableException(descriptor.Format, exception);
        }

        using (encoded)
        {
            if (encoded.Size <= 0)
                throw new MediaVariantFormatUnavailableException(descriptor.Format);
            return new MediaVariantResult(encoded.ToArray(), descriptor.MimeType, descriptor.Extension, descriptor.Width, targetHeight);
        }
    }

    private static SKRect ResolveSourceRect(MediaAsset asset, int sourceWidth, int sourceHeight, int targetWidth, int? targetHeight)
    {
        var cropX = (float)(asset.CropX ?? 0m);
        var cropY = (float)(asset.CropY ?? 0m);
        var cropWidth = (float)(asset.CropWidth ?? 1m);
        var cropHeight = (float)(asset.CropHeight ?? 1m);
        var baseRect = new SKRect(
            cropX * sourceWidth,
            cropY * sourceHeight,
            (cropX + cropWidth) * sourceWidth,
            (cropY + cropHeight) * sourceHeight);

        if (!targetHeight.HasValue) return baseRect;

        var focalX = (float)(asset.FocalPointX ?? 0.5m) * sourceWidth;
        var focalY = (float)(asset.FocalPointY ?? 0.5m) * sourceHeight;
        var targetAspect = (float)targetWidth / targetHeight.Value;
        return FitAspect(baseRect, targetAspect, focalX, focalY);
    }

    private static SKRect FitAspect(SKRect bounds, float targetAspect, float focalX, float focalY)
    {
        if (targetAspect <= 0f) throw new ArgumentOutOfRangeException(nameof(targetAspect));
        var currentAspect = bounds.Width / bounds.Height;
        if (Math.Abs(currentAspect - targetAspect) < 0.0001f) return bounds;

        if (currentAspect > targetAspect)
        {
            var width = bounds.Height * targetAspect;
            var left = Math.Clamp(focalX - (width / 2f), bounds.Left, bounds.Right - width);
            return new SKRect(left, bounds.Top, left + width, bounds.Bottom);
        }

        var height = bounds.Width / targetAspect;
        var top = Math.Clamp(focalY - (height / 2f), bounds.Top, bounds.Bottom - height);
        return new SKRect(bounds.Left, top, bounds.Right, top + height);
    }

    private static int CalculateProportionalHeight(int targetWidth, SKRect sourceRect) =>
        Math.Max(1, (int)Math.Round(targetWidth * sourceRect.Height / sourceRect.Width, MidpointRounding.AwayFromZero));

    private static void ValidateRequestedDimension(int value, string parameterName)
    {
        if (value < MinRequestedDimension || value > MaxRequestedDimension)
            throw new ArgumentOutOfRangeException(parameterName, $"Use dimensões entre {MinRequestedDimension}px e {MaxRequestedDimension}px.");
    }

    private static string NormalizeFormat(string format)
    {
        if (string.IsNullOrWhiteSpace(format)) throw new ArgumentException("Formato obrigatório.", nameof(format));
        var normalized = format.Trim().ToLowerInvariant();
        return normalized is "webp" or "avif"
            ? normalized
            : throw new ArgumentException("Use WEBP ou AVIF para variantes derivadas.", nameof(format));
    }

    private static (string MimeType, string Extension, SKEncodedImageFormat EncodedFormat) ResolveFormat(string format) =>
        format switch
        {
            "webp" => ("image/webp", "webp", SKEncodedImageFormat.Webp),
            "avif" => ("image/avif", "avif", SKEncodedImageFormat.Avif),
            _ => throw new ArgumentException("Formato de variante não suportado.", nameof(format))
        };

    private static string FormatDecimal(decimal? value) =>
        value?.ToString("0.######", CultureInfo.InvariantCulture) ?? "-";
}

public sealed record MediaVariantRequest(int Width, int? Height, string Format);

public sealed record MediaVariantDescriptor(
    int Width,
    int? Height,
    string Format,
    string MimeType,
    string Extension,
    string CacheKey);

public sealed record MediaVariantResult(byte[] Bytes, string MimeType, string Extension, int Width, int Height);

public sealed class MediaVariantFormatUnavailableException : InvalidOperationException
{
    public MediaVariantFormatUnavailableException(string format)
        : base($"O codec {format.ToUpperInvariant()} não está disponível neste runtime de imagem.") { }

    public MediaVariantFormatUnavailableException(string format, Exception innerException)
        : base($"O codec {format.ToUpperInvariant()} não está disponível neste runtime de imagem.", innerException) { }
}
