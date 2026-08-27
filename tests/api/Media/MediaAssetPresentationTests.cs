using MunicipalPlatform.Api.Modules.Media.Domain;

namespace MunicipalPlatform.Api.Tests.Media;

public sealed class MediaAssetPresentationTests
{
    [Fact]
    public void PresentationNormalizesTagsAndPersistsFocalAndCropCoordinates()
    {
        var asset = Create("image/jpeg");

        asset.UpdatePresentation(" Obras, saúde, obras ", 0.25m, 0.75m, 0.10m, 0.20m, 0.60m, 0.50m);

        Assert.Equal("Obras, saúde", asset.TagsCsv);
        Assert.Equal(0.25m, asset.FocalPointX);
        Assert.Equal(0.75m, asset.FocalPointY);
        Assert.Equal(0.10m, asset.CropX);
        Assert.Equal(0.60m, asset.CropWidth);
    }

    [Fact]
    public void PresentationRejectsCropOutsideImageBounds()
    {
        var asset = Create("image/png");

        Assert.Throws<ArgumentException>(() =>
            asset.UpdatePresentation(null, 0.5m, 0.5m, 0.70m, 0.20m, 0.40m, 0.50m));
    }

    [Fact]
    public void PresentationRejectsPartialCropAndInvalidFocalPoint()
    {
        var asset = Create("image/webp");

        Assert.Throws<ArgumentException>(() =>
            asset.UpdatePresentation(null, 0.5m, 0.5m, 0.10m, null, 0.50m, 0.50m));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            asset.UpdatePresentation(null, 1.1m, 0.5m, null, null, null, null));
    }

    [Fact]
    public void PresentationIsNotAvailableForNonImages()
    {
        var asset = Create("application/pdf");

        Assert.Throws<InvalidOperationException>(() =>
            asset.UpdatePresentation("documento", 0.5m, 0.5m, null, null, null, null));
    }

    private static MediaAsset Create(string mimeType) => new(
        Guid.NewGuid(),
        $"media/{Guid.NewGuid():N}",
        "arquivo.bin",
        mimeType,
        128,
        new string('a', 64),
        Guid.NewGuid());
}
