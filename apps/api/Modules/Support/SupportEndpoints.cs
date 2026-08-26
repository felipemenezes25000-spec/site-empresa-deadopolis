using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MunicipalPlatform.Api.Infrastructure.Persistence;
using MunicipalPlatform.Api.Modules.Operations.Domain;
using MunicipalPlatform.Api.Modules.Support.Domain;
using MunicipalPlatform.Api.Platform.Tenancy;

namespace MunicipalPlatform.Api.Modules.Support;

public static class SupportEndpoints
{
    public static IEndpointRouteBuilder MapSupportEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/tickets", CreateAsync).AllowAnonymous().WithTags("Support");
        endpoints.MapGet("/api/v1/tickets/{protocol}", TrackAsync).AllowAnonymous().WithTags("Support");
        endpoints.MapGet("/api/v1/admin/tickets", ListAsync)
            .RequireAuthorization(policy => policy.RequireClaim("capability", "support.write"))
            .WithTags("Admin", "Support");
        return endpoints;
    }

    private static async Task<IResult> CreateAsync(
        TicketRequest request,
        HttpContext context,
        ApplicationDbContext database,
        TenantContext tenant,
        CancellationToken cancellationToken)
    {
        var errors = Validate(request);
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var policy = await database.SlaPolicies.SingleOrDefaultAsync(cancellationToken);
        if (policy is null)
        {
            policy = SlaPolicy.CreateDefault(tenant.RequireMunicipalityId());
            database.SlaPolicies.Add(policy);
        }

        var openedAt = DateTimeOffset.UtcNow;
        var protocol = $"DEO-{openedAt:yyyyMMdd}-{Convert.ToHexString(RandomNumberGenerator.GetBytes(4))}";
        var ticket = new Ticket(
            tenant.RequireMunicipalityId(),
            protocol,
            request.RequesterName,
            request.Contact,
            request.Category,
            TicketPriority.Normal,
            request.Description,
            policy.CalculateDeadlines(TicketPriority.Normal, openedAt));
        database.Tickets.Add(ticket);
        database.AuditEvents.Add(new AuditEvent(
            tenant.RequireMunicipalityId(),
            null,
            "support.ticket.created",
            "Ticket",
            ticket.Id.ToString(),
            JsonSerializer.Serialize(new { ticket.Protocol, ticket.Category, privacyConsent = true }),
            context.TraceIdentifier));
        await database.SaveChangesAsync(cancellationToken);

        return Results.Created($"/api/v1/tickets/{ticket.Protocol}", new
        {
            ticket.Protocol,
            ticket.TrackingCode,
            ticket.Status,
            ticket.FirstResponseDueAt,
            ticket.ResolutionDueAt
        });
    }

    private static async Task<IResult> TrackAsync(
        string protocol,
        string? code,
        ApplicationDbContext database,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return Results.BadRequest(new { title = "Código de acompanhamento obrigatório", status = StatusCodes.Status400BadRequest });
        }

        var ticket = await database.Tickets.AsNoTracking()
            .Where(item => item.Protocol == protocol && item.TrackingCode == code)
            .Select(item => new { item.Protocol, item.Category, item.Status, item.OpenedAt, item.FirstResponseDueAt, item.ResolutionDueAt, item.FirstResponseAt, item.ResolvedAt })
            .SingleOrDefaultAsync(cancellationToken);
        return ticket is null ? Results.NotFound() : Results.Ok(ticket);
    }

    private static async Task<IResult> ListAsync(ApplicationDbContext database, CancellationToken cancellationToken)
    {
        var tickets = await database.Tickets.AsNoTracking()
            .OrderBy(item => item.ResolutionDueAt)
            .Select(item => new
            {
                item.Id,
                item.Protocol,
                item.RequesterName,
                item.Category,
                item.Priority,
                item.Status,
                item.OpenedAt,
                item.FirstResponseDueAt,
                item.ResolutionDueAt
            })
            .ToListAsync(cancellationToken);
        return Results.Ok(tickets);
    }

    private static Dictionary<string, string[]> Validate(TicketRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        if (string.IsNullOrWhiteSpace(request.RequesterName) || request.RequesterName.Trim().Length > 160) errors["requesterName"] = ["Informe um nome com até 160 caracteres."];
        if (string.IsNullOrWhiteSpace(request.Contact) || request.Contact.Trim().Length > 200) errors["contact"] = ["Informe um meio de contato válido."];
        if (string.IsNullOrWhiteSpace(request.Category) || request.Category.Trim().Length > 80) errors["category"] = ["Informe a categoria."];
        if (string.IsNullOrWhiteSpace(request.Description) || request.Description.Trim().Length is < 20 or > 4_000) errors["description"] = ["Descreva a solicitação entre 20 e 4.000 caracteres."];
        if (!request.PrivacyConsent) errors["privacyConsent"] = ["É necessário concordar com o tratamento dos dados para atendimento."];
        return errors;
    }

    public sealed record TicketRequest(
        string RequesterName,
        string Contact,
        string Category,
        string Description,
        bool PrivacyConsent);
}
