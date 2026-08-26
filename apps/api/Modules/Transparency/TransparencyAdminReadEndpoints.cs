using Microsoft.EntityFrameworkCore;
using MunicipalPlatform.Api.Infrastructure.Persistence;

namespace MunicipalPlatform.Api.Modules.Transparency;

public static class TransparencyAdminReadEndpoints
{
    public static IEndpointRouteBuilder MapTransparencyAdminReadEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var admin = endpoints.MapGroup("/api/v1/admin/datasets")
            .WithTags("Admin", "OpenData")
            .RequireAuthorization(policy => policy.RequireClaim("capability", "datasets.manage"));

        admin.MapGet("/{id:guid}/versions", ListVersionsAsync);
        return endpoints;
    }

    private static async Task<IResult> ListVersionsAsync(
        Guid id,
        ApplicationDbContext db,
        CancellationToken cancellationToken)
    {
        var exists = await db.Datasets.AsNoTracking().AnyAsync(dataset => dataset.Id == id, cancellationToken);
        if (!exists) return Results.NotFound();

        var versions = await db.DatasetVersions.AsNoTracking()
            .Where(version => version.DatasetId == id)
            .OrderByDescending(version => version.Version)
            .Select(version => new
            {
                version.Id,
                version.DatasetId,
                version.Version,
                version.FileName,
                version.MimeType,
                version.SizeBytes,
                version.Sha256,
                version.Format,
                version.MetadataJson,
                version.PublishedAt
            })
            .ToListAsync(cancellationToken);

        return Results.Ok(versions);
    }
}
