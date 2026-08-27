using System.Security.Cryptography;
using MunicipalPlatform.Api.Modules.Media.Domain;
using MunicipalPlatform.Api.Modules.Media.Services;
using SkiaSharp;

namespace MunicipalPlatform.Api.Tests.Media;

public sealed class MediaVariantServiceTests
{
    private readonly MediaVariantService _service = new();

    [Fact]
    public void WebpCapabilityIsAvailableOnSupportedRuntime()
    {
        Assert.Equal("AVAILABLE", _service.Capabilities.Webp.State);
        Assert.True(_service.Capabilities.Avif.State is "AVAILABLE" or "UNAVAILABLE");
    }

    [Fact]
    public void WebpVariantAppliesConfiguredCropAndRequestedAspect()
    {
        var source = CreateSourcePng();
        var asset = CreateAsset(source);
        asset.UpdatePresentation("banner, prefeitura", 0.75m, 0.5m, 0.10m, 0m, 0.80m, 1m);
        var descriptor = _service.Describe(asset, new MediaVariantRequest(320, 180, "webp"));

        var result = _service.Render(asset, source, descriptor);

        Assert.Equal("image/webp", result.MimeType);
        Assert.Equal("webp", result.Extension);
        Assert.Equal(320, result.Width);
        Assert.Equal(180, result.Height);
        Assert.NotEmpty(result.Bytes);
        using var decoded = SKBitmap.Decode(result.Bytes);
        Assert.NotNull(decoded);
        Assert.Equal(320, decoded.Width);
        Assert.Equal(180, decoded.Height);
    }

    [Fact]
    public void CacheKeyChangesWhenPresentationChanges()
    {
        var source = CreateSourcePng();
        var asset = CreateAsset(source);
        var first = _service.Describe(asset, new MediaVariantRequest(640, null, "webp"));

        asset.UpdatePresentation("destaque", 0.9m, 0.4m, 0.2m, 0.1m, 0.6m, 0.8m);
        var second = _service.Describe(asset, new MediaVariantRequest(640, null, "webp"));

        Assert.NotEqual(first.CacheKey, second.CacheKey);
    }

    [Fact]
    public void OversizedRequestedDimensionIsRejectedBeforeDecode()
    {
        var source = CreateSourcePng();
        var asset = CreateAsset(source);

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            _service.Describe(asset, new MediaVariantRequest(MediaVariantService.MaxRequestedDimension + 1, null, "webp")));

        Assert.Contains("2560", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AvifIsEitherEncodedOrReportedAsExplicitRuntimeCapability()
    {
        var source = CreateSourcePng();
        var asset = CreateAsset(source);
        var descriptor = _service.Describe(asset, new MediaVariantRequest(128, 128, "avif"));

        try
        {
            var result = _service.Render(asset, source, descriptor);
            Assert.Equal("image/avif", result.MimeType);
            Assert.NotEmpty(result.Bytes);
        }
        catch (MediaVariantFormatUnavailableException exception)
        {
            Assert.Contains("AVIF", exception.Message, StringComparison.Ordinal);
        }
    }

    private static MediaAsset CreateAsset(byte[] source)
    {
        var sha = Convert.ToHexString(SHA256.HashData(source)).ToLowerInvariant();
        var asset = new MediaAsset(
            Guid.NewGuid(),
            "media/tests/source.png",
            "source.png",
            "image/png",
            source.LongLength,
            sha,
            Guid.NewGuid());
        asset.Approve();
        return asset;
    }

    private static byte[] CreateSourcePng()
    {
        using var bitmap = new SKBitmap(new SKImageInfo(640, 360, SKColorType.Rgba8888, SKAlphaType.Premul));
        bitmap.Erase(SKColors.CornflowerBlue);
        using var data = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }
}
