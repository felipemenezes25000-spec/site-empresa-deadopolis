using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using MunicipalPlatform.Api.Infrastructure.Persistence;
using MunicipalPlatform.Api.Modules.Media.Services;
using MunicipalPlatform.Api.Platform.Storage;

namespace MunicipalPlatform.Api.Modules.Media;

public static class MediaVariantEndpoints
{
    public static IEndpointRouteBuilder MapMediaVariantEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/media/{id:guid}/variant", ReadVariantAsync)
            .AllowAnonymous()
            .WithTags("Media");
        return endpoints;
    }

    private static async Task<IResult> ReadVariantAsync(
        Guid id,
        int width,
        int? height,
        string format,
        HttpContext context,
        ApplicationDbContext db,
        IObjectStorageProvider storage,
        MediaVariantService variants,
        CancellationToken ct)
    {
        var asset = await db.MediaAssets.AsNoTracking().SingleOrDefaultAsync(
            item => item.Id == id && item.Status == "APPROVED",
            ct);
        if (asset is null) return Results.NotFound();
        if (!asset.MimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            return Results.Problem(title: "Variante indisponível", detail: "Somente assets de imagem podem gerar variantes responsivas.", statusCode: StatusCodes.Status415UnsupportedMediaType);
        if (storage.State == "NOT_CONFIGURED")
            return Results.Problem(title: "Mídia indisponível", detail: storage.Description, statusCode: StatusCodes.Status503ServiceUnavailable);

        MediaVariantDescriptor descriptor;
        try
        {
            descriptor = variants.Describe(asset, new MediaVariantRequest(width, height, format));
        }
        catch (ArgumentException exception)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["variant"] = [exception.Message] });
        }

        var bytes = await storage.ReadAsync(descriptor.CacheKey, ct);
        var cacheHit = bytes is not null;
        if (bytes is null)
        {
            var source = await storage.ReadAsync(asset.ObjectKey, ct);
            if (source is null)
                return Results.Problem(title: "Objeto de mídia ausente", detail: "O metadado existe, mas o arquivo original não foi localizado no storage.", statusCode: StatusCodes.Status409Conflict);

            try
            {
                var rendered = variants.Render(asset, source, descriptor);
                bytes = rendered.Bytes;
            }
            catch (MediaVariantFormatUnavailableException exception)
            {
                return Results.Problem(title: "Codec de variante indisponível", detail: exception.Message, statusCode: StatusCodes.Status501NotImplemented);
            }
            catch (InvalidDataException exception)
            {
                return Results.Problem(title: "Imagem original inválida para transformação", detail: exception.Message, statusCode: StatusCodes.Status422UnprocessableEntity);
            }
            catch (ArgumentException exception)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["variant"] = [exception.Message] });
            }

            await storage.SaveAsync(descriptor.CacheKey, bytes, ct);
        }

        var sha256 = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        var etag = $"\"sha256-{sha256}\"";
        context.Response.Headers.ETag = etag;
        context.Response.Headers.CacheControl = "public,max-age=31536000,immutable";
        context.Response.Headers["X-Media-Variant-Cache"] = cacheHit ? "HIT" : "MISS";
        if (context.Request.Headers.IfNoneMatch.Any(value => string.Equals(value, etag, StringComparison.Ordinal)))
            return Results.StatusCode(StatusCodes.Status304NotModified);

        return Results.File(bytes, descriptor.MimeType, enableRangeProcessing: false);
    }
}
