using MunicipalPlatform.Api.Platform.Tenancy;

namespace MunicipalPlatform.Api.Modules.Content.Domain;

public sealed class NewsArticle : ITenantEntity
{
    private NewsArticle()
    {
    }

    private NewsArticle(Guid municipalityId, string title, string slug, Guid actorId)
    {
        Id = Guid.NewGuid();
        MunicipalityId = municipalityId;
        Title = RequireText(title, nameof(title), 180);
        Slug = RequireText(slug, nameof(slug), 180).ToLowerInvariant();
        Category = "GERAL";
        CreatedBy = actorId;
        UpdatedBy = actorId;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
        Status = EditorialStatus.Draft;
    }

    public Guid Id { get; private set; }
    public Guid MunicipalityId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string Summary { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public string Category { get; private set; } = "GERAL";
    public string? CoverImageUrl { get; private set; }
    public string? CoverImageAlt { get; private set; }
    public EditorialStatus Status { get; private set; }
    public int Version { get; private set; }
    public bool IsFeatured { get; private set; }
    public Guid CreatedBy { get; private set; }
    public Guid UpdatedBy { get; private set; }
    public Guid? ApprovedBy { get; private set; }
    public Guid? PublishedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? ScheduledFor { get; private set; }
    public DateTimeOffset? PublishedAt { get; private set; }

    public static NewsArticle Create(Guid municipalityId, string title, string slug, Guid actorId)
    {
        EnsureIds(municipalityId, actorId);
        return new NewsArticle(municipalityId, title, slug, actorId);
    }

    public void UpdateDraft(
        string title,
        string summary,
        string body,
        string? coverImageUrl,
        string? coverImageAlt,
        string? category,
        bool isFeatured,
        Guid actorId,
        DateTimeOffset changedAt)
    {
        EnsureMutable();
        Title = RequireText(title, nameof(title), 180);
        Summary = RequireText(summary, nameof(summary), 320);
        Body = RequireText(body, nameof(body), 100_000);
        CoverImageUrl = string.IsNullOrWhiteSpace(coverImageUrl) ? null : coverImageUrl.Trim();
        CoverImageAlt = string.IsNullOrWhiteSpace(coverImageAlt) ? null : coverImageAlt.Trim();
        Category = NormalizeCategory(category);
        IsFeatured = isFeatured;
        Touch(actorId, changedAt);
    }

    public void SubmitForReview(Guid actorId, DateTimeOffset changedAt)
    {
        RequireStatus(EditorialStatus.Draft, "IN_REVIEW");
        Status = EditorialStatus.InReview;
        Touch(actorId, changedAt);
    }

    public void Approve(Guid actorId, DateTimeOffset changedAt)
    {
        RequireStatus(EditorialStatus.InReview, "APPROVED");
        Status = EditorialStatus.Approved;
        ApprovedBy = actorId;
        Touch(actorId, changedAt);
    }

    public void Schedule(DateTimeOffset publishAt, Guid actorId, DateTimeOffset changedAt)
    {
        RequireStatus(EditorialStatus.Approved, "SCHEDULED");
        if (publishAt <= changedAt)
        {
            throw new ArgumentOutOfRangeException(nameof(publishAt), "O agendamento precisa estar no futuro.");
        }

        Status = EditorialStatus.Scheduled;
        ScheduledFor = publishAt;
        Touch(actorId, changedAt);
    }

    public void Publish(Guid actorId, DateTimeOffset publishedAt)
    {
        if (Status is not (EditorialStatus.Approved or EditorialStatus.Scheduled))
        {
            throw new EditorialTransitionException(
                $"A notícia precisa estar APPROVED ou SCHEDULED para publicar; estado atual: {Status}.");
        }

        Status = EditorialStatus.Published;
        PublishedAt = publishedAt;
        PublishedBy = actorId;
        Touch(actorId, publishedAt);
    }

    private static string RequireText(string value, string parameterName, int maxLength)
    {
        var normalized = value.Trim();
        if (normalized.Length is 0 || normalized.Length > maxLength)
        {
            throw new ArgumentException($"{parameterName} deve ter entre 1 e {maxLength} caracteres.", parameterName);
        }

        return normalized;
    }

    private static string NormalizeCategory(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "GERAL";
        var normalized = value.Trim().ToUpperInvariant();
        if (normalized.Length > 80 || normalized.Any(character => !char.IsAsciiLetterOrDigit(character) && character != '_'))
            throw new ArgumentException("Categoria deve conter apenas letras sem acento, números e sublinhado.", nameof(value));
        return normalized;
    }

    private static void EnsureIds(Guid municipalityId, Guid actorId)
    {
        if (municipalityId == Guid.Empty || actorId == Guid.Empty)
        {
            throw new ArgumentException("Município e ator são obrigatórios.");
        }
    }

    private void EnsureMutable()
    {
        if (Status is EditorialStatus.Published or EditorialStatus.Archived)
        {
            throw new EditorialTransitionException("Conteúdo publicado ou arquivado exige uma nova versão.");
        }
    }

    private void RequireStatus(EditorialStatus expected, string target)
    {
        if (Status != expected)
        {
            throw new EditorialTransitionException(
                $"Transição para {target} exige estado {expected}; estado atual: {Status}.");
        }
    }

    private void Touch(Guid actorId, DateTimeOffset changedAt)
    {
        if (actorId == Guid.Empty)
        {
            throw new ArgumentException("O ator é obrigatório.", nameof(actorId));
        }

        UpdatedBy = actorId;
        UpdatedAt = changedAt;
        Version++;
    }
}
