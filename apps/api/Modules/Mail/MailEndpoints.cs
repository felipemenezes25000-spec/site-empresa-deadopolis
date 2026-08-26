using System.Net.Mail;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MunicipalPlatform.Api.Infrastructure.Persistence;
using MunicipalPlatform.Api.Modules.Mail.Domain;
using MunicipalPlatform.Api.Modules.Mail.Providers;
using MunicipalPlatform.Api.Modules.Operations.Domain;
using MunicipalPlatform.Api.Platform.Tenancy;

namespace MunicipalPlatform.Api.Modules.Mail;

public static class MailEndpoints
{
    public static IEndpointRouteBuilder MapMailEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/admin/mailboxes").WithTags("Admin", "Mail").RequireAuthorization(p => p.RequireClaim("capability", "mail.manage"));
        group.MapGet("/", ListAsync); group.MapPost("/", CreateAsync); group.MapPut("/{id:guid}", UpdateAsync); return endpoints;
    }
    private static async Task<IResult> ListAsync(ApplicationDbContext db, IInstitutionalEmailProvider provider, CancellationToken ct) => Results.Ok(new { provider = new { provider.State, provider.Description }, mailboxes = await db.Mailboxes.AsNoTracking().OrderBy(x => x.Address).ToListAsync(ct) });
    private static async Task<IResult> CreateAsync(MailboxRequest request, ClaimsPrincipal principal, HttpContext context, ApplicationDbContext db, TenantContext tenant, IInstitutionalEmailProvider provider, CancellationToken ct)
    {
        try { _ = new MailAddress(request.Address); } catch (FormatException) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["address"] = ["Endereço de e-mail inválido."] }); }
        if (await db.Mailboxes.AnyAsync(x => x.Address == request.Address.Trim().ToLower(), ct)) return Results.Conflict(new { title = "Caixa já cadastrada", status = 409 });
        var actor = RequireActor(principal); var mailbox = new Mailbox(tenant.RequireMunicipalityId(), request.Address, request.DisplayName, request.QuotaMegabytes); var result = await provider.ProvisionAsync(new MailboxProvisioningRequest(mailbox.Address, mailbox.DisplayName, mailbox.QuotaMegabytes), ct); mailbox.ApplyProviderResult(result.Status, result.ExternalId); db.Mailboxes.Add(mailbox); db.AuditEvents.Add(new AuditEvent(tenant.RequireMunicipalityId(), actor, "mailbox.requested", "Mailbox", mailbox.Id.ToString(), JsonSerializer.Serialize(new { mailbox.Address, provider.State, result.Status }), context.TraceIdentifier)); await db.SaveChangesAsync(ct); return Results.Created($"/api/v1/admin/mailboxes/{mailbox.Id}", new { mailbox.Id, mailbox.Address, mailbox.DisplayName, mailbox.QuotaMegabytes, mailbox.Status, mailbox.ExternalId, provider = provider.State, result.Detail });
    }
    private static async Task<IResult> UpdateAsync(Guid id, MailboxUpdateRequest request, ClaimsPrincipal principal, HttpContext context, ApplicationDbContext db, TenantContext tenant, CancellationToken ct) { var mailbox = await db.Mailboxes.SingleOrDefaultAsync(x => x.Id == id, ct); if (mailbox is null) return Results.NotFound(); try { mailbox.Update(request.DisplayName, request.QuotaMegabytes); } catch (ArgumentOutOfRangeException ex) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["quotaMegabytes"] = [ex.Message] }); } db.AuditEvents.Add(new AuditEvent(tenant.RequireMunicipalityId(), RequireActor(principal), "mailbox.updated", "Mailbox", mailbox.Id.ToString(), JsonSerializer.Serialize(new { mailbox.DisplayName, mailbox.QuotaMegabytes }), context.TraceIdentifier)); await db.SaveChangesAsync(ct); return Results.Ok(mailbox); }
    private static Guid RequireActor(ClaimsPrincipal p) => Guid.TryParse(p.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : throw new InvalidOperationException("Sessão inválida.");
    public sealed record MailboxRequest(string Address, string DisplayName, int QuotaMegabytes);
    public sealed record MailboxUpdateRequest(string DisplayName, int QuotaMegabytes);
}
