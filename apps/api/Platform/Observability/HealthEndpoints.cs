using Microsoft.EntityFrameworkCore;
using MunicipalPlatform.Api.Infrastructure.Persistence;
using MunicipalPlatform.Api.Modules.Gazette.Providers;
using MunicipalPlatform.Api.Modules.Mail.Providers;
using MunicipalPlatform.Api.Modules.Media.Providers;
using MunicipalPlatform.Api.Modules.Media.Services;
using MunicipalPlatform.Api.Platform.Storage;

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

        endpoints.MapGet("/health/ready", async (
            ApplicationDbContext database,
            IObjectStorageProvider storage,
            IDigitalSigner signer,
            ITimestampProvider timestamp,
            IInstitutionalEmailProvider email,
            IMalwareScanner malware,
            MediaVariantService mediaVariants,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            var databaseReady = false;
            try { databaseReady = await database.Database.CanConnectAsync(cancellationToken); }
            catch { databaseReady = false; }

            var mediaCapabilities = mediaVariants.Capabilities;
            var mediaReady = mediaCapabilities.Webp.State == "AVAILABLE";
            var ready = databaseReady && mediaReady;
            var payload = new
            {
                status = ready ? "ready" : "not_ready",
                checks = new
                {
                    database = databaseReady ? "CONFIGURED" : "UNAVAILABLE",
                    storage = storage.State,
                    digitalSignature = signer.State,
                    timestamp = timestamp.State,
                    institutionalEmail = email.State,
                    malwareScanner = malware.State,
                    mediaVariants = new
                    {
                        webp = mediaCapabilities.Webp,
                        avif = mediaCapabilities.Avif
                    }
                },
                correlationId = context.TraceIdentifier
            };

            return ready
                ? Results.Ok(payload)
                : Results.Json(payload, statusCode: StatusCodes.Status503ServiceUnavailable);
        })
        .AllowAnonymous()
        .WithName("ReadyHealth")
        .WithTags("Operations");

        return endpoints;
    }
}
