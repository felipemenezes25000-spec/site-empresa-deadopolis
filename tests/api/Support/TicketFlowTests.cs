using MunicipalPlatform.Api.Modules.Support.Domain;

namespace MunicipalPlatform.Api.Tests.Support;

public sealed class TicketFlowTests
{
    [Fact]
    public void Response_and_resolution_record_sla_timestamps()
    {
        var now = DateTimeOffset.UtcNow; var ticket = new Ticket(Guid.NewGuid(), "DEO-TEST", "Pessoa", "contato", "Suporte", TicketPriority.Normal, "Descrição suficientemente longa para teste.", new SlaDeadlines(now.AddHours(8), now.AddHours(40))); ticket.RecordResponse(now.AddMinutes(2)); Assert.Equal("IN_PROGRESS", ticket.Status); Assert.NotNull(ticket.FirstResponseAt); ticket.Resolve(now.AddMinutes(5)); Assert.Equal("RESOLVED", ticket.Status); Assert.NotNull(ticket.ResolvedAt);
    }
}
