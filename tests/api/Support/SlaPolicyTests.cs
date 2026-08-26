using MunicipalPlatform.Api.Modules.Support.Domain;

namespace MunicipalPlatform.Api.Tests.Support;

public sealed class SlaPolicyTests
{
    [Theory]
    [InlineData(TicketPriority.Critical, 1, 4)]
    [InlineData(TicketPriority.High, 4, 16)]
    [InlineData(TicketPriority.Normal, 8, 40)]
    [InlineData(TicketPriority.Low, 16, 80)]
    public void CalculateDeadlinesUsesPriorityDurations(
        TicketPriority priority,
        int expectedFirstResponseHours,
        int expectedResolutionHours)
    {
        var policy = SlaPolicy.CreateDefault(Guid.NewGuid());
        var openedAt = new DateTimeOffset(2026, 8, 25, 12, 0, 0, TimeSpan.Zero);

        var deadlines = policy.CalculateDeadlines(priority, openedAt);

        Assert.Equal(openedAt.AddHours(expectedFirstResponseHours), deadlines.FirstResponseDueAt);
        Assert.Equal(openedAt.AddHours(expectedResolutionHours), deadlines.ResolutionDueAt);
    }
}
