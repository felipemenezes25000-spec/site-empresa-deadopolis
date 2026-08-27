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
        admin.MapPost("/{id:guid}/corrections", CreateCorrectionAsync).RequireAuthorization(policy => policy.RequireClaim("capability", "gazette.write"));
        admin.MapGet("/{id:guid}/integrity", IntegrityAsync).RequireAuthorization(policy => policy.RequireClaim("capability", "gazette.write"));

        endpoints.MapGet("/api/v1/gazette/{id:guid}/document", DownloadAsync).AllowAnonymous().WithTags("Gazette");
        endpoints.MapGet("/api/v1/gazette/{id:guid}/integrity", PublicIntegrityAsync).AllowAnonymous().WithTags("Gazette");
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
            var baseUrl = ResolvePublicBaseUrl(configuration, context);
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
        ISignatureValidator validator,
        CancellationToken cancellationToken)
    {
        if (signer.State == "NOT_CONFIGURED") return Results.Problem(title: "Assinatura ICP-Brasil não configurada", detail: signer.Description, statusCode: StatusCodes.Status503ServiceUnavailable);
        if (validator.State == "NOT_CONFIGURED") return Results.Problem(title: "Validação de assinatura não configurada", detail: "Não é permitido registrar uma assinatura sem validar sua integridade.", statusCode: StatusCodes.Status503ServiceUnavailable);

        var edition = await database.GazetteEditions.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (edition is null) return Results.NotFound();
        if (string.IsNullOrWhiteSpace(edition.Sha256)) return Results.Conflict(new { title = "Gere o PDF antes de assinar", status = 409 });
        if (await database.GazetteSignatures.AnyAsync(item => item.GazetteEditionId == id, cancellationToken))
            return Results.Conflict(new { title = "A edição já possui registro de assinatura", status = 409 });

        try
        {
            var signature = await signer.SignHashAsync(edition.Sha256, cancellationToken);
            if (signer.State != "DEMO_ONLY" && !signature.Certificate.IsIcpBrasil)
                return Results.Problem(title: "Certificado não ICP-Brasil", detail: "Assinaturas oficiais do Diário exigem certificado reconhecido como ICP-Brasil.", statusCode: StatusCodes.Status422UnprocessableEntity);

            var validation = await validator.ValidateAsync(edition.Sha256, signature.SignatureBase64, cancellationToken);
            if (!validation.IsValid)
                return Results.Problem(title: "Assinatura inválida", detail: validation.Detail, statusCode: StatusCodes.Status422UnprocessableEntity);

            var actor = RequireActor(principal);
            edition.RegisterSignature(signature.Certificate.Serial, signature.Certificate.Subject, signature.Certificate.Issuer, signature.SignedAt, actor);
            var integrityRecord = new GazetteSignature(
                tenant.RequireMunicipalityId(),
                edition.Id,
                signature.Provider,
                signature.SignatureBase64,
                signature.Certificate.Serial,
                signature.Certificate.Subject,
                signature.Certificate.Issuer,
                signature.Certificate.ValidFrom,
                signature.Certificate.ValidTo,
                signature.Certificate.IsIcpBrasil,
                signature.SignedAt,
                $"VALID:{validation.Provider}:{validation.Detail}");
            database.GazetteSignatures.Add(integrityRecord);
            AddAudit(database, tenant, actor, "gazette.signed", edition.Id, context.TraceIdentifier, new { signatureId = integrityRecord.Id, provider = signature.Provider, signature.Certificate.IsIcpBrasil, validator = validation.Provider, signer.State });
            await database.SaveChangesAsync(cancellationToken);
            return Results.Ok(new { edition = ToResponse(edition), signatureId = integrityRecord.Id, provider = signature.Provider, signature.Certificate.IsIcpBrasil, validation = new { validation.Provider, validation.Detail }, warning = signature.Certificate.IsIcpBrasil ? null : "Assinatura de demonstração; NÃO possui valor de assinatura ICP-Brasil." });
        }
        catch (Exception exception) when (exception is GazetteTransitionException or ArgumentException or ArgumentOutOfRangeException)
        {
            return Results.Conflict(new { title = "Transição de assinatura inválida", detail = exception.Message, status = 409 });
        }
    }

    private static async Task<IResult> PublishAsync(
        Guid id,
        ClaimsPrincipal principal,
        HttpContext context,
        ApplicationDbContext database,
        TenantContext tenant,
        ITimestampProvider timestamp,
        IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        var edition = await database.GazetteEditions.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (edition is null) return Results.NotFound();
        if (await database.GazettePublications.AnyAsync(item => item.GazetteEditionId == id, cancellationToken))
            return Results.Conflict(new { title = "A edição já possui registro de publicação", status = 409 });
        if (string.IsNullOrWhiteSpace(edition.Sha256) || string.IsNullOrWhiteSpace(edition.VerificationCode))
            return Results.Conflict(new { title = "A edição precisa possuir PDF, hash e código de verificação antes da publicação", status = 409 });

        TimestampResult? timestampReceipt = null;
        if (timestamp.State != "NOT_CONFIGURED")
        {
            try
            {
                timestampReceipt = await timestamp.TimestampHashAsync(edition.Sha256, cancellationToken);
            }
            catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
            {
                return Results.Problem(title: "Falha ao aplicar carimbo do tempo", detail: exception.Message, statusCode: StatusCodes.Status502BadGateway);
            }
        }

        var actor = RequireActor(principal);
        var publishedAt = DateTimeOffset.UtcNow;
        try
        {
            edition.Publish(actor, publishedAt);
            var publicUrl = $"{ResolvePublicBaseUrl(configuration, context)}/api/v1/gazette/{edition.Id}/document";
            var publication = new GazettePublication(tenant.RequireMunicipalityId(), edition.Id, publishedAt, edition.Sha256, edition.VerificationCode, publicUrl);
            database.GazettePublications.Add(publication);
            AddAudit(database, tenant, actor, "gazette.published", edition.Id, context.TraceIdentifier, new
            {
                publicationId = publication.Id,
                publication.Sha256,
                publication.VerificationCode,
                timestampState = timestamp.State,
                timestampProvider = timestampReceipt?.Provider,
                timestampAt = timestampReceipt?.Timestamp
            });
            await database.SaveChangesAsync(cancellationToken);
            return Results.Ok(new
            {
                edition = ToResponse(edition),
                publication = new { publication.Id, publication.PublicUrl, publication.Sha256, publication.VerificationCode, publication.PublishedAt },
                timestamp = new { state = timestamp.State, provider = timestampReceipt?.Provider, appliedAt = timestampReceipt?.Timestamp }
            });
        }
        catch (Exception exception) when (exception is GazetteTransitionException or GazetteImmutabilityException or ArgumentException)
        {
            return Results.Conflict(new { title = "Transição de publicação inválida", detail = exception.Message, status = 409 });
        }
    }

    private static async Task<IResult> CreateCorrectionAsync(
        Guid id,
        CreateCorrectionRequest request,
        ClaimsPrincipal principal,
        HttpContext context,
        ApplicationDbContext database,
        TenantContext tenant,
        CancellationToken cancellationToken)
    {
        var original = await database.GazetteEditions.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (original is null) return Results.NotFound();
        if (original.Status != GazetteStatus.Published)
            return Results.Conflict(new { title = "Somente edição publicada pode receber correção vinculada", status = 409 });
        if (request.Number <= 0 || request.Year is < 2000 or > 2200 || request.PublicationDate == default)
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["correction"] = ["Número, ano e data de publicação válidos são obrigatórios."] });
        if (request.PublicationDate < original.PublicationDate)
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["publicationDate"] = ["A correção não pode ter data anterior à edição original."] });
        if (await database.GazetteEditions.AnyAsync(item => item.Number == request.Number && item.Year == request.Year, cancellationToken))
            return Results.Conflict(new { title = "Já existe edição com esse número e ano", status = 409 });

        var actor = RequireActor(principal);
        try
        {
            var correctionEdition = GazetteEdition.Create(
                tenant.RequireMunicipalityId(),
                request.Number,
                request.Year,
                GazetteEditionType.Complementary,
                request.PublicationDate,
                actor);
            var link = new GazetteCorrection(tenant.RequireMunicipalityId(), original.Id, correctionEdition.Id, request.Reason, actor);
            database.GazetteEditions.Add(correctionEdition);
            database.GazetteCorrections.Add(link);
            AddAudit(database, tenant, actor, "gazette.correction.created", correctionEdition.Id, context.TraceIdentifier, new { link.Id, originalEditionId = original.Id, correctionEditionId = correctionEdition.Id, link.Reason });
            await database.SaveChangesAsync(cancellationToken);
            return Results.Created($"/api/v1/admin/gazette/{correctionEdition.Id}", new { edition = ToResponse(correctionEdition), correction = new { link.Id, link.OriginalEditionId, link.CorrectionEditionId, link.Reason, link.CreatedAt } });
        }
        catch (ArgumentException exception)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["correction"] = [exception.Message] });
        }
    }

    private static async Task<IResult> IntegrityAsync(Guid id, ApplicationDbContext database, CancellationToken cancellationToken) =>
        await BuildIntegrityResponseAsync(id, database, requirePublished: false, cancellationToken);

    private static async Task<IResult> PublicIntegrityAsync(Guid id, ApplicationDbContext database, CancellationToken cancellationToken) =>
        await BuildIntegrityResponseAsync(id, database, requirePublished: true, cancellationToken);

    private static async Task<IResult> BuildIntegrityResponseAsync(Guid id, ApplicationDbContext database, bool requirePublished, CancellationToken cancellationToken)
    {
        var edition = await database.GazetteEditions.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (edition is null || (requirePublished && edition.Status != GazetteStatus.Published)) return Results.NotFound();

        var signatures = await database.GazetteSignatures.AsNoTracking()
            .Where(item => item.GazetteEditionId == id)
            .OrderBy(item => item.SignedAt)
            .Select(item => new
            {
                item.Id,
                item.Provider,
                item.CertificateSerial,
                item.CertificateSubject,
                item.CertificateIssuer,
                item.CertificateValidFrom,
                item.CertificateValidTo,
                item.IsIcpBrasil,
                item.SignedAt,
                item.ValidationState
            })
            .ToListAsync(cancellationToken);
        var publication = await database.GazettePublications.AsNoTracking()
            .Where(item => item.GazetteEditionId == id)
            .Select(item => new { item.Id, item.PublishedAt, item.Sha256, item.VerificationCode, item.PublicUrl })
            .SingleOrDefaultAsync(cancellationToken);
        var links = await database.GazetteCorrections.AsNoTracking()
            .Where(item => item.OriginalEditionId == id || item.CorrectionEditionId == id)
            .Select(item => new { item.Id, item.OriginalEditionId, item.CorrectionEditionId, item.Reason, item.CreatedAt })
            .ToListAsync(cancellationToken);

        return Results.Ok(new
        {
            edition = ToResponse(edition),
            signatures,
            publication,
            corrections = links.Where(item => item.OriginalEditionId == id),
            corrects = links.Where(item => item.CorrectionEditionId == id)
        });
    }

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

    private static string ResolvePublicBaseUrl(IConfiguration configuration, HttpContext context)
    {
        var configured = configuration["PublicPortalBaseUrl"];
        var candidate = string.IsNullOrWhiteSpace(configured)
            ? $"{context.Request.Scheme}://{context.Request.Host}"
            : configured.Trim();
        if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            throw new ArgumentException("PublicPortalBaseUrl precisa ser uma URL HTTP/HTTPS absoluta.", nameof(configuration));
        return uri.ToString().TrimEnd('/');
    }

    private static Guid RequireActor(ClaimsPrincipal principal) => Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var actor) ? actor : throw new InvalidOperationException("Sessão sem identificador de usuário.");
    private static void AddAudit(ApplicationDbContext database, TenantContext tenant, Guid actor, string action, Guid id, string correlationId, object diff) => database.AuditEvents.Add(new AuditEvent(tenant.RequireMunicipalityId(), actor, action, "GazetteEdition", id.ToString(), JsonSerializer.Serialize(diff), correlationId));
    private static string ToWireStatus(GazetteStatus status) => status switch { GazetteStatus.Draft => "DRAFT", GazetteStatus.Review => "REVIEW", GazetteStatus.Approved => "APPROVED", GazetteStatus.Generated => "GENERATED", GazetteStatus.DigitallySigned => "SIGNED", GazetteStatus.Published => "PUBLISHED", _ => throw new ArgumentOutOfRangeException(nameof(status)) };

    public sealed record CreateGazetteRequest(int Number, int Year, string Type, DateOnly PublicationDate);
    public sealed record CreateCorrectionRequest(int Number, int Year, DateOnly PublicationDate, string Reason);
}
