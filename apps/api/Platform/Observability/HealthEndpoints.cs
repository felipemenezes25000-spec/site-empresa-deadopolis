namespace MunicipalPlatform.Api.Platform.Observability;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapPlatformHealth(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/health/live", (HttpContext context) => Results.Ok(new
        {
            status = "healthy",
            correlationId = context.TraceIdentifier
        }))
        .AllowAnonymous()
        .WithName("LiveHealth")
        .WithTags("Operations");

        return endpoints;
    }
}
