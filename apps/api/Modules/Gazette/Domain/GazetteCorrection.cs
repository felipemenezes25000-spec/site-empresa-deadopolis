using MunicipalPlatform.Api.Platform.Tenancy;

namespace MunicipalPlatform.Api.Modules.Gazette.Domain;

public sealed class GazetteCorrection : ITenantEntity
{
    private GazetteCorrection() { }

    public GazetteCorrection(Guid municipalityId, Guid originalEditionId, Guid correctionEditionId, string reason, Guid actorId)
    {
        if (municipalityId == Guid.Empty || originalEditionId == Guid.Empty || correctionEditionId == Guid.Empty || actorId == Guid.Empty)
            throw new ArgumentException("Município, edições e ator são obrigatórios.");
        if (originalEditionId == correctionEditionId)
            throw new ArgumentException("A edição de correção deve ser diferente da edição original.", nameof(correctionEditionId));

        var normalizedReason = reason?.Trim() ?? string.Empty;
        if (normalizedReason.Length is < 10 or > 2_000)
            throw new ArgumentException("A justificativa da correção deve possuir entre 10 e 2.000 caracteres.", nameof(reason));

        Id = Guid.NewGuid();
        MunicipalityId = municipalityId;
        OriginalEditionId = originalEditionId;
        CorrectionEditionId = correctionEditionId;
        Reason = normalizedReason;
        CreatedBy = actorId;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid MunicipalityId { get; private set; }
    public Guid OriginalEditionId { get; private set; }
    public Guid CorrectionEditionId { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public Guid CreatedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
}
