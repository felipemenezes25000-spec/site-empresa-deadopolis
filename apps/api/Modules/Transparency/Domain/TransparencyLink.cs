using MunicipalPlatform.Api.Platform.Tenancy;

namespace MunicipalPlatform.Api.Modules.Transparency.Domain;

public sealed class TransparencyLink : ITenantEntity
{
    private TransparencyLink()
    {
    }

    public TransparencyLink(Guid municipalityId, string title, string category, string url, int displayOrder)
    {
        Id = Guid.NewGuid();
        MunicipalityId = municipalityId;
        Title = title.Trim();
        Category = category.Trim();
        Url = url.Trim();
        DisplayOrder = displayOrder;
    }

    public Guid Id { get; private set; }
    public Guid MunicipalityId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Category { get; private set; } = string.Empty;
    public string Url { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public int DisplayOrder { get; private set; }
    public bool IsExternal { get; private set; } = true;
    public bool IsActive { get; private set; } = true;
}
