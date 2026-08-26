namespace MunicipalPlatform.Api.Platform.Observability;

public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var provided)
            && !string.IsNullOrWhiteSpace(provided.ToString())
                ? provided.ToString().Trim()
                : Guid.NewGuid().ToString("N");

        context.TraceIdentifier = correlationId;
        context.Response.Headers[HeaderName] = correlationId;
        await next(context);
    }
}
