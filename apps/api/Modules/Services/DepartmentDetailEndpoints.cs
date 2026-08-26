using Microsoft.EntityFrameworkCore;
using MunicipalPlatform.Api.Infrastructure.Persistence;

namespace MunicipalPlatform.Api.Modules.Services;

public static class DepartmentDetailEndpoints
{
    public static IEndpointRouteBuilder MapDepartmentDetailEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/departments/{slug}", GetDepartmentAsync)
            .AllowAnonymous()
            .WithName("GetDepartment")
            .WithTags("Departments");
        return endpoints;
    }

    private static async Task<IResult> GetDepartmentAsync(
        string slug,
        ApplicationDbContext database,
        CancellationToken cancellationToken)
    {
        var normalizedSlug = slug.Trim().ToLowerInvariant();
        if (normalizedSlug.Length == 0)
            return Results.NotFound();

        var item = await database.Departments
            .AsNoTracking()
            .Where(department => department.IsActive && department.Slug == normalizedSlug)
            .Select(department => new
            {
                department.Name,
                department.Slug,
                department.Acronym,
                department.ManagerName,
                department.Phone,
                department.Email,
                department.Address,
                department.OpeningHours
            })
            .SingleOrDefaultAsync(cancellationToken);

        return item is null ? Results.NotFound() : Results.Ok(item);
    }
}
