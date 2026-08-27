using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace MunicipalPlatform.Api.Platform.Observability;

public static class ObservabilityServiceCollectionExtensions
{
    public static IServiceCollection AddPlatformObservability(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var serviceName = configuration["Observability:ServiceName"] ?? PlatformTelemetry.SourceName;
        var otlpEnabled = configuration.GetValue<bool>("Observability:OtlpEnabled")
            || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT"));

        var telemetry = services
            .AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(
                serviceName: serviceName,
                serviceNamespace: "municipal-platform"));

        telemetry.WithTracing(tracing =>
        {
            tracing
                .AddSource(PlatformTelemetry.SourceName)
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation();

            if (otlpEnabled)
            {
                tracing.AddOtlpExporter();
            }
        });

        telemetry.WithMetrics(metrics =>
        {
            metrics
                .AddMeter(PlatformTelemetry.SourceName)
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation();

            if (otlpEnabled)
            {
                metrics.AddOtlpExporter();
            }
        });

        return services;
    }
}
