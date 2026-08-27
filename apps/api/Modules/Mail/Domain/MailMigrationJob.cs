using MunicipalPlatform.Api.Platform.Tenancy;

namespace MunicipalPlatform.Api.Modules.Mail.Domain;

public sealed class MailMigrationJob : ITenantEntity
{
    private MailMigrationJob() { }

    public MailMigrationJob(Guid municipalityId, string sourceType, string sourceReference, string targetAddress)
    {
        if (municipalityId == Guid.Empty) throw new ArgumentException("Município obrigatório.", nameof(municipalityId));
        Id = Guid.NewGuid();
        MunicipalityId = municipalityId;
        SourceType = Require(sourceType, 16).ToUpperInvariant();
        SourceReference = Require(sourceReference, 500);
        TargetAddress = Require(targetAddress, 320).ToLowerInvariant();
        State = "CREATED";
        CreatedAt = UpdatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid MunicipalityId { get; private set; }
    public string SourceType { get; private set; } = string.Empty;
    public string SourceReference { get; private set; } = string.Empty;
    public string TargetAddress { get; private set; } = string.Empty;
    public string State { get; private set; } = string.Empty;
    public int CandidateMessages { get; private set; }
    public int ImportedMessages { get; private set; }
    public int FailedMessages { get; private set; }
    public long SourceBytes { get; private set; }
    public string? SourceSha256 { get; private set; }
    public DateTimeOffset? InspectedAt { get; private set; }
    public string? LastError { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public void RecordLocalInspection(
        int candidateMessages,
        int invalidMessages,
        long sourceBytes,
        string sourceSha256,
        string? warning,
        DateTimeOffset inspectedAt)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(candidateMessages);
        ArgumentOutOfRangeException.ThrowIfNegative(invalidMessages);
        ArgumentOutOfRangeException.ThrowIfNegative(sourceBytes);
        var normalizedSha = Require(sourceSha256, 64).ToLowerInvariant();
        if (normalizedSha.Length != 64 || normalizedSha.Any(character => !Uri.IsHexDigit(character)))
            throw new ArgumentException("SHA-256 inválido.", nameof(sourceSha256));

        CandidateMessages = candidateMessages;
        FailedMessages = invalidMessages;
        SourceBytes = sourceBytes;
        SourceSha256 = normalizedSha;
        InspectedAt = inspectedAt;
        State = candidateMessages > 0 ? "VALIDATED_LOCAL" : "LOCAL_VALIDATION_FAILED";
        LastError = NormalizeOptional(warning, 2_000);
        UpdatedAt = inspectedAt;
    }

    public void UpdateProgress(string state, int imported, int failed, string? error)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(imported);
        ArgumentOutOfRangeException.ThrowIfNegative(failed);
        State = Require(state, 40).ToUpperInvariant();
        ImportedMessages = imported;
        FailedMessages = failed;
        LastError = NormalizeOptional(error, 2_000);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static string Require(string value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Campo obrigatório.");
        var normalized = value.Trim();
        if (normalized.Length > maxLength) throw new ArgumentException($"Campo deve possuir até {maxLength} caracteres.");
        return normalized;
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim();
        return normalized.Length <= maxLength ? normalized : normalized[..maxLength];
    }
}
