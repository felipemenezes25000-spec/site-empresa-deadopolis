using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MunicipalPlatform.Api.Infrastructure.Persistence;
using MunicipalPlatform.Api.Modules.Operations.Domain;
using MunicipalPlatform.Api.Platform.Tenancy;

namespace MunicipalPlatform.Api.Modules.Operations;

public static class OperationsEndpoints
{
    public static IEndpointRouteBuilder MapOperationsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var changes = endpoints.MapGroup("/api/v1/admin/changes")
            .WithTags("Admin", "Operations")
            .RequireAuthorization(p => p.RequireClaim("capability", "changes.manage"));
        changes.MapGet("/", ListChangesAsync);
        changes.MapPost("/", CreateChangeAsync);
        changes.MapPost("/{id:guid}/transition", TransitionChangeAsync);

        var ops = endpoints.MapGroup("/api/v1/admin/operations")
            .WithTags("Admin", "Operations")
            .RequireAuthorization(p => p.RequireClaim("capability", "operations.manage"));
        ops.MapGet("/links", ListLinksAsync);
        ops.MapPost("/links", CreateLinkAsync);
        ops.MapPost("/links/{id:guid}/check", CheckLinkAsync);
        ops.MapDelete("/links/{id:guid}", DeleteLinkAsync);
        ops.MapGet("/backups", ListBackupsAsync);
        ops.MapPost("/backups/evidence", AddBackupEvidenceAsync);
        ops.MapGet("/changelog", ListChangelogAsync);
        ops.MapPost("/changelog", CreateChangelogAsync);
        return endpoints;
    }

    private static async Task<IResult> ListChangesAsync(ApplicationDbContext db, CancellationToken ct) =>
        Results.Ok(await db.ChangeRequests.AsNoTracking().OrderByDescending(x => x.UpdatedAt).Take(200).ToListAsync(ct));

    private static async Task<IResult> CreateChangeAsync(
        ChangeCreateRequest request,
        ClaimsPrincipal principal,
        HttpContext context,
        ApplicationDbContext db,
        TenantContext tenant,
        CancellationToken ct)
    {
        var item = new ChangeRequest(
            tenant.RequireMunicipalityId(),
            RequireActor(principal),
            request.Title,
            request.BusinessReason,
            request.Impact,
            request.Priority);
        db.ChangeRequests.Add(item);
        Audit(db, tenant, principal, context, "change.requested", "ChangeRequest", item.Id, new { item.Title, item.Priority, item.State });
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/admin/changes/{item.Id}", item);
    }

    private static async Task<IResult> TransitionChangeAsync(
        Guid id,
        ChangeTransitionRequest request,
        ClaimsPrincipal principal,
        HttpContext context,
        ApplicationDbContext db,
        TenantContext tenant,
        CancellationToken ct)
    {
        var item = await db.ChangeRequests.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return Results.NotFound();
        if (!Enum.TryParse<ChangeRequestState>(request.State, true, out var state))
            return Results.ValidationProblem(new Dictionary<string, string[]> { { "state", ["Estado inválido."] } });
        item.Transition(state, request.Decision, request.PlannedRelease);
        Audit(db, tenant, principal, context, "change.transitioned", "ChangeRequest", item.Id, new { item.State, item.Decision, item.PlannedRelease });
        await db.SaveChangesAsync(ct);
        return Results.Ok(item);
    }

    private static async Task<IResult> ListLinksAsync(ApplicationDbContext db, CancellationToken ct) =>
        Results.Ok(await db.LinkChecks.AsNoTracking().OrderByDescending(x => x.CheckedAt).ThenBy(x => x.Url).Take(500).ToListAsync(ct));

    private static async Task<IResult> CreateLinkAsync(
        LinkCreateRequest request,
        ClaimsPrincipal principal,
        HttpContext context,
        ApplicationDbContext db,
        TenantContext tenant,
        CancellationToken ct)
    {
        if (!LinkCheckProbeService.TryNormalizeTarget(request.Url, out var uri, out var error))
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["url"] = [error] });

        var normalized = uri.AbsoluteUri;
        if (await db.LinkChecks.AnyAsync(item => item.Url == normalized, ct))
            return Results.Conflict(new { title = "URL já monitorada", status = 409 });

        var item = new LinkCheck(tenant.RequireMunicipalityId(), normalized);
        db.LinkChecks.Add(item);
        Audit(db, tenant, principal, context, "operations.link.created", "LinkCheck", item.Id, new { item.Url });
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/admin/operations/links/{item.Id}", item);
    }

    private static async Task<IResult> CheckLinkAsync(
        Guid id,
        ClaimsPrincipal principal,
        HttpContext context,
        ApplicationDbContext db,
        TenantContext tenant,
        LinkCheckProbeService probeService,
        CancellationToken ct)
    {
        var item = await db.LinkChecks.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return Results.NotFound();

        await probeService.CheckAsync(item, ct);
        Audit(
            db,
            tenant,
            principal,
            context,
            "operations.link.checked",
            "LinkCheck",
            item.Id,
            new { item.Url, item.State, item.StatusCode, item.LatencyMilliseconds, item.ConsecutiveFailures, item.FailureReason });
        await db.SaveChangesAsync(ct);
        return Results.Ok(item);
    }

    private static async Task<IResult> DeleteLinkAsync(
        Guid id,
        ClaimsPrincipal principal,
        HttpContext context,
        ApplicationDbContext db,
        TenantContext tenant,
        CancellationToken ct)
    {
        var item = await db.LinkChecks.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return Results.NotFound();

        Audit(db, tenant, principal, context, "operations.link.removed", "LinkCheck", item.Id, new { item.Url, item.State, item.CheckedAt });
        db.LinkChecks.Remove(item);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> ListBackupsAsync(ApplicationDbContext db, CancellationToken ct) =>
        Results.Ok(await db.BackupEvidences.AsNoTracking().OrderByDescending(x => x.StartedAt).Take(200).ToListAsync(ct));

    private static async Task<IResult> AddBackupEvidenceAsync(
        BackupEvidenceRequest request,
        ClaimsPrincipal principal,
        HttpContext context,
        ApplicationDbContext db,
        TenantContext tenant,
        CancellationToken ct)
    {
        var item = new BackupEvidence(tenant.RequireMunicipalityId(), request.Provider, request.BackupType, request.StartedAt);
        if (string.Equals(request.Status, "COMPLETED", StringComparison.OrdinalIgnoreCase)
            && request.CompletedAt.HasValue
            && !string.IsNullOrWhiteSpace(request.Reference))
        {
            item.Complete(request.Reference, request.SizeBytes, request.CompletedAt.Value);
        }
        else if (string.Equals(request.Status, "FAILED", StringComparison.OrdinalIgnoreCase) && request.CompletedAt.HasValue)
        {
            item.Fail(request.Error ?? "Falha não detalhada.", request.CompletedAt.Value);
        }
        if (request.RestoreTestedAt.HasValue) item.MarkRestoreTested(request.RestoreTestedAt.Value);
        db.BackupEvidences.Add(item);
        Audit(db, tenant, principal, context, "backup.evidence.recorded", "BackupEvidence", item.Id, new { item.Provider, item.BackupType, item.Status, item.RestoreTestedAt });
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/admin/operations/backups/{item.Id}", item);
    }

    private static async Task<IResult> ListChangelogAsync(ApplicationDbContext db, CancellationToken ct) =>
        Results.Ok(await db.ChangelogEntries.AsNoTracking().OrderByDescending(x => x.ReleaseDate).Take(200).ToListAsync(ct));

    private static async Task<IResult> CreateChangelogAsync(
        ChangelogRequest request,
        ClaimsPrincipal principal,
        HttpContext context,
        ApplicationDbContext db,
        TenantContext tenant,
        CancellationToken ct)
    {
        if (!DateOnly.TryParse(request.ReleaseDate, out var date))
            return Results.ValidationProblem(new Dictionary<string, string[]> { { "releaseDate", ["Data inválida."] } });
        var item = new ChangelogEntry(
            tenant.RequireMunicipalityId(),
            request.Title,
            request.Version,
            date,
            request.Summary,
            request.Details,
            request.Impact,
            request.Audience);
        db.ChangelogEntries.Add(item);
        Audit(db, tenant, principal, context, "changelog.created", "ChangelogEntry", item.Id, new { item.Title, item.Version, item.ReleaseDate });
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/admin/operations/changelog/{item.Id}", item);
    }

    private static void Audit(
        ApplicationDbContext db,
        TenantContext tenant,
        ClaimsPrincipal principal,
        HttpContext context,
        string action,
        string resource,
        Guid id,
        object diff) =>
        db.AuditEvents.Add(new AuditEvent(
            tenant.RequireMunicipalityId(),
            RequireActor(principal),
            action,
            resource,
            id.ToString(),
            JsonSerializer.Serialize(diff),
            context.TraceIdentifier));

    private static Guid RequireActor(ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? id
            : throw new InvalidOperationException("Sessão inválida.");

    public sealed record ChangeCreateRequest(string Title, string BusinessReason, string Impact, string Priority);
    public sealed record ChangeTransitionRequest(string State, string? Decision, string? PlannedRelease);
    public sealed record LinkCreateRequest(string Url);
    public sealed record BackupEvidenceRequest(
        string Provider,
        string BackupType,
        DateTimeOffset StartedAt,
        string Status,
        DateTimeOffset? CompletedAt,
        string? Reference,
        long? SizeBytes,
        DateTimeOffset? RestoreTestedAt,
        string? Error);
    public sealed record ChangelogRequest(
        string Title,
        string Version,
        string ReleaseDate,
        string Summary,
        string Details,
        string Impact,
        string Audience);
}
