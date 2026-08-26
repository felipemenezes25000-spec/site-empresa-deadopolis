using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using MunicipalPlatform.Api.Infrastructure.Persistence;
using MunicipalPlatform.Api.Modules.Administration;
using MunicipalPlatform.Api.Modules.Content;
using MunicipalPlatform.Api.Modules.Gazette;
using MunicipalPlatform.Api.Modules.Gazette.Providers;
using MunicipalPlatform.Api.Modules.Gazette.Services;
using MunicipalPlatform.Api.Modules.Identity;
using MunicipalPlatform.Api.Modules.Identity.Domain;
using MunicipalPlatform.Api.Modules.Mail;
using MunicipalPlatform.Api.Modules.Mail.Providers;
using MunicipalPlatform.Api.Modules.Media;
using MunicipalPlatform.Api.Modules.Media.Providers;
using MunicipalPlatform.Api.Modules.Migration;
using MunicipalPlatform.Api.Modules.Migration.Services;
using MunicipalPlatform.Api.Modules.Operations;
using MunicipalPlatform.Api.Modules.Portal;
using MunicipalPlatform.Api.Modules.Support;
using MunicipalPlatform.Api.Modules.Transparency;
using MunicipalPlatform.Api.Platform.Observability;
using MunicipalPlatform.Api.Platform.Security;
using MunicipalPlatform.Api.Platform.Storage;
using MunicipalPlatform.Api.Platform.Tenancy;

var builder = WebApplication.CreateBuilder(args);
var databaseConnection = builder.Configuration.GetConnectionString("Database") ?? "Host=localhost;Port=5432;Database=municipal_platform;Username=municipal";
var keyRingPath = builder.Configuration["DataProtection:KeyRingPath"] ?? Path.Combine(Path.GetTempPath(), "municipal-dp-keys");
Directory.CreateDirectory(keyRingPath);
builder.Services.AddDataProtection().SetApplicationName("MunicipalPlatform").PersistKeysToFileSystem(new DirectoryInfo(keyRingPath));
builder.Services.AddScoped<TenantContext>();
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(databaseConnection));
builder.Services.AddProblemDetails();
builder.Services.AddScoped<IPasswordHasher<UserAccount>, PasswordHasher<UserAccount>>();
builder.Services.AddSingleton<MfaTotpService>();
builder.Services.AddSingleton<GazetteDocumentService>();
builder.Services.AddSingleton<LegacyCrawlerService>();
builder.Services.AddSingleton<LinkCheckProbeService>();
builder.Services.AddHostedService<ScheduledPublicationWorker>();
builder.Services.AddHostedService<LinkCheckWorker>();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
});
builder.Services.AddSingleton<IObjectStorageProvider>(services =>
{
    var env = services.GetRequiredService<IHostEnvironment>();
    var config = services.GetRequiredService<IConfiguration>();
    return env.IsDevelopment() || env.IsEnvironment("Testing") || config.GetValue<bool>("PresentationMode")
        ? new LocalObjectStorageProvider(config)
        : new NotConfiguredObjectStorageProvider();
});
builder.Services.AddSingleton<IDigitalSigner>(services =>
{
    var env = services.GetRequiredService<IHostEnvironment>();
    var config = services.GetRequiredService<IConfiguration>();
    return env.IsEnvironment("Testing") || config.GetValue<bool>("PresentationMode")
        ? new DemoDigitalSigner()
        : new NotConfiguredDigitalSigner();
});
builder.Services.AddSingleton<ICertificateProvider>(services => (ICertificateProvider)services.GetRequiredService<IDigitalSigner>());
builder.Services.AddSingleton<ISignatureValidator>(services => (ISignatureValidator)services.GetRequiredService<IDigitalSigner>());
builder.Services.AddSingleton<ITimestampProvider, NotConfiguredTimestampProvider>();
builder.Services.AddSingleton<IInstitutionalEmailProvider>(services =>
{
    var env = services.GetRequiredService<IHostEnvironment>();
    var config = services.GetRequiredService<IConfiguration>();
    return env.IsEnvironment("Testing") || config.GetValue<bool>("PresentationMode")
        ? new DemoInstitutionalEmailProvider()
        : new NotConfiguredInstitutionalEmailProvider();
});
builder.Services.AddSingleton<IMalwareScanner>(services =>
{
    var env = services.GetRequiredService<IHostEnvironment>();
    var config = services.GetRequiredService<IConfiguration>();
    return env.IsEnvironment("Testing") || config.GetValue<bool>("PresentationMode")
        ? new DemoMalwareScanner()
        : new NotConfiguredMalwareScanner();
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
    options.Cookie.Name = "municipal.session";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Testing")
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
    options.SlidingExpiration = true;
    options.Events.OnRedirectToLogin = context => { context.Response.StatusCode = StatusCodes.Status401Unauthorized; return Task.CompletedTask; };
    options.Events.OnRedirectToAccessDenied = context => { context.Response.StatusCode = StatusCodes.Status403Forbidden; return Task.CompletedTask; };
    options.Events.OnValidatePrincipal = async context =>
    {
        var idValue = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        var versionValue = context.Principal?.FindFirstValue("session_version");
        if (!Guid.TryParse(idValue, out var id)
            || !int.TryParse(versionValue, NumberStyles.None, CultureInfo.InvariantCulture, out var version))
        {
            context.RejectPrincipal();
            return;
        }

        var database = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
        var valid = await database.Users.AsNoTracking().AnyAsync(
            user => user.Id == id && user.IsActive && user.SessionVersion == version,
            context.HttpContext.RequestAborted);
        if (!valid) context.RejectPrincipal();
    };
});
builder.Services.AddAuthorization();

var app = builder.Build();
if (!app.Environment.IsEnvironment("Testing")) await DatabaseInitializer.InitializeAsync(app.Services, app.Configuration, app.Environment);
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<RequestTelemetryMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseExceptionHandler();
app.MapPlatformHealth();
if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseMiddleware<LegacyRedirectMiddleware>();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapIdentityEndpoints();
app.MapPortalEndpoints();
app.MapContentEndpoints();
app.MapResourceEndpoints();
app.MapSupportEndpoints();
app.MapGazetteEndpoints();
app.MapGazetteCompositionEndpoints();
app.MapAdministrationEndpoints();
app.MapMailEndpoints();
app.MapMailGovernanceEndpoints();
app.MapMediaEndpoints();
app.MapMigrationEndpoints();
app.MapMigrationJobEndpoints();
app.MapMigrationCrawlerEndpoints();
app.MapTransparencyEndpoints();
app.MapTransparencyAdminReadEndpoints();
app.MapOperationsEndpoints();
app.Run();

public partial class Program;
