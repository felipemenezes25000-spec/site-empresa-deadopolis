using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MunicipalPlatform.Api.Infrastructure.Persistence;
using MunicipalPlatform.Api.Modules.Gazette.Domain;
using MunicipalPlatform.Api.Modules.Gazette.Providers;
using MunicipalPlatform.Api.Modules.Gazette.Services;
using MunicipalPlatform.Api.Modules.Operations.Domain;
using MunicipalPlatform.Api.Platform.Storage;
using MunicipalPlatform.Api.Platform.Tenancy;
using QRCoder;

namespace MunicipalPlatform.Api.Modules.Gazette;

public static class GazetteEndpoints
{
    public static IEndpointRouteBuilder MapGazetteEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var admin = endpoints.MapGroup("/api/v1/admin/gazette").WithTags("Admin", "Gazette");
        admin.MapGet("/", ListAsync).RequireAuthorization(policy => policy.RequireClaim("capability", "gazette.write"));
        admin.MapPost("/", CreateAsync).RequireAuthorization(policy => policy.RequireClaim("capability", "gazette.write"));
        admin.MapPut("/{id:guid}/composition", ComposeAsync).RequireAuthorization(policy => policy.RequireClaim("capability", "gazette.write"));
        admin.MapPost("/{id:guid}/submit", SubmitAsync).RequireAuthorization(policy => policy.RequireClaim("capability", "gazette.write"));
        admin.MapPost("/{id:guid}/approve", ApproveAsync).RequireAuthorization(policy => policy.RequireClaim("capability", "gazette.write"));
        admin.MapPost("/{id:guid}/generate", GenerateAsync).RequireAuthorization(policy => policy.RequireClaim("capability", "gazette.write"));
        admin.MapPost("/{id:guid}/sign", SignAsync).RequireAuthorization(policy => policy.RequireClaim("capability", "gazette.sign"));
        admin.MapPost("/{id:guid}/publish", PublishAsync).RequireAuthorization(policy => policy.RequireClaim("capability", "gazette.publish"));

        endpoints.MapGet("/api/v1/gazette/{id:guid}/document", DownloadAsync).AllowAnonymous().WithTags("Gazette");
        endpoints.MapGet("/api/v1/gazette/verify/{code}/qr.svg", QrAsync).AllowAnonymous().WithTags("Gazette");
        endpoints.MapGet("/api/v1/admin/providers", ProviderStatus).RequireAuthorization(policy => policy.RequireClaim("capability", "settings.manage")).WithTags("Admin", "Operations");
        return endpoints;
    }

    private static async Task<IResult> ListAsync(ApplicationDbContext database, CancellationToken cancellationToken)
    {
        var editions = await database.GazetteEditions.AsNoTracking()
            .OrderByDescending(item => item.Year).ThenByDescending(item => item.Number)
            .Select(item => new { item.Id, item.Number, item.Year, item.Type, item.PublicationDate, item.Status, item.Sha256, item.VerificationCode, item.SignedAt, item.PublishedAt })
            .ToListAsync(cancellationToken);
        return Results.Ok(editions.Select(item => new { item.Id, item.Number, item.Year, Type = item.Type.ToString().ToUpperInvariant(), item.PublicationDate, Status = ToWireStatus(item.Status), item.Sha256, item.VerificationCode, item.SignedAt, item.PublishedAt }));
    }

    private static async Task<IResult> CreateAsync(
        CreateGazetteRequest request,
        ClaimsPrincipal principal,
        HttpContext context,
        ApplicationDbContext database,
        TenantContext tenant,
        CancellationToken cancellationToken)
    {
        if (request.Number <= 0 || request.Year is < 2000 or > 2200 || request.PublicationDate == default)
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["edition"] = ["Número, ano e data de publicação válidos são obrigatórios."] });
        if (!Enum.TryParse<GazetteEditionType>(request.Type, true, out var type))
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["type"] = ["Use ORDINARY, EXTRAORDINARY ou COMPLEMENTARY."] });
        if (await database.GazetteEditions.AnyAsync(item => item.Number == request.Number && item.Year == request.Year, cancellationToken))
            return Results.Conflict(new { title = "Edição já existente", status = 409 });

        var actor = RequireActor(principal);
        var edition = GazetteEdition.Create(tenant.RequireMunicipalityId(), request.Number, request.Year, type, request.PublicationDate, actor);
        database.GazetteEditions.Add(edition);
        AddAudit(database, tenant, actor, "gazette.created", edition.Id, context.TraceIdentifier, new { edition.Number, edition.Year, type = type.ToString() });
        await database.SaveChangesAsync(cancellationToken);
        return Results.Created($"/api/v1/admin/gazette/{edition.Id}", ToResponse(edition));
    }

    private static async Task<IResult> ComposeAsync(
        Guid id,
        GazetteComposition request,
        GazetteDocumentService documents,
        ClaimsPrincipal principal,
        HttpContext context,
        ApplicationDbContext database,
        TenantContext tenant,
        CancellationToken cancellationToken)
    {
        var edition = await database.GazetteEditions.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (edition is null) return Results.NotFound();
        try
        {
            var normalized = documents.NormalizeComposition(request);
            var actor = RequireActor(principal);
            edition.SetComposition(normalized, actor, DateTimeOffset.UtcNow);
            AddAudit(database, tenant, actor, "gazette.composition.updated", edition.Id, context.TraceIdentifier, new { sections = request.Sections.Count });
            await database.SaveChangesAsync(cancellationToken);
            return Results.Ok(ToResponse(edition));
        }
        catch (ArgumentException exception)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["composition"] = [exception.Message] });
        }
        catch (GazetteTransitionException exception)
        {
            return Results.Conflict(new { title = "Edição não pode mais ser editada", detail = exception.Message, status = 409 });
        }
    }

    private static Task<IResult> SubmitAsync(Guid id, ClaimsPrincipal principal, HttpContext context, ApplicationDbContext database, TenantContext tenant, CancellationToken cancellationToken) =>
        TransitionAsync(id, "gazette.submitted", principal, context, database, tenant, (edition, actor, at) => edition.SubmitForReview(actor, at), cancellationToken);

    private static Task<IResult> ApproveAsync(Guid id, ClaimsPrincipal principal, HttpContext context, ApplicationDbContext database, TenantContext tenant, CancellationToken cancellationToken) =>
        TransitionAsync(id, "gazette.approved", principal, context, database, tenant, (edition, actor, at) => edition.Approve(actor, at), cancellationToken);

    private static async Task<IResult> GenerateAsync(
        Guid id,
        ClaimsPrincipal principal,
        HttpContext context,
        ApplicationDbContext database,
        TenantContext tenant,
        GazetteDocumentService documents,
        IObjectStorageProvider storage,
        IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        if (storage.State == "NOT_CONFIGURED") return Results.Problem(title: "Storage não configurado", detail: storage.Description, statusCode: StatusCodes.Status503ServiceUnavailable);
        var edition = await database.GazetteEditions.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (edition is null) return Results.NotFound();
        try
        {
            var baseUrl = configuration["PublicPortalBaseUrl"] ?? $"{context.Request.Scheme}://{context.Request.Host}";
            var result = documents.Generate(edition, baseUrl);
            var objectKey = $"gazette/{edition.Year}/{edition.Number:D5}-{result.Sha256[..12]}.pdf";
            await storage.SaveAsync(objectKey, result.PdfBytes, cancellationToken);
            var actor = RequireActor(principal);
            edition.RegisterGeneratedDocument(objectKey, result.Sha256, result.VerificationCode, actor, DateTimeOffset.UtcNow);
            AddAudit(database, tenant, actor, "gazette.generated", edition.Id, context.TraceIdentifier, new { result.Sha256, result.ContentSha256, result.VerificationCode, storage = storage.State });
            await database.SaveChangesAsync(cancellationToken);
            return Results.Ok(new { edition = ToResponse(edition), result.ContentSha256, storage = storage.State });
        }
        catch (Exception exception) when (exception is ArgumentException or GazetteTransitionException)
        {
            return Results.Conflict(new { title = "Não foi possível gerar a edição", detail = exception.Message, status = 409 });
        }
    }

    private static async Task<IResult> SignAsync(
        Guid id,
        ClaimsPrincipal principal,
        HttpContext context,
        ApplicationDbContext database,
        TenantContext tenant,
        IDigitalSigner signer,
        CancellationToken cancellationToken)
    {
        if (signer.State == "NOT_CONFIGURED") return Results.Problem(title: "Assinatura ICP-Brasil não configurada", detail: signer.Description, statusCode: StatusCodes.Status503ServiceUnavailable);
        var edition = await database.GazetteEditions.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (edition is null) return Results.NotFound();
        if (string.IsNullOrWhiteSpace(edition.Sha256)) return Results.Conflict(new { title = "Gere o PDF antes de assinar", status = 409 });
        try
        {
            var signature = await signer.SignHashAsync(edition.Sha256, cancellationToken);
            var actor = RequireActor(principal);
            edition.RegisterSignature(signature.Certificate.Serial, signature.Certificate.Subject, signature.Certificate.Issuer, signature.SignedAt, actor);
            AddAudit(database, tenant, actor, "gazette.signed", edition.Id, context.TraceIdentifier, new { provider = signature.Provider, signature.Certificate.IsIcpBrasil, signer.State });
            await database.SaveChangesAsync(cancellationToken);
            return Results.Ok(new { edition = ToResponse(edition), provider = signature.Provider, signature.Certificate.IsIcpBrasil, warning = signature.Certificate.IsIcpBrasil ? null : "Assinatura de demonstração; NÃO possui valor de assinatura ICP-Brasil." });
        }
        catch (GazetteTransitionException exception)
        {
            return Results.Conflict(new { title = "Transição de assinatura inválida", detail = exception.Message, status = 409 });
        }
    }

    private static Task<IResult> PublishAsync(Guid id, ClaimsPrincipal principal, HttpContext context, ApplicationDbContext database, TenantContext tenant, CancellationToken cancellationToken) =>
        TransitionAsync(id, "gazette.published", principal, context, database, tenant, (edition, actor, at) => edition.Publish(actor, at), cancellationToken);

    private static async Task<IResult> TransitionAsync(
        Guid id,
        string action,
        ClaimsPrincipal principal,
        HttpContext context,
        ApplicationDbContext database,
        TenantContext tenant,
        Action<GazetteEdition, Guid, DateTimeOffset> transition,
        CancellationToken cancellationToken)
    {
        var edition = await database.GazetteEditions.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (edition is null) return Results.NotFound();
        var actor = RequireActor(principal);
        try
        {
            transition(edition, actor, DateTimeOffset.UtcNow);
            AddAudit(database, tenant, actor, action, edition.Id, context.TraceIdentifier, new { status = ToWireStatus(edition.Status) });
            await database.SaveChangesAsync(cancellationToken);
            return Results.Ok(ToResponse(edition));
        }
        catch (Exception exception) when (exception is GazetteTransitionException or GazetteImmutabilityException)
        {
            return Results.Conflict(new { title = "Transição do Diário inválida", detail = exception.Message, status = 409 });
        }
    }

    private static async Task<IResult> DownloadAsync(Guid id, ApplicationDbContext database, IObjectStorageProvider storage, CancellationToken cancellationToken)
    {
        var edition = await database.GazetteEditions.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id && item.Status == GazetteStatus.Published, cancellationToken);
        if (edition is null || string.IsNullOrWhiteSpace(edition.DocumentObjectKey)) return Results.NotFound();
        if (storage.State == "NOT_CONFIGURED") return Results.Problem(title: "Documento temporariamente indisponível", detail: "Storage de produção ainda não configurado.", statusCode: 503);
        var bytes = await storage.ReadAsync(edition.DocumentObjectKey, cancellationToken);
        return bytes is null ? Results.NotFound() : Results.File(bytes, "application/pdf", $"diario-{edition.Year}-{edition.Number:D5}.pdf", enableRangeProcessing: true);
    }

    private static async Task<IResult> QrAsync(string code, HttpContext context, ApplicationDbContext database, CancellationToken cancellationToken)
    {
        var exists = await database.GazetteEditions.AsNoTracking().AnyAsync(item => item.VerificationCode == code && item.Status == GazetteStatus.Published, cancellationToken);
        if (!exists) return Results.NotFound();
        var target = $"{context.Request.Scheme}://{context.Request.Host}/verificar/{Uri.EscapeDataString(code)}";
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(target, QRCodeGenerator.ECCLevel.Q);
        var svg = new SvgQRCode(data).GetGraphic(4);
        return Results.Text(svg, "image/svg+xml", Encoding.UTF8);
    }

    private static IResult ProviderStatus(IObjectStorageProvider storage, IDigitalSigner signer, ITimestampProvider timestamp, ICertificateProvider certificate, ISignatureValidator validator) => Results.Ok(new
    {
        storage = new { state = storage.State, storage.Description },
        signature = new { state = signer.State, signer.Description },
        certificate = new { state = certificate.State },
        timestamp = new { state = timestamp.State },
        validation = new { state = validator.State }
    });

    private static object ToResponse(GazetteEdition edition) => new
    {
        edition.Id,
        edition.Number,
        edition.Year,
        type = edition.Type.ToString().ToUpperInvariant(),
        edition.PublicationDate,
        status = ToWireStatus(edition.Status),
        edition.Sha256,
        edition.VerificationCode,
        edition.DocumentObjectKey,
        edition.SignedAt,
        edition.PublishedAt
    };

    private static Guid RequireActor(ClaimsPrincipal principal) => Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var actor) ? actor : throw new InvalidOperationException("Sessão sem identificador de usuário.");
    private static void AddAudit(ApplicationDbContext database, TenantContext tenant, Guid actor, string action, Guid id, string correlationId, object diff) => database.AuditEvents.Add(new AuditEvent(tenant.RequireMunicipalityId(), actor, action, "GazetteEdition", id.ToString(), JsonSerializer.Serialize(diff), correlationId));
    private static string ToWireStatus(GazetteStatus status) => status switch { GazetteStatus.Draft => "DRAFT", GazetteStatus.Review => "REVIEW", GazetteStatus.Approved => "APPROVED", GazetteStatus.Generated => "GENERATED", GazetteStatus.DigitallySigned => "SIGNED", GazetteStatus.Published => "PUBLISHED", _ => throw new ArgumentOutOfRangeException(nameof(status)) };

    public sealed record CreateGazetteRequest(int Number, int Year, string Type, DateOnly PublicationDate);
}
