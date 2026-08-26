using MunicipalPlatform.Api.Platform.Tenancy;

namespace MunicipalPlatform.Api.Modules.Support.Domain;

public sealed class SlaPolicy : ITenantEntity
{
    private SlaPolicy(Guid municipalityId)
    {
        Id = Guid.NewGuid();
        MunicipalityId = municipalityId;
    }

    public Guid Id { get; private set; }
    public Guid MunicipalityId { get; private set; }

    public static SlaPolicy CreateDefault(Guid municipalityId)
    {
        if (municipalityId == Guid.Empty)
        {
            throw new ArgumentException("O município é obrigatório.", nameof(municipalityId));
        }

        return new SlaPolicy(municipalityId);
    }

    public SlaDeadlines CalculateDeadlines(TicketPriority priority, DateTimeOffset openedAt)
    {
        var (firstResponseHours, resolutionHours) = priority switch
        {
            TicketPriority.Critical => (1, 4),
            TicketPriority.High => (4, 16),
            TicketPriority.Normal => (8, 40),
            TicketPriority.Low => (16, 80),
            _ => throw new ArgumentOutOfRangeException(nameof(priority))
        };

        return new SlaDeadlines(
            openedAt.AddHours(firstResponseHours),
            openedAt.AddHours(resolutionHours));
    }
}
