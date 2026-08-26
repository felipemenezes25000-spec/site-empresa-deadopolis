using MunicipalPlatform.Api.Platform.Tenancy;

namespace MunicipalPlatform.Api.Modules.Support.Domain;

public sealed class TicketComment : ITenantEntity
{
    private TicketComment() { }
    public TicketComment(Guid municipalityId, Guid ticketId, Guid authorId, string body, bool isInternal) { if (string.IsNullOrWhiteSpace(body) || body.Trim().Length > 8000) throw new ArgumentException("Comentário deve possuir entre 1 e 8.000 caracteres."); Id = Guid.NewGuid(); MunicipalityId = municipalityId; TicketId = ticketId; AuthorId = authorId; Body = body.Trim(); IsInternal = isInternal; CreatedAt = DateTimeOffset.UtcNow; }
    public Guid Id { get; private set; }
    public Guid MunicipalityId { get; private set; }
    public Guid TicketId { get; private set; }
    public Guid AuthorId { get; private set; }
    public string Body { get; private set; } = string.Empty;
    public bool IsInternal { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
}
