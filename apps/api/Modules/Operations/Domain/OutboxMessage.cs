using MunicipalPlatform.Api.Platform.Tenancy;

namespace MunicipalPlatform.Api.Modules.Operations.Domain;

public sealed class OutboxMessage : ITenantEntity
{
    private OutboxMessage() { }

    public OutboxMessage(Guid municipalityId, string type, string payloadJson)
    {
        Id = Guid.NewGuid();
        MunicipalityId = municipalityId;
        Type = type.Trim();
        PayloadJson = payloadJson;
        OccurredAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid MunicipalityId { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public string PayloadJson { get; private set; } = "{}";
    public DateTimeOffset OccurredAt { get; private set; }
    public DateTimeOffset? ProcessedAt { get; private set; }
    public int Attempts { get; private set; }

    public void MarkProcessed(DateTimeOffset at) => ProcessedAt = at;
    public void RecordAttempt() => Attempts++;
}
