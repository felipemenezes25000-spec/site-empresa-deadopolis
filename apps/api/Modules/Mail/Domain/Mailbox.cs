using MunicipalPlatform.Api.Platform.Tenancy;

namespace MunicipalPlatform.Api.Modules.Mail.Domain;

public sealed class Mailbox : ITenantEntity
{
    private Mailbox()
    {
    }

    public Mailbox(Guid municipalityId, string address, string displayName, int quotaMegabytes)
    {
        Id = Guid.NewGuid();
        MunicipalityId = municipalityId;
        Address = address.Trim().ToLowerInvariant();
        DisplayName = displayName.Trim();
        QuotaMegabytes = quotaMegabytes;
        Status = "NOT_CONFIGURED";
    }

    public Guid Id { get; private set; }
    public Guid MunicipalityId { get; private set; }
    public string Address { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public int QuotaMegabytes { get; private set; }
    public string Status { get; private set; } = string.Empty;
    public string? ExternalId { get; private set; }
}
