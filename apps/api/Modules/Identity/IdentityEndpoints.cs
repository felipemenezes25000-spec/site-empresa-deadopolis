using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using MunicipalPlatform.Api.Infrastructure.Persistence;
using MunicipalPlatform.Api.Modules.Identity.Domain;
using MunicipalPlatform.Api.Modules.Operations.Domain;
using MunicipalPlatform.Api.Platform.Tenancy;

namespace MunicipalPlatform.Api.Modules.Identity;

public static class IdentityEndpoints
{
    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/auth").WithTags("Identity");
        group.MapPost("/login", LoginAsync).AllowAnonymous().RequireRateLimiting("auth");
        group.MapPost("/logout", LogoutAsync).RequireAuthorization();
        group.MapGet("/me", GetCurrentUser).RequireAuthorization();
        group.MapPost("/mfa/enroll", BeginMfaEnrollmentAsync).RequireAuthorization();
        group.MapPost("/mfa/confirm", ConfirmMfaAsync).RequireAuthorization().RequireRateLimiting("auth");
        group.MapPost("/sessions/revoke", RevokeSessionsAsync).RequireAuthorization();

        var users = endpoints.MapGroup("/api/v1/admin/users")
            .WithTags("Admin", "Identity")
            .RequireAuthorization(policy => policy.RequireClaim("capability", "users.manage"));
        users.MapGet("/", ListUsersAsync);
        users.MapGet("/roles", ListRolesAsync);
        users.MapPost("/", CreateUserAsync);
        users.MapPut("/{id:guid}/role", AssignRoleAsync);
        users.MapPost("/{id:guid}/state", SetUserStateAsync);
        users.MapPost("/{id:guid}/sessions/revoke", RevokeUserSessionsAsync);
        return endpoints;
    }

    private static async Task<IResult> ListUsersAsync(ApplicationDbContext database, CancellationToken cancellationToken)
    {
        var users = await database.Users.AsNoTracking().OrderBy(user => user.DisplayName).Take(500).ToListAsync(cancellationToken);
        return Results.Ok(users.Select(ToAdminResponse));
    }

    private static async Task<IResult> ListRolesAsync(ApplicationDbContext database, CancellationToken cancellationToken)
    {
        var capabilities = await database.RoleCapabilities.AsNoTracking().OrderBy(item => item.Role).ThenBy(item => item.Capability).ToListAsync(cancellationToken);
        return Results.Ok(capabilities.GroupBy(item => item.Role).Select(group => new { role = group.Key, capabilities = group.Select(item => item.Capability).ToArray() }));
    }

    private static async Task<IResult> CreateUserAsync(
        AdminCreateUserRequest request,
        ClaimsPrincipal principal,
        HttpContext context,
        ApplicationDbContext database,
        TenantContext tenant,
        IPasswordHasher<UserAccount> passwordHasher,
        CancellationToken cancellationToken)
    {
        var username = request.Username?.Trim().ToLowerInvariant() ?? string.Empty;
        if (username.Length is < 3 or > 100 || username.Any(character => !char.IsAsciiLetterOrDigit(character) && character is not ('.' or '_' or '-')))
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["username"] = ["Use de 3 a 100 letras sem acento, números, ponto, hífen ou sublinhado."] });
        if (string.IsNullOrWhiteSpace(request.DisplayName) || request.DisplayName.Trim().Length > 160)
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["displayName"] = ["Informe um nome de exibição com até 160 caracteres."] });
        if (!IsStrongTemporaryPassword(request.TemporaryPassword))
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["temporaryPassword"] = ["A senha temporária deve ter de 14 a 128 caracteres, com maiúscula, minúscula, número e símbolo."] });
        var role = request.Role?.Trim().ToUpperInvariant() ?? string.Empty;
        if (!await database.RoleCapabilities.AnyAsync(item => item.Role == role, cancellationToken))
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["role"] = ["Selecione um papel RBAC cadastrado."] });
        if (await database.Users.AnyAsync(user => user.Username == username, cancellationToken))
            return Results.Conflict(new { title = "Nome de usuário já cadastrado", status = 409 });

        try
        {
            var passwordSubject = new UserAccount(tenant.RequireMunicipalityId(), username, request.DisplayName, role, "pending");
            var passwordHash = passwordHasher.HashPassword(passwordSubject, request.TemporaryPassword);
            var user = new UserAccount(tenant.RequireMunicipalityId(), username, request.DisplayName, role, passwordHash);
            database.Users.Add(user);
            database.AuditEvents.Add(AdminAudit(tenant, principal, context, "identity.user.created", user.Id, new { user.Username, user.Role, user.IsActive }));
            await database.SaveChangesAsync(cancellationToken);
            return Results.Created($"/api/v1/admin/users/{user.Id}", ToAdminResponse(user));
        }
        catch (ArgumentException exception)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["user"] = [exception.Message] });
        }
    }

    private static async Task<IResult> AssignRoleAsync(Guid id, AdminRoleRequest request, ClaimsPrincipal principal, HttpContext context, ApplicationDbContext database, TenantContext tenant, CancellationToken cancellationToken)
    {
        var actor = RequireActor(principal);
        if (id == actor) return Results.Conflict(new { title = "Não altere o próprio papel durante uma sessão ativa", status = 409 });
        var role = request.Role?.Trim().ToUpperInvariant() ?? string.Empty;
        if (!await database.RoleCapabilities.AnyAsync(item => item.Role == role, cancellationToken))
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["role"] = ["Selecione um papel RBAC cadastrado."] });
        var user = await database.Users.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (user is null) return Results.NotFound();
        user.AssignRole(role);
        database.AuditEvents.Add(AdminAudit(tenant, principal, context, "identity.user.role.assigned", user.Id, new { user.Username, user.Role, sessionsRevoked = true }));
        await database.SaveChangesAsync(cancellationToken);
        return Results.Ok(ToAdminResponse(user));
    }

    private static async Task<IResult> SetUserStateAsync(Guid id, AdminUserStateRequest request, ClaimsPrincipal principal, HttpContext context, ApplicationDbContext database, TenantContext tenant, CancellationToken cancellationToken)
    {
        var actor = RequireActor(principal);
        if (id == actor && !request.Active) return Results.Conflict(new { title = "A conta da sessão atual não pode ser desativada", status = 409 });
        var user = await database.Users.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (user is null) return Results.NotFound();
        user.SetActive(request.Active);
        database.AuditEvents.Add(AdminAudit(tenant, principal, context, "identity.user.state.changed", user.Id, new { user.Username, user.IsActive, sessionsRevoked = true }));
        await database.SaveChangesAsync(cancellationToken);
        return Results.Ok(ToAdminResponse(user));
    }

    private static async Task<IResult> RevokeUserSessionsAsync(Guid id, ClaimsPrincipal principal, HttpContext context, ApplicationDbContext database, TenantContext tenant, CancellationToken cancellationToken)
    {
        var user = await database.Users.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (user is null) return Results.NotFound();
        user.RevokeSessions();
        database.AuditEvents.Add(AdminAudit(tenant, principal, context, "identity.user.sessions.revoked", user.Id, new { user.Username, sessionsRevoked = true }));
        await database.SaveChangesAsync(cancellationToken);
        return Results.Ok(ToAdminResponse(user));
    }

    private static async Task<IResult> LoginAsync(LoginRequest request, HttpContext httpContext, ApplicationDbContext database, IPasswordHasher<UserAccount> passwordHasher, MfaTotpService mfa, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password)) return InvalidCredentials();
        var normalizedUsername = request.Username.Trim().ToLowerInvariant();
        var user = await database.Users.SingleOrDefaultAsync(candidate => candidate.Username == normalizedUsername && candidate.IsActive, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        if (user is null) return InvalidCredentials();
        if (user.IsLocked(now)) return InvalidCredentials();
        if (passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
        {
            user.RecordFailedLogin(now); await database.SaveChangesAsync(cancellationToken); return InvalidCredentials();
        }
        if (user.MfaEnabled && (string.IsNullOrWhiteSpace(user.MfaSecretProtected) || !mfa.VerifyProtectedSecret(user.MfaSecretProtected, request.TotpCode, now)))
        {
            user.RecordFailedLogin(now); await database.SaveChangesAsync(cancellationToken);
            return Results.Problem(title: "Verificação adicional necessária", detail: "Código de autenticação inválido ou ausente.", statusCode: StatusCodes.Status401Unauthorized, extensions: new Dictionary<string, object?> { ["mfaRequired"] = true });
        }

        var capabilities = await database.RoleCapabilities.AsNoTracking().Where(item => item.Role == user.Role).Select(item => item.Capability).ToListAsync(cancellationToken);
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, user.Id.ToString()), new(ClaimTypes.Name, user.DisplayName), new(ClaimTypes.Role, user.Role), new("municipality_id", user.MunicipalityId.ToString()), new("session_version", user.SessionVersion.ToString(System.Globalization.CultureInfo.InvariantCulture)) };
        claims.AddRange(capabilities.Select(capability => new Claim("capability", capability)));
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
        await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties { AllowRefresh = true, ExpiresUtc = now.AddHours(8), IsPersistent = false });
        user.RecordLogin(now); await database.SaveChangesAsync(cancellationToken);
        return Results.Ok(new { user.Id, user.DisplayName, user.Role, capabilities, mfaEnabled = user.MfaEnabled });
    }

    private static async Task<IResult> BeginMfaEnrollmentAsync(ClaimsPrincipal principal, HttpContext context, ApplicationDbContext database, MfaTotpService mfa, TenantContext tenant, CancellationToken cancellationToken)
    {
        var user = await FindCurrentUserAsync(principal, database, cancellationToken); if (user is null) return Results.Unauthorized();
        var enrollment = mfa.CreateEnrollment("Prefeitura de Deodápolis", user.Username); user.BeginMfaEnrollment(enrollment.ProtectedSecret);
        database.AuditEvents.Add(Audit(tenant, user.Id, "identity.mfa.enrollment.started", user.Id, context.TraceIdentifier)); await database.SaveChangesAsync(cancellationToken);
        return Results.Ok(new { enrollment.Secret, enrollment.OtpAuthUri, warning = "O segredo é exibido somente para cadastro no autenticador. Não o registre em logs ou chamados." });
    }

    private static async Task<IResult> ConfirmMfaAsync(MfaConfirmRequest request, ClaimsPrincipal principal, HttpContext context, ApplicationDbContext database, MfaTotpService mfa, TenantContext tenant, CancellationToken cancellationToken)
    {
        var user = await FindCurrentUserAsync(principal, database, cancellationToken); if (user is null) return Results.Unauthorized();
        if (string.IsNullOrWhiteSpace(user.MfaPendingSecretProtected)) return Results.Conflict(new { title = "Não existe cadastro MFA pendente", status = 409 });
        if (!mfa.VerifyProtectedSecret(user.MfaPendingSecretProtected, request.Code, DateTimeOffset.UtcNow)) return Results.ValidationProblem(new Dictionary<string, string[]> { ["code"] = ["Código inválido."] });
        user.ConfirmMfaEnrollment(); database.AuditEvents.Add(Audit(tenant, user.Id, "identity.mfa.enabled", user.Id, context.TraceIdentifier)); await database.SaveChangesAsync(cancellationToken);
        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Results.Ok(new { enabled = true, sessionsRevoked = true, message = "MFA ativado. Entre novamente usando senha e código do autenticador." });
    }

    private static async Task<IResult> RevokeSessionsAsync(ClaimsPrincipal principal, HttpContext context, ApplicationDbContext database, TenantContext tenant, CancellationToken cancellationToken)
    {
        var user = await FindCurrentUserAsync(principal, database, cancellationToken); if (user is null) return Results.Unauthorized();
        user.RevokeSessions(); database.AuditEvents.Add(Audit(tenant, user.Id, "identity.sessions.revoked", user.Id, context.TraceIdentifier)); await database.SaveChangesAsync(cancellationToken); await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme); return Results.Ok(new { revoked = true });
    }

    private static async Task<IResult> LogoutAsync(HttpContext httpContext, CancellationToken cancellationToken) { cancellationToken.ThrowIfCancellationRequested(); await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme); return Results.NoContent(); }
    private static IResult GetCurrentUser(ClaimsPrincipal principal) => Results.Ok(new { id = principal.FindFirstValue(ClaimTypes.NameIdentifier), displayName = principal.Identity?.Name, role = principal.FindFirstValue(ClaimTypes.Role), capabilities = principal.FindAll("capability").Select(claim => claim.Value).Order().ToArray() });
    private static async Task<UserAccount?> FindCurrentUserAsync(ClaimsPrincipal principal, ApplicationDbContext database, CancellationToken cancellationToken) => Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? await database.Users.SingleOrDefaultAsync(x => x.Id == id && x.IsActive, cancellationToken) : null;
    private static AuditEvent Audit(TenantContext tenant, Guid actor, string action, Guid resourceId, string correlationId) => new(tenant.RequireMunicipalityId(), actor, action, "UserAccount", resourceId.ToString(), JsonSerializer.Serialize(new { securityEvent = true }), correlationId);
    private static AuditEvent AdminAudit(TenantContext tenant, ClaimsPrincipal principal, HttpContext context, string action, Guid resourceId, object detail) => new(tenant.RequireMunicipalityId(), RequireActor(principal), action, "UserAccount", resourceId.ToString(), JsonSerializer.Serialize(detail), context.TraceIdentifier);
    private static Guid RequireActor(ClaimsPrincipal principal) => Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var actor) ? actor : throw new InvalidOperationException("Sessão sem identificador de usuário.");
    private static object ToAdminResponse(UserAccount user) => new { user.Id, user.Username, user.DisplayName, user.Role, user.IsActive, user.MfaEnabled, user.CreatedAt, user.LastLoginAt, user.LockedUntil, user.SessionVersion };
    private static bool IsStrongTemporaryPassword(string? password) => password is { Length: >= 14 and <= 128 }
        && password.Any(char.IsUpper)
        && password.Any(char.IsLower)
        && password.Any(char.IsDigit)
        && password.Any(character => !char.IsLetterOrDigit(character));
    private static IResult InvalidCredentials() => Results.Problem(title: "Não foi possível entrar", detail: "Credenciais inválidas.", statusCode: StatusCodes.Status401Unauthorized);
    public sealed record LoginRequest(string Username, string Password, string? TotpCode = null);
    public sealed record MfaConfirmRequest(string Code);
    public sealed record AdminCreateUserRequest(string Username, string DisplayName, string Role, string TemporaryPassword);
    public sealed record AdminRoleRequest(string Role);
    public sealed record AdminUserStateRequest(bool Active);
}
