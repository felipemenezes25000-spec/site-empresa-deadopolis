using Microsoft.EntityFrameworkCore;
using MunicipalPlatform.Api.Infrastructure.Persistence;

namespace MunicipalPlatform.Api.Platform.Tenancy;

public sealed class TenantResolutionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(
        HttpContext context,
        TenantContext tenantContext,
        ApplicationDbContext database,
        IConfiguration configuration)
    {
        if (context.Request.Path.StartsWithSegments("/health")
            || context.Request.Path.StartsWithSegments("/openapi"))
        {
            await next(context);
            return;
        }

        var slug = context.Request.Headers["X-Municipality"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(slug))
        {
            slug = ResolveFromHost(context.Request.Host.Host)
                ?? configuration["DefaultMunicipalitySlug"];
        }

        if (string.IsNullOrWhiteSpace(slug))
        {
            await WriteProblemAsync(context, "Município não identificado", StatusCodes.Status400BadRequest);
            return;
        }

        var normalizedSlug = slug.Trim().ToLowerInvariant();
        var municipality = await database.Municipalities
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Slug == normalizedSlug, context.RequestAborted);
        if (municipality is null || !municipality.IsActive)
        {
            await WriteProblemAsync(context, "Município não encontrado", StatusCodes.Status404NotFound);
            return;
        }

        tenantContext.SetMunicipality(municipality.Id, municipality.Slug);
        await next(context);
    }

    private static string? ResolveFromHost(string host)
    {
        if (host is "localhost" or "127.0.0.1")
        {
            return null;
        }

        var firstLabel = host.Split('.', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return firstLabel is "www" ? null : firstLabel;
    }

    private static Task WriteProblemAsync(HttpContext context, string title, int status)
    {
        context.Response.StatusCode = status;
        return context.Response.WriteAsJsonAsync(new
        {
            type = "https://httpstatuses.com/" + status,
            title,
            status,
            correlationId = context.TraceIdentifier
        });
    }
}
