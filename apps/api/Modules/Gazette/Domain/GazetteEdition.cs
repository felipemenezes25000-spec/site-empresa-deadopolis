using System.Text.Json;
using System.Text.RegularExpressions;
using MunicipalPlatform.Api.Platform.Tenancy;

namespace MunicipalPlatform.Api.Modules.Gazette.Domain;

public sealed partial class GazetteEdition : ITenantEntity
{
    private GazetteEdition()
    {
    }

    private GazetteEdition(Guid municipalityId, int number, int year, GazetteEditionType type, DateOnly publicationDate, Guid actorId)
    {
        Id = Guid.NewGuid();
        MunicipalityId = municipalityId;
        Number = number;
        Year = year;
        Type = type;
        PublicationDate = publicationDate;
        CreatedBy = actorId;
        UpdatedBy = actorId;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
        CompositionJson = "{\"sections\":[]}";
    }

    public Guid Id { get; private set; }
    public Guid MunicipalityId { get; private set; }
    public int Number { get; private set; }
    public int Year { get; private set; }
    public GazetteEditionType Type { get; private set; }
    public DateOnly PublicationDate { get; private set; }
    public GazetteStatus Status { get; private set; }
    public bool IsLegacy { get; private set; }
    public string CompositionJson { get; private set; } = "{\"sections\":[]}";
    public string? DocumentObjectKey { get; private set; }
    public string? Sha256 { get; private set; }
    public string? VerificationCode { get; private set; }
    public string? CertificateSerial { get; private set; }
    public string? CertificateSubject { get; private set; }
    public string? CertificateIssuer { get; private set; }
    public DateTimeOffset? SignedAt { get; private set; }
    public DateTimeOffset? PublishedAt { get; private set; }
    public Guid CreatedBy { get; private set; }
    public Guid UpdatedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static GazetteEdition Create(Guid municipalityId, int number, int year, GazetteEditionType type, DateOnly publicationDate, Guid actorId)
    {
        if (municipalityId == Guid.Empty || actorId == Guid.Empty || number <= 0 || year < 2000) throw new ArgumentException("Município, ator, número e ano válidos são obrigatórios.");
        return new GazetteEdition(municipalityId, number, year, type, publicationDate, actorId);
    }

    public void SetComposition(string compositionJson, Guid actorId, DateTimeOffset changedAt)
    {
        EnsureNotPublished();
        if (Status is not (GazetteStatus.Draft or GazetteStatus.Review)) throw new GazetteTransitionException("A composição só pode ser alterada em DRAFT ou REVIEW.");
        if (string.IsNullOrWhiteSpace(compositionJson) || compositionJson.Length > 4_000_000) throw new ArgumentException("Composição inválida ou excessivamente grande.", nameof(compositionJson));
        using var _ = JsonDocument.Parse(compositionJson);
        CompositionJson = compositionJson;
        Touch(actorId, changedAt);
    }

    public void SubmitForReview(Guid actorId, DateTimeOffset changedAt)
    {
        RequireStatus(GazetteStatus.Draft, "REVIEW");
        if (CompositionJson == "{\"sections\":[]}") throw new GazetteTransitionException("Inclua ao menos uma seção e um ato antes da revisão.");
        Status = GazetteStatus.Review;
        Touch(actorId, changedAt);
    }

    public void Approve(Guid actorId, DateTimeOffset changedAt)
    {
        RequireStatus(GazetteStatus.Review, "APPROVED");
        Status = GazetteStatus.Approved;
        Touch(actorId, changedAt);
    }

    public void RegisterGeneratedDocument(string objectKey, string sha256, string verificationCode, Guid actorId, DateTimeOffset changedAt)
    {
        EnsureNotPublished();
        if (Status is not (GazetteStatus.Approved or GazetteStatus.Generated)) throw new GazetteTransitionException("A geração exige uma edição APPROVED.");
        var normalizedHash = sha256.Trim().ToLowerInvariant();
        if (!Sha256Regex().IsMatch(normalizedHash)) throw new ArgumentException("SHA-256 inválido.", nameof(sha256));
        DocumentObjectKey = RequireText(objectKey, nameof(objectKey));
        Sha256 = normalizedHash;
        VerificationCode = RequireText(verificationCode, nameof(verificationCode));
        Status = GazetteStatus.Generated;
        Touch(actorId, changedAt);
    }

    public void RegisterSignature(string certificateSerial, string certificateSubject, string certificateIssuer, DateTimeOffset signedAt, Guid actorId)
    {
        EnsureNotPublished();
        RequireStatus(GazetteStatus.Generated, "SIGNED");
        CertificateSerial = RequireText(certificateSerial, nameof(certificateSerial));
        CertificateSubject = RequireText(certificateSubject, nameof(certificateSubject));
        CertificateIssuer = RequireText(certificateIssuer, nameof(certificateIssuer));
        SignedAt = signedAt;
        Status = GazetteStatus.DigitallySigned;
        Touch(actorId, signedAt);
    }

    public void Publish(Guid actorId, DateTimeOffset publishedAt)
    {
        if (Status != GazetteStatus.DigitallySigned && !IsLegacy) throw new GazetteTransitionException("Uma nova edição precisa estar assinada antes da publicação.");
        if (string.IsNullOrWhiteSpace(DocumentObjectKey) || string.IsNullOrWhiteSpace(Sha256)) throw new GazetteTransitionException("O documento gerado e seu hash são obrigatórios.");
        Status = GazetteStatus.Published;
        PublishedAt = publishedAt;
        Touch(actorId, publishedAt);
    }

    private void EnsureNotPublished()
    {
        if (Status == GazetteStatus.Published) throw new GazetteImmutabilityException("Uma edição publicada é imutável; publique uma correção vinculada.");
    }

    private void RequireStatus(GazetteStatus expected, string target)
    {
        if (Status != expected) throw new GazetteTransitionException($"Transição para {target} exige estado {expected}; estado atual: {Status}.");
    }

    private static string RequireText(string value, string parameterName)
    {
        var normalized = value.Trim();
        if (normalized.Length is 0 or > 500) throw new ArgumentException("O valor é obrigatório e deve ter até 500 caracteres.", parameterName);
        return normalized;
    }

    private void Touch(Guid actorId, DateTimeOffset changedAt)
    {
        if (actorId == Guid.Empty) throw new ArgumentException("O ator é obrigatório.", nameof(actorId));
        UpdatedBy = actorId;
        UpdatedAt = changedAt;
    }

    [GeneratedRegex("^[a-f0-9]{64}$", RegexOptions.CultureInvariant)]
    private static partial Regex Sha256Regex();
}
