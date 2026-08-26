using MunicipalPlatform.Api.Platform.Tenancy;

namespace MunicipalPlatform.Api.Modules.Services.Domain;

public sealed class ServiceItem : ITenantEntity
{
    private ServiceItem()
    {
    }

    public ServiceItem(
        Guid municipalityId,
        string name,
        string slug,
        string description,
        string area,
        string audience,
        bool isOnline,
        string? onlineUrl)
    {
        Id = Guid.NewGuid();
        MunicipalityId = municipalityId;
        Name = name.Trim();
        Slug = slug.Trim().ToLowerInvariant();
        Description = description.Trim();
        Area = area.Trim();
        Audience = audience.Trim();
        IsOnline = isOnline;
        OnlineUrl = string.IsNullOrWhiteSpace(onlineUrl) ? null : onlineUrl.Trim();
        Status = "PUBLISHED";
        LastReviewedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid MunicipalityId { get; private set; }
    public Guid? DepartmentId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Area { get; private set; } = string.Empty;
    public string Audience { get; private set; } = string.Empty;
    public string Requirements { get; private set; } = string.Empty;
    public string Documents { get; private set; } = string.Empty;
    public string Steps { get; private set; } = string.Empty;
    public string ExpectedDuration { get; private set; } = string.Empty;
    public string Cost { get; private set; } = "Gratuito";
    public string Channels { get; private set; } = string.Empty;
    public bool IsOnline { get; private set; }
    public string? OnlineUrl { get; private set; }
    public string Phone { get; private set; } = string.Empty;
    public string Address { get; private set; } = string.Empty;
    public string OpeningHours { get; private set; } = string.Empty;
    public string LegalBasis { get; private set; } = string.Empty;
    public string Status { get; private set; } = string.Empty;
    public bool IsFeatured { get; private set; }
    public DateTimeOffset LastReviewedAt { get; private set; }
}
