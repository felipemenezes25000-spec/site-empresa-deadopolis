using System.Diagnostics;

namespace MunicipalPlatform.Api.Platform.Observability;

public sealed partial class RequestTelemetryMiddleware(RequestDelegate next, ILogger<RequestTelemetryMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var started = Stopwatch.GetTimestamp();
        using var activity = PlatformTelemetry.ActivitySource.StartActivity($"{context.Request.Method} {context.Request.Path}", ActivityKind.Server);
        activity?.SetTag("http.request.method", context.Request.Method);
        activity?.SetTag("url.path", context.Request.Path.Value);
        activity?.SetTag("municipal.correlation_id", context.TraceIdentifier);

        try
        {
            await next(context);
        }
        finally
        {
            var elapsedMs = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            var status = context.Response.StatusCode;
            var tags = new TagList
            {
                { "http.request.method", context.Request.Method },
                { "http.response.status_code", status }
            };
            PlatformTelemetry.HttpRequests.Add(1, tags);
            PlatformTelemetry.HttpDurationMs.Record(elapsedMs, tags);
            if (status >= 500) PlatformTelemetry.HttpFailures.Add(1, tags);
            activity?.SetTag("http.response.status_code", status);
            LogRequestCompleted(logger, context.Request.Method, context.Request.Path.Value ?? "/", status, elapsedMs, context.TraceIdentifier);
        }
    }

    [LoggerMessage(EventId = 1001, Level = LogLevel.Information, Message = "HTTP {Method} {Path} -> {StatusCode} in {ElapsedMs:F1} ms correlation={CorrelationId}")]
    private static partial void LogRequestCompleted(ILogger logger, string method, string path, int statusCode, double elapsedMs, string correlationId);
}
