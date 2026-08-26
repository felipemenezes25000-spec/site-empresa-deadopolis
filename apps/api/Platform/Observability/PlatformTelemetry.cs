using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace MunicipalPlatform.Api.Platform.Observability;

public static class PlatformTelemetry
{
    public const string SourceName = "MunicipalPlatform.Api";
    public static readonly ActivitySource ActivitySource = new(SourceName);
    public static readonly Meter Meter = new(SourceName, "1.0.0");
    public static readonly Counter<long> HttpRequests = Meter.CreateCounter<long>("municipal.http.requests");
    public static readonly Counter<long> HttpFailures = Meter.CreateCounter<long>("municipal.http.failures");
    public static readonly Histogram<double> HttpDurationMs = Meter.CreateHistogram<double>("municipal.http.duration", "ms");
}
