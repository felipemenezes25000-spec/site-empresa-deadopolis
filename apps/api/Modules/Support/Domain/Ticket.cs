using System.Security.Cryptography;
using MunicipalPlatform.Api.Platform.Tenancy;

namespace MunicipalPlatform.Api.Modules.Support.Domain;

public sealed class Ticket : ITenantEntity
{
    private Ticket() { }
    public Ticket(Guid municipalityId, string protocol, string requesterName, string contact, string category, TicketPriority priority, string description, SlaDeadlines deadlines) { Id = Guid.NewGuid(); MunicipalityId = municipalityId; Protocol = protocol.Trim(); RequesterName = requesterName.Trim(); Contact = contact.Trim(); Category = category.Trim(); Priority = priority; Description = description.Trim(); Status = "OPEN"; OpenedAt = DateTimeOffset.UtcNow; FirstResponseDueAt = deadlines.FirstResponseDueAt; ResolutionDueAt = deadlines.ResolutionDueAt; PrivacyConsentAt = DateTimeOffset.UtcNow; TrackingCode = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant(); }
    public Guid Id { get; private set; }
    public Guid MunicipalityId { get; private set; }
    public string Protocol { get; private set; } = string.Empty;
    public string RequesterName { get; private set; } = string.Empty;
    public string Contact { get; private set; } = string.Empty;
    public string TrackingCode { get; private set; } = string.Empty;
    public string Category { get; private set; } = string.Empty;
    public TicketPriority Priority { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public string Status { get; private set; } = string.Empty;
    public DateTimeOffset OpenedAt { get; private set; }
    public DateTimeOffset FirstResponseDueAt { get; private set; }
    public DateTimeOffset ResolutionDueAt { get; private set; }
    public DateTimeOffset? FirstResponseAt { get; private set; }
    public DateTimeOffset? ResolvedAt { get; private set; }
    public DateTimeOffset PrivacyConsentAt { get; private set; }
    public void SetPriority(TicketPriority priority, SlaDeadlines deadlines) { Priority = priority; FirstResponseDueAt = deadlines.FirstResponseDueAt; ResolutionDueAt = deadlines.ResolutionDueAt; }
    public void RecordResponse(DateTimeOffset at) { FirstResponseAt ??= at; if (Status == "OPEN") Status = "IN_PROGRESS"; }
    public void Resolve(DateTimeOffset at) { FirstResponseAt ??= at; ResolvedAt = at; Status = "RESOLVED"; }
    public void Reopen() { ResolvedAt = null; Status = "OPEN"; }
}
