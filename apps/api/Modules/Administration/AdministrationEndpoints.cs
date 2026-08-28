using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MunicipalPlatform.Api.Infrastructure.Persistence;
using MunicipalPlatform.Api.Modules.Content.Domain;
using MunicipalPlatform.Api.Modules.Gazette.Providers;
using MunicipalPlatform.Api.Modules.Mail.Providers;
using MunicipalPlatform.Api.Modules.Media.Providers;
using MunicipalPlatform.Api.Modules.Media.Services;
using MunicipalPlatform.Api.Modules.Operations.Domain;
using MunicipalPlatform.Api.Modules.Services.Domain;
using MunicipalPlatform.Api.Platform.Storage;
using MunicipalPlatform.Api.Platform.Tenancy;

namespace MunicipalPlatform.Api.Modules.Administration;

public static class AdministrationEndpoints
{
    public static IEndpointRouteBuilder MapAdministrationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/admin/dashboard", DashboardAsync).RequireAuthorization().WithTags("Admin");
        var services = endpoints.MapGroup("/api/v1/admin/services").RequireAuthorization(p => p.RequireClaim("capability", "services.manage")).WithTags("Admin", "Services");
        services.MapGet("/", ListServicesAsync); services.MapPost("/", CreateServiceAsync); services.MapPut("/{id:guid}", UpdateServiceAsync);
        var departments = endpoints.MapGroup("/api/v1/admin/departments").RequireAuthorization(p => p.RequireClaim("capability", "services.manage")).WithTags("Admin", "Departments");
        departments.MapGet("/", ListDepartmentsAsync); departments.MapPost("/", CreateDepartmentAsync); departments.MapPut("/{id:guid}", UpdateDepartmentAsync);
        endpoints.MapGet("/api/v1/admin/integrations", IntegrationsAsync).RequireAuthorization(p => p.RequireClaim("capability", "settings.manage")).WithTags("Admin", "Operations");
        endpoints.MapGet("/api/v1/admin/compliance", ComplianceAsync).RequireAuthorization(p => p.RequireClaim("capability", "settings.manage")).WithTags("Admin", "Operations", "Compliance");
        return endpoints;
    }

    private static async Task<IResult> DashboardAsync(ApplicationDbContext db, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        return Results.Ok(new
        {
            editorial = new
            {
                drafts = await db.NewsArticles.CountAsync(x => x.Status == EditorialStatus.Draft, ct),
                review = await db.NewsArticles.CountAsync(x => x.Status == EditorialStatus.InReview, ct),
                scheduled = await db.NewsArticles.CountAsync(x => x.Status == EditorialStatus.Scheduled, ct)
            },
            support = new
            {
                open = await db.Tickets.CountAsync(x => x.Status != "RESOLVED", ct),
                breached = await db.Tickets.CountAsync(x => x.Status != "RESOLVED" && x.ResolutionDueAt < now, ct)
            },
            content = new
            {
                resources = await db.PortalResources.CountAsync(ct),
                services = await db.Services.CountAsync(ct),
                mediaQuarantined = await db.MediaAssets.CountAsync(x => x.Status == "QUARANTINED", ct)
            },
            integrations = (await db.IntegrationStatuses.AsNoTracking().OrderBy(x => x.Provider).ToListAsync(ct)).Select(ToIntegrationResponse)
        });
    }

    private static async Task<IResult> ListServicesAsync(ApplicationDbContext db, CancellationToken ct) => Results.Ok(await db.Services.AsNoTracking().OrderBy(x => x.Name).ToListAsync(ct));

    private static async Task<IResult> CreateServiceAsync(ServiceRequest request, ClaimsPrincipal principal, HttpContext context, ApplicationDbContext db, TenantContext tenant, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Slug) || request.Slug.Any(c => !char.IsAsciiLetterOrDigit(c) && c != '-')) return Results.ValidationProblem(new Dictionary<string, string[]> { ["slug"] = ["Slug inválido."] });
        var normalizedSlug = request.Slug.Trim().ToLowerInvariant();
        if (await db.Services.AnyAsync(x => x.Slug == normalizedSlug, ct)) return Results.Conflict(new { title = "Slug já usado", status = 409 });
        try
        {
            var service = new ServiceItem(tenant.RequireMunicipalityId(), request.Name, normalizedSlug, request.Description, request.Area, request.Audience, request.IsOnline, request.OnlineUrl);
            service.Update(request.ToDetails()); service.SetPublished(request.Published); db.Services.Add(service);
            Audit(db, tenant, principal, context, "service.created", service.Id, new { service.Slug, service.Status });
            await db.SaveChangesAsync(ct); return Results.Created($"/api/v1/admin/services/{service.Id}", service);
        }
        catch (ArgumentException ex) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["service"] = [ex.Message] }); }
    }

    private static async Task<IResult> UpdateServiceAsync(Guid id, ServiceRequest request, ClaimsPrincipal principal, HttpContext context, ApplicationDbContext db, TenantContext tenant, CancellationToken ct)
    {
        var service = await db.Services.SingleOrDefaultAsync(x => x.Id == id, ct); if (service is null) return Results.NotFound();
        try { service.Update(request.ToDetails()); service.SetPublished(request.Published); Audit(db, tenant, principal, context, "service.updated", service.Id, new { service.Status, service.IsFeatured }); await db.SaveChangesAsync(ct); return Results.Ok(service); }
        catch (ArgumentException ex) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["service"] = [ex.Message] }); }
    }

    private static async Task<IResult> ListDepartmentsAsync(ApplicationDbContext db, CancellationToken ct) => Results.Ok(await db.Departments.AsNoTracking().OrderBy(x => x.DisplayOrder).ToListAsync(ct));

    private static async Task<IResult> CreateDepartmentAsync(DepartmentRequest request, ClaimsPrincipal principal, HttpContext context, ApplicationDbContext db, TenantContext tenant, CancellationToken ct)
    {
        var normalizedSlug = request.Slug.Trim().ToLowerInvariant();
        if (await db.Departments.AnyAsync(x => x.Slug == normalizedSlug, ct)) return Results.Conflict(new { title = "Slug já usado", status = 409 });
        var item = new Department(tenant.RequireMunicipalityId(), request.Name, normalizedSlug, request.Acronym, request.DisplayOrder);
        item.Update(request.Name, request.Acronym, request.ManagerName, request.Phone, request.Email, request.Address, request.OpeningHours, request.DisplayOrder);
        db.Departments.Add(item); Audit(db, tenant, principal, context, "department.created", item.Id, new { item.Slug }); await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/admin/departments/{item.Id}", item);
    }

    private static async Task<IResult> UpdateDepartmentAsync(Guid id, DepartmentRequest request, ClaimsPrincipal principal, HttpContext context, ApplicationDbContext db, TenantContext tenant, CancellationToken ct)
    {
        var item = await db.Departments.SingleOrDefaultAsync(x => x.Id == id, ct); if (item is null) return Results.NotFound();
        try { item.Update(request.Name, request.Acronym, request.ManagerName, request.Phone, request.Email, request.Address, request.OpeningHours, request.DisplayOrder); item.SetActive(request.Active); Audit(db, tenant, principal, context, "department.updated", item.Id, new { item.Name, item.IsActive }); await db.SaveChangesAsync(ct); return Results.Ok(item); }
        catch (ArgumentException ex) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["department"] = [ex.Message] }); }
    }

    private static async Task<IResult> IntegrationsAsync(ApplicationDbContext db, CancellationToken ct) => Results.Ok((await db.IntegrationStatuses.AsNoTracking().OrderBy(x => x.Provider).ToListAsync(ct)).Select(ToIntegrationResponse));

    private static object ToIntegrationResponse(IntegrationStatus status) => new { status.Provider, state = status.State.ToExternalState(), status.Message, status.LastErrorCode, status.LastCheckedAt };

    private static async Task<IResult> ComplianceAsync(
        ApplicationDbContext db,
        IObjectStorageProvider storage,
        IDigitalSigner signer,
        ITimestampProvider timestamp,
        IInstitutionalEmailProvider email,
        IMalwareScanner malware,
        MediaVariantService mediaVariants,
        CancellationToken ct)
    {
        var databaseReady = false;
        try { databaseReady = await db.Database.CanConnectAsync(ct); }
        catch { databaseReady = false; }

        var capabilities = mediaVariants.Capabilities;
        var webpReady = capabilities.Webp.State == "AVAILABLE";
        var integrations = await db.IntegrationStatuses.AsNoTracking().OrderBy(item => item.Provider).ToListAsync(ct);
        var linkTotal = await db.LinkChecks.CountAsync(ct);
        var linkDegraded = await db.LinkChecks.CountAsync(item => item.State != "HEALTHY", ct);
        var migrationEvidence = await db.MigrationEvidences.CountAsync(ct);
        var backupTotal = await db.BackupEvidences.CountAsync(ct);
        var restoreTested = await db.BackupEvidences.CountAsync(item => item.RestoreTestedAt.HasValue, ct);
        var signatures = await db.GazetteSignatures.CountAsync(ct);
        var publications = await db.GazettePublications.CountAsync(ct);
        var corrections = await db.GazetteCorrections.CountAsync(ct);

        return Results.Ok(new
        {
            generatedAt = DateTimeOffset.UtcNow,
            readiness = new { state = databaseReady && webpReady ? "READY" : "NOT_READY", databaseReady },
            providers = new
            {
                storage = new { state = storage.State, description = storage.Description },
                digitalSignature = new { state = signer.State, description = signer.Description },
                timestamp = new { state = timestamp.State, description = "Carimbo do tempo depende de provider externo contratado e configurado." },
                institutionalEmail = new { state = email.State, description = email.Description },
                malwareScanner = new { state = malware.State, description = malware.Description },
                mediaVariants = new { webp = capabilities.Webp, avif = capabilities.Avif }
            },
            evidence = new
            {
                links = new { total = linkTotal, degraded = linkDegraded },
                migration = new { total = migrationEvidence },
                backups = new { total = backupTotal, restoreTested },
                gazette = new { signatures, publications, corrections }
            },
            integrations = integrations.Select(ToIntegrationResponse),
            externalDependencies = new[]
            {
                new { name = "Storage de produção", state = storage.State, requirement = "Configurar storage compatível com retenção, backup e credenciais gerenciadas." },
                new { name = "ICP-Brasil", state = signer.State, requirement = "Contratar/configurar certificado ou serviço ICP-Brasil e validar a política institucional." },
                new { name = "Carimbo do tempo", state = timestamp.State, requirement = "Configurar uma autoridade de carimbo do tempo quando exigido pela política do Diário." },
                new { name = "E-mail institucional", state = email.State, requirement = "Contratar/configurar provider, DNS e credenciais fora do repositório." },
                new { name = "Scanner antimalware", state = malware.State, requirement = "Configurar scanner de produção antes de liberar uploads oficiais." }
            }
        });
    }
    private static void Audit(ApplicationDbContext db, TenantContext tenant, ClaimsPrincipal principal, HttpContext context, string action, Guid id, object diff) { var actor = Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var parsed) ? parsed : throw new InvalidOperationException("Sessão inválida."); db.AuditEvents.Add(new AuditEvent(tenant.RequireMunicipalityId(), actor, action, "Administration", id.ToString(), JsonSerializer.Serialize(diff), context.TraceIdentifier)); }

    public sealed record ServiceRequest(string Name, string Slug, string Description, string Area, string Audience, Guid? DepartmentId, string? Requirements, string? Documents, string? Steps, string? ExpectedDuration, string? Cost, string? Channels, bool IsOnline, string? OnlineUrl, string? Phone, string? Address, string? OpeningHours, string? LegalBasis, bool IsFeatured, bool Published)
    {
        public ServiceDetails ToDetails() => new(Name, Description, Area, Audience, DepartmentId, Requirements, Documents, Steps, ExpectedDuration, Cost, Channels, IsOnline, OnlineUrl, Phone, Address, OpeningHours, LegalBasis, IsFeatured);
    }
    public sealed record DepartmentRequest(string Name, string Slug, string Acronym, string ManagerName, string Phone, string Email, string Address, string OpeningHours, int DisplayOrder, bool Active);
}
