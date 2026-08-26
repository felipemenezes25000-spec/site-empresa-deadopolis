using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MunicipalPlatform.Api.Infrastructure.Persistence;
using MunicipalPlatform.Api.Modules.Identity.Domain;

namespace MunicipalPlatform.Api.Modules.Identity;

public static class IdentityEndpoints
{
    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/auth").WithTags("Identity");
        group.MapPost("/login", LoginAsync).AllowAnonymous();
        group.MapPost("/logout", LogoutAsync).RequireAuthorization();
        group.MapGet("/me", GetCurrentUser).RequireAuthorization();
        return endpoints;
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        HttpContext httpContext,
        ApplicationDbContext database,
        IPasswordHasher<UserAccount> passwordHasher,
        CancellationToken cancellationToken)
    {
        var normalizedUsername = request.Username.Trim().ToLowerInvariant();
        var user = await database.Users
            .SingleOrDefaultAsync(
                candidate => candidate.Username == normalizedUsername && candidate.IsActive,
                cancellationToken);
        if (user is null
            || passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password)
                == PasswordVerificationResult.Failed)
        {
            return Results.Problem(
                title: "Não foi possível entrar",
                detail: "Credenciais inválidas.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var capabilities = await database.RoleCapabilities
            .AsNoTracking()
            .Where(item => item.Role == user.Role)
            .Select(item => item.Capability)
            .ToListAsync(cancellationToken);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.DisplayName),
            new(ClaimTypes.Role, user.Role),
            new("municipality_id", user.MunicipalityId.ToString())
        };
        claims.AddRange(capabilities.Select(capability => new Claim("capability", capability)));
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme));

        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                AllowRefresh = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8),
                IsPersistent = false
            });
        user.RecordLogin(DateTimeOffset.UtcNow);
        await database.SaveChangesAsync(cancellationToken);

        return Results.Ok(new
        {
            user.Id,
            user.DisplayName,
            user.Role,
            capabilities
        });
    }

    private static async Task<IResult> LogoutAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Results.NoContent();
    }

    private static IResult GetCurrentUser(ClaimsPrincipal principal) => Results.Ok(new
    {
        id = principal.FindFirstValue(ClaimTypes.NameIdentifier),
        displayName = principal.Identity?.Name,
        role = principal.FindFirstValue(ClaimTypes.Role),
        capabilities = principal.FindAll("capability").Select(claim => claim.Value).Order().ToArray()
    });

    public sealed record LoginRequest(string Username, string Password);
}
