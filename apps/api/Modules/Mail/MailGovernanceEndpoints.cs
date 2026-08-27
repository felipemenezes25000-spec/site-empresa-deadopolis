using System.Net.Mail;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MunicipalPlatform.Api.Infrastructure.Persistence;
using MunicipalPlatform.Api.Modules.Mail.Domain;
using MunicipalPlatform.Api.Modules.Mail.Providers;
using MunicipalPlatform.Api.Modules.Mail.Services;
using MunicipalPlatform.Api.Modules.Operations.Domain;
using MunicipalPlatform.Api.Platform.Tenancy;

namespace MunicipalPlatform.Api.Modules.Mail;

public static class MailGovernanceEndpoints
{
    public static IEndpointRouteBuilder MapMailGovernanceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/admin/mail")
            .WithTags("Admin", "Mail")
            .RequireAuthorization(p => p.RequireClaim("capability", "mail.manage"));

        group.MapGet("/domains", ListDomainsAsync);
        group.MapPost("/domains", CreateDomainAsync);
        group.MapGet("/aliases", ListAliasesAsync);
        group.MapPost("/aliases", CreateAliasAsync);
        group.MapPost("/aliases/{id:guid}/deactivate", DeactivateAliasAsync);
        group.MapGet("/migration-jobs", ListMigrationJobsAsync);
        group.MapPost("/migration-jobs", CreateMigrationJobAsync);
        group.MapPost("/migration-jobs/{id:guid}/inspect", InspectMigrationArchiveAsync).DisableAntiforgery();
        return endpoints;
    }

    private static async Task<IResult> ListDomainsAsync(
        ApplicationDbContext db,
        IInstitutionalEmailProvider provider,
        CancellationToken ct) =>
        Results.Ok(new
        {
            provider = new { provider.State, provider.Description },
            domains = await db.MailDomains.AsNoTracking().OrderBy(x => x.Domain).ToListAsync(ct)
        });

    private static async Task<IResult> CreateDomainAsync(
        DomainRequest request,
        ClaimsPrincipal principal,
        HttpContext context,
        ApplicationDbContext db,
        TenantContext tenant,
        IInstitutionalEmailProvider provider,
        CancellationToken ct)
    {
        var domain = NormalizeDomain(request.Domain);
        if (domain is null)
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["domain"] = ["Domínio inválido."] });

        if (await db.MailDomains.AnyAsync(x => x.Domain == domain, ct))
            return Results.Conflict(new { title = "Domínio já cadastrado." });

        var item = new MailDomain(tenant.RequireMunicipalityId(), domain);
        item.ApplyProviderState(provider.State, null);
        db.MailDomains.Add(item);
        AddAudit(
            db,
            tenant,
            principal,
            context,
            "mail.domain.created",
            "MailDomain",
            item.Id,
            new { item.Domain, domainState = item.State, providerState = provider.State });
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/admin/mail/domains/{item.Id}", item);
    }

    private static async Task<IResult> ListAliasesAsync(ApplicationDbContext db, CancellationToken ct) =>
        Results.Ok(await db.MailAliases.AsNoTracking().OrderBy(x => x.Address).ToListAsync(ct));

    private static async Task<IResult> CreateAliasAsync(
        AliasRequest request,
        ClaimsPrincipal principal,
        HttpContext context,
        ApplicationDbContext db,
        TenantContext tenant,
        CancellationToken ct)
    {
        var address = NormalizeAddress(request.Address);
        var target = NormalizeAddress(request.TargetAddress);
        if (address is null || target is null)
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["address"] = ["Alias ou destino inválido."] });

        if (await db.MailAliases.AnyAsync(x => x.Address == address, ct))
            return Results.Conflict(new { title = "Alias já cadastrado." });

        var item = new MailAlias(tenant.RequireMunicipalityId(), address, target);
        db.MailAliases.Add(item);
        AddAudit(db, tenant, principal, context, "mail.alias.created", "MailAlias", item.Id, new { item.Address, item.TargetAddress });
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/admin/mail/aliases/{item.Id}", item);
    }

    private static async Task<IResult> DeactivateAliasAsync(
        Guid id,
        ClaimsPrincipal principal,
        HttpContext context,
        ApplicationDbContext db,
        TenantContext tenant,
        CancellationToken ct)
    {
        var item = await db.MailAliases.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return Results.NotFound();

        item.Deactivate();
        AddAudit(db, tenant, principal, context, "mail.alias.deactivated", "MailAlias", item.Id, new { item.Address, item.IsActive });
        await db.SaveChangesAsync(ct);
        return Results.Ok(item);
    }

    private static async Task<IResult> ListMigrationJobsAsync(ApplicationDbContext db, CancellationToken ct) =>
        Results.Ok(await db.MailMigrationJobs.AsNoTracking().OrderByDescending(x => x.CreatedAt).Take(100).ToListAsync(ct));

    private static async Task<IResult> CreateMigrationJobAsync(
        MailMigrationRequest request,
        ClaimsPrincipal principal,
        HttpContext context,
        ApplicationDbContext db,
        TenantContext tenant,
        CancellationToken ct)
    {
        var source = request.SourceType.Trim().ToUpperInvariant();
        if (source is not ("IMAP" or "MBOX" or "EML"))
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["sourceType"] = ["Use IMAP, MBOX ou EML."] });

        var target = NormalizeAddress(request.TargetAddress);
        if (target is null)
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["targetAddress"] = ["Destino inválido."] });

        MailMigrationJob job;
        try
        {
            job = new MailMigrationJob(tenant.RequireMunicipalityId(), source, request.SourceReference, target);
        }
        catch (ArgumentException exception)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["migration"] = [exception.Message] });
        }

        db.MailMigrationJobs.Add(job);
        AddAudit(
            db,
            tenant,
            principal,
            context,
            "mail.migration.requested",
            "MailMigrationJob",
            job.Id,
            new { job.SourceType, job.TargetAddress, state = job.State, externalDependency = true });
        await db.SaveChangesAsync(ct);
        return Results.Accepted(
            $"/api/v1/admin/mail/migration-jobs/{job.Id}",
            new
            {
                job,
                externalDependency = "Credenciais/conector do provider devem ser configurados fora do repositório; nenhum segredo é persistido neste job."
            });
    }

    private static async Task<IResult> InspectMigrationArchiveAsync(
        Guid id,
        IFormFile file,
        ClaimsPrincipal principal,
        HttpContext context,
        ApplicationDbContext db,
        TenantContext tenant,
        MailArchiveInspectionService inspector,
        IInstitutionalEmailProvider provider,
        CancellationToken ct)
    {
        var job = await db.MailMigrationJobs.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (job is null) return Results.NotFound();
        if (job.SourceType == "IMAP")
            return Results.Problem(
                title: "Inspeção local indisponível para IMAP",
                detail: "IMAP exige conector e credenciais externas; use EML ou MBOX para inspeção local sem segredos.",
                statusCode: StatusCodes.Status409Conflict);
        if (file.Length <= 0 || file.Length > MailArchiveInspectionService.MaxArchiveBytes)
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["file"] = ["Arquivo deve possuir até 25 MB."] });
        if (!IsExpectedArchiveName(job.SourceType, file.FileName))
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["file"] = [$"O arquivo selecionado não corresponde ao tipo {job.SourceType} do job."] });

        MailArchiveInspectionResult result;
        try
        {
            await using var source = file.OpenReadStream();
            result = await inspector.InspectAsync(job.SourceType, source, ct);
        }
        catch (Exception exception) when (exception is InvalidDataException or ArgumentException or IOException)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["file"] = [exception.Message] });
        }

        var warning = result.Warnings.Count == 0 ? null : string.Join(" ", result.Warnings);
        job.RecordLocalInspection(
            result.CandidateMessages,
            result.InvalidMessages,
            result.SourceBytes,
            result.SourceSha256,
            warning,
            DateTimeOffset.UtcNow);
        AddAudit(
            db,
            tenant,
            principal,
            context,
            "mail.migration.archive.inspected",
            "MailMigrationJob",
            job.Id,
            new
            {
                job.SourceType,
                job.TargetAddress,
                job.State,
                job.CandidateMessages,
                job.FailedMessages,
                job.SourceBytes,
                job.SourceSha256,
                providerState = provider.State,
                importExecuted = false
            });
        await db.SaveChangesAsync(ct);

        return Results.Ok(new
        {
            job,
            inspection = result,
            importExecuted = false,
            provider = new { provider.State, provider.Description },
            nextStep = provider.State is "NOT_CONFIGURED" or "DEMO_ONLY"
                ? "A inspeção local terminou. Configure um provider real antes de importar mensagens."
                : "A inspeção local terminou. A etapa de importação depende do conector específico do provider."
        });
    }

    private static bool IsExpectedArchiveName(string sourceType, string fileName)
    {
        var extension = Path.GetExtension(Path.GetFileName(fileName));
        return sourceType switch
        {
            "EML" => extension.Equals(".eml", StringComparison.OrdinalIgnoreCase),
            "MBOX" => extension.Equals(".mbox", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".mbx", StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }

    private static string? NormalizeDomain(string input)
    {
        var value = input.Trim().TrimEnd('.').ToLowerInvariant();
        if (value.Length is 0 or > 253 || value.Contains('/') || value.Contains(':') || !value.Contains('.')) return null;
        return Uri.CheckHostName(value) == UriHostNameType.Dns ? value : null;
    }

    private static string? NormalizeAddress(string input)
    {
        try
        {
            return new MailAddress(input.Trim()).Address.ToLowerInvariant();
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private static void AddAudit(
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

    public sealed record DomainRequest(string Domain);
    public sealed record AliasRequest(string Address, string TargetAddress);
    public sealed record MailMigrationRequest(string SourceType, string SourceReference, string TargetAddress);
}
