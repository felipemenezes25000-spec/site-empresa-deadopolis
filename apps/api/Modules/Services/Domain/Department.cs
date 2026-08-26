using MunicipalPlatform.Api.Platform.Tenancy;

namespace MunicipalPlatform.Api.Modules.Services.Domain;

public sealed class Department : ITenantEntity
{
    private Department()
    {
    }

    public Department(Guid municipalityId, string name, string slug, string acronym, int displayOrder)
    {
        Id = Guid.NewGuid();
        MunicipalityId = municipalityId;
        Name = name.Trim();
        Slug = slug.Trim().ToLowerInvariant();
        Acronym = acronym.Trim().ToUpperInvariant();
        DisplayOrder = displayOrder;
    }

    public Guid Id { get; private set; }
    public Guid MunicipalityId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string Acronym { get; private set; } = string.Empty;
    public string ManagerName { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string Address { get; private set; } = string.Empty;
    public string OpeningHours { get; private set; } = string.Empty;
    public int DisplayOrder { get; private set; }
    public bool IsActive { get; private set; } = true;
}
