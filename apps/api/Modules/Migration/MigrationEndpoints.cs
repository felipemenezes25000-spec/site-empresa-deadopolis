using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MunicipalPlatform.Api.Infrastructure.Persistence;
using MunicipalPlatform.Api.Modules.Migration.Domain;
using MunicipalPlatform.Api.Modules.Operations.Domain;
using MunicipalPlatform.Api.Platform.Tenancy;

namespace MunicipalPlatform.Api.Modules.Migration;

public static class MigrationEndpoints
{
    public static IEndpointRouteBuilder MapMigrationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/legacy/resolve", ResolveAsync).AllowAnonymous().WithTags("Migration");
        var group = endpoints.MapGroup("/api/v1/admin/redirects").WithTags("Admin", "Migration").RequireAuthorization(p => p.RequireClaim("capability", "migration.manage"));
        group.MapGet("/", ListAsync);
        group.MapPost("/", CreateAsync);
        group.MapPost("/{id:guid}/deactivate", DeactivateAsync);
        return endpoints;
    }

    private static async Task<IResult> ResolveAsync(string url, ApplicationDbContext db, CancellationToken ct)
    {
        string normalized;
        try { normalized = LegacyUrlNormalizer.Normalize(url); }
        catch (ArgumentException) { return Results.BadRequest(); }
        var rule = await db.RedirectRules.AsNoTracking().SingleOrDefaultAsync(x => x.LegacyPath == normalized && x.IsActive, ct);
        return rule is null || !RedirectRule.IsInternalDestination(rule.DestinationPath)
            ? Results.NotFound()
            : Results.Ok(new { source = normalized, destination = rule.DestinationPath, statusCode = rule.StatusCode });
    }

    private static async Task<IResult> ListAsync(ApplicationDbContext db, CancellationToken ct) =>
        Results.Ok((await db.RedirectRules.AsNoTracking().OrderBy(x => x.LegacyPath).ToListAsync(ct)).Select(ToResponse));

    private static object ToResponse(RedirectRule rule) => new { rule.Id, rule.LegacyPath, rule.DestinationPath, rule.StatusCode, rule.IsActive, rule.CreatedAt, rule.LastValidatedAt };

    private static async Task<IResult> CreateAsync(RedirectRequest request, ClaimsPrincipal principal, HttpContext context, ApplicationDbContext db, TenantContext tenant, CancellationToken ct)
    {
        RedirectRule rule;
        try { rule = new RedirectRule(tenant.RequireMunicipalityId(), request.LegacyUrl, request.DestinationPath, request.Permanent); }
        catch (Exception ex) when (ex is ArgumentException or UriFormatException) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["url"] = [ex.Message] }); }
        if (!rule.DestinationPath.StartsWith('/')) return Results.ValidationProblem(new Dictionary<string, string[]> { ["destinationPath"] = ["Destino interno deve começar com '/'."] });
        if (rule.LegacyPath == rule.DestinationPath) return Results.ValidationProblem(new Dictionary<string, string[]> { ["destinationPath"] = ["Redirect não pode apontar para si mesmo."] });
        if (await db.RedirectRules.AnyAsync(x => x.LegacyPath == rule.LegacyPath, ct)) return Results.Conflict(new { title = "URL legada já mapeada", status = 409 });
        if (await ClosesALoopAsync(rule, db, ct)) return Results.ValidationProblem(new Dictionary<string, string[]> { ["destinationPath"] = ["O destino volta para esta URL legada e criaria um laço de redirect no navegador."] });
        db.RedirectRules.Add(rule);
        db.AuditEvents.Add(new AuditEvent(tenant.RequireMunicipalityId(), RequireActor(principal), "redirect.created", "RedirectRule", rule.Id.ToString(), JsonSerializer.Serialize(new { rule.LegacyPath, rule.DestinationPath, rule.StatusCode }), context.TraceIdentifier));
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/admin/redirects/{rule.Id}", ToResponse(rule));
    }

    /// <summary>Percorre a cadeia de destinos para impedir que o navegador entre em um laço 301.</summary>
    private static async Task<bool> ClosesALoopAsync(RedirectRule rule, ApplicationDbContext db, CancellationToken ct)
    {
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { rule.LegacyPath };
        var current = rule.DestinationPath;
        for (var hop = 0; hop < 10; hop++)
        {
            if (!visited.Add(current)) return true;
            var next = await db.RedirectRules.AsNoTracking().SingleOrDefaultAsync(item => item.LegacyPath == current && item.IsActive, ct);
            if (next is null) return false;
            if (string.Equals(next.DestinationPath, rule.LegacyPath, StringComparison.OrdinalIgnoreCase)) return true;
            current = next.DestinationPath;
        }
        return true;
    }

    private static async Task<IResult> DeactivateAsync(Guid id, ClaimsPrincipal principal, HttpContext context, ApplicationDbContext db, TenantContext tenant, CancellationToken ct)
    {
        var rule = await db.RedirectRules.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (rule is null) return Results.NotFound();
        rule.Deactivate();
        db.AuditEvents.Add(new AuditEvent(tenant.RequireMunicipalityId(), RequireActor(principal), "redirect.deactivated", "RedirectRule", rule.Id.ToString(), JsonSerializer.Serialize(new { rule.LegacyPath }), context.TraceIdentifier));
        await db.SaveChangesAsync(ct);
        return Results.Ok(ToResponse(rule));
    }

    private static Guid RequireActor(ClaimsPrincipal p) => Guid.TryParse(p.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : throw new InvalidOperationException("Sessão inválida.");
    public sealed record RedirectRequest(string LegacyUrl, string DestinationPath, bool Permanent);
}

public sealed class LegacyRedirectMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ApplicationDbContext database)
    {
        if (context.Request.Path.StartsWithSegments("/health")
            || context.Request.Path.StartsWithSegments("/openapi")
            || context.Request.Path.StartsWithSegments("/swagger"))
        {
            await next(context);
            return;
        }

        if ((HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method))
            && !context.Request.Path.StartsWithSegments("/api"))
        {
            var raw = context.Request.Path + context.Request.QueryString;
            string normalized;
            try { normalized = LegacyUrlNormalizer.Normalize(raw); }
            catch (ArgumentException) { normalized = string.Empty; }
            if (normalized.Length > 0)
            {
                var rule = await database.RedirectRules.AsNoTracking().SingleOrDefaultAsync(x => x.LegacyPath == normalized && x.IsActive, context.RequestAborted);
                // Regras persistidas antes desta validacao nunca podem emitir um destino externo.
                if (rule is not null
                    && RedirectRule.IsInternalDestination(rule.DestinationPath)
                    && !string.Equals(rule.DestinationPath, normalized, StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.StatusCode = rule.StatusCode;
                    context.Response.Headers.Location = rule.DestinationPath;
                    return;
                }
            }
        }
        await next(context);
    }
}
