using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MunicipalPlatform.Api.Infrastructure.Persistence;
using MunicipalPlatform.Api.Modules.Identity;
using MunicipalPlatform.Api.Modules.Identity.Domain;
using MunicipalPlatform.Api.Modules.Portal;
using MunicipalPlatform.Api.Platform.Observability;
using MunicipalPlatform.Api.Platform.Security;
using MunicipalPlatform.Api.Platform.Tenancy;

var builder = WebApplication.CreateBuilder(args);
var databaseConnection = builder.Configuration.GetConnectionString("Database")
    ?? "Host=localhost;Port=5432;Database=municipal_platform;Username=municipal";

builder.Services.AddScoped<TenantContext>();
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(databaseConnection));
builder.Services.AddProblemDetails();
builder.Services.AddScoped<IPasswordHasher<UserAccount>, PasswordHasher<UserAccount>>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "municipal.session";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            || builder.Environment.IsEnvironment("Testing")
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.MapPlatformHealth();
if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<TenantResolutionMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapIdentityEndpoints();
app.MapPortalEndpoints();

app.Run();

public partial class Program;
