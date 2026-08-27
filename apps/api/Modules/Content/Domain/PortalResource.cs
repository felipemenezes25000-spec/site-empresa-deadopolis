using System.Text.Json;
using MunicipalPlatform.Api.Platform.Tenancy;

namespace MunicipalPlatform.Api.Modules.Content.Domain;

public sealed class PortalResource : ITenantEntity
{
    private PortalResource() { }

    public PortalResource(Guid municipalityId, string kind, string slug, string title, string summary, string payloadJson, int displayOrder, Guid actorId)
    {
        if (municipalityId == Guid.Empty || actorId == Guid.Empty) throw new ArgumentException("Município e ator são obrigatórios.");
        Id = Guid.NewGuid();
        MunicipalityId = municipalityId;
        Kind = Require(kind, 32).ToUpperInvariant();
        Slug = Require(slug, 180).ToLowerInvariant();
        Title = Require(title, 220);
        Summary = Optional(summary, 500);
        PayloadJson = ValidateJson(Kind, payloadJson);
        DisplayOrder = displayOrder;
        Status = "DRAFT";
        CreatedBy = actorId;
        UpdatedBy = actorId;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public Guid Id { get; private set; }
    public Guid MunicipalityId { get; private set; }
    public string Kind { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Summary { get; private set; } = string.Empty;
    public string PayloadJson { get; private set; } = "{}";
    public string Status { get; private set; } = "DRAFT";
    public int DisplayOrder { get; private set; }
    public int Version { get; private set; }
    public DateTimeOffset? StartsAt { get; private set; }
    public DateTimeOffset? EndsAt { get; private set; }
    public DateTimeOffset? PublishedAt { get; private set; }
    public DateTimeOffset LastReviewedAt { get; private set; }
    public Guid CreatedBy { get; private set; }
    public Guid UpdatedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public void Update(string title, string summary, string payloadJson, int displayOrder, DateTimeOffset? startsAt, DateTimeOffset? endsAt, Guid actorId, DateTimeOffset changedAt)
    {
        if (Status == "ARCHIVED") throw new InvalidOperationException("Conteúdo arquivado deve ser restaurado antes de editar.");
        if (endsAt.HasValue && startsAt.HasValue && endsAt <= startsAt) throw new ArgumentException("A data final deve ser posterior à inicial.");
        Title = Require(title, 220);
        Summary = Optional(summary, 500);
        PayloadJson = ValidateJson(Kind, payloadJson);
        DisplayOrder = displayOrder;
        StartsAt = startsAt;
        EndsAt = endsAt;
        Touch(actorId, changedAt);
    }

    public void Publish(Guid actorId, DateTimeOffset at)
    {
        if (Status == "ARCHIVED") throw new InvalidOperationException("Conteúdo arquivado não pode ser publicado diretamente.");
        Status = "PUBLISHED";
        PublishedAt ??= at;
        LastReviewedAt = at;
        Touch(actorId, at);
    }

    public void Archive(Guid actorId, DateTimeOffset at)
    {
        Status = "ARCHIVED";
        Touch(actorId, at);
    }

    public void Restore(Guid actorId, DateTimeOffset at)
    {
        if (Status != "ARCHIVED") throw new InvalidOperationException("Somente conteúdo arquivado pode ser restaurado.");
        Status = "DRAFT";
        Touch(actorId, at);
    }

    private void Touch(Guid actorId, DateTimeOffset at)
    {
        if (actorId == Guid.Empty) throw new ArgumentException("Ator obrigatório.", nameof(actorId));
        UpdatedBy = actorId;
        UpdatedAt = at;
        Version++;
    }

    private static string ValidateJson(string kind, string value)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? "{}" : value.Trim();
        if (normalized.Length > 1_000_000) throw new ArgumentException("Payload excede 1 MB.", nameof(value));
        using var document = JsonDocument.Parse(normalized);
        if (kind == "PAGE") PageBlockPayloadValidator.Validate(document.RootElement);
        return normalized;
    }

    private static string Require(string value, int max)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Campo obrigatório.");
        var normalized = value.Trim();
        if (normalized.Length > max) throw new ArgumentException($"Campo deve possuir até {max} caracteres.");
        return normalized;
    }

    private static string Optional(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var normalized = value.Trim();
        if (normalized.Length > max) throw new ArgumentException($"Campo deve possuir até {max} caracteres.");
        return normalized;
    }
}
