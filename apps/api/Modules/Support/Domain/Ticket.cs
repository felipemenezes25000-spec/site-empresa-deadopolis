using MunicipalPlatform.Api.Platform.Tenancy;

namespace MunicipalPlatform.Api.Modules.Support.Domain;

public sealed class Ticket : ITenantEntity
{
    private Ticket()
    {
    }

    public Ticket(
        Guid municipalityId,
        string protocol,
        string requesterName,
        string category,
        TicketPriority priority,
        string description,
        SlaDeadlines deadlines)
    {
        Id = Guid.NewGuid();
        MunicipalityId = municipalityId;
        Protocol = protocol.Trim();
        RequesterName = requesterName.Trim();
        Category = category.Trim();
        Priority = priority;
        Description = description.Trim();
        Status = "OPEN";
        OpenedAt = DateTimeOffset.UtcNow;
        FirstResponseDueAt = deadlines.FirstResponseDueAt;
        ResolutionDueAt = deadlines.ResolutionDueAt;
    }

    public Guid Id { get; private set; }
    public Guid MunicipalityId { get; private set; }
    public string Protocol { get; private set; } = string.Empty;
    public string RequesterName { get; private set; } = string.Empty;
    public string Category { get; private set; } = string.Empty;
    public TicketPriority Priority { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public string Status { get; private set; } = string.Empty;
    public DateTimeOffset OpenedAt { get; private set; }
    public DateTimeOffset FirstResponseDueAt { get; private set; }
    public DateTimeOffset ResolutionDueAt { get; private set; }
    public DateTimeOffset? FirstResponseAt { get; private set; }
    public DateTimeOffset? ResolvedAt { get; private set; }
}
