using MunicipalPlatform.Api.Platform.Tenancy;

namespace MunicipalPlatform.Api.Modules.Transparency.Domain;

public enum DatasetStatus { Draft, Published, Archived }

public sealed class Dataset : ITenantEntity
{
    private Dataset() { }
    public Dataset(Guid municipalityId, string title, string slug, string description, string category, string responsibleDepartment, string license, string updateFrequency)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Título obrigatório.", nameof(title));
        if (string.IsNullOrWhiteSpace(slug)) throw new ArgumentException("Slug obrigatório.", nameof(slug));
        Id = Guid.NewGuid(); MunicipalityId = municipalityId; Title = title.Trim(); Slug = slug.Trim().ToLowerInvariant(); Description = description.Trim(); Category = category.Trim(); ResponsibleDepartment = responsibleDepartment.Trim(); License = license.Trim(); UpdateFrequency = updateFrequency.Trim(); CreatedAt = UpdatedAt = DateTimeOffset.UtcNow;
    }
    public Guid Id { get; private set; }
    public Guid MunicipalityId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Category { get; private set; } = string.Empty;
    public string ResponsibleDepartment { get; private set; } = string.Empty;
    public string License { get; private set; } = string.Empty;
    public string UpdateFrequency { get; private set; } = string.Empty;
    public string? ReferencePeriod { get; private set; }
    public DateTimeOffset? LastUpdatedAt { get; private set; }
    public DateTimeOffset? NextExpectedUpdateAt { get; private set; }
    public DatasetStatus Status { get; private set; } = DatasetStatus.Draft;
    public string? Source { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? PublishedAt { get; private set; }
    public void UpdateMetadata(string title,string description,string category,string responsibleDepartment,string license,string updateFrequency,string? referencePeriod,string? source,DateTimeOffset? nextExpectedUpdateAt){if(Status==DatasetStatus.Archived)throw new InvalidOperationException("Dataset arquivado não pode ser editado.");Title=title.Trim();Description=description.Trim();Category=category.Trim();ResponsibleDepartment=responsibleDepartment.Trim();License=license.Trim();UpdateFrequency=updateFrequency.Trim();ReferencePeriod=referencePeriod?.Trim();Source=source?.Trim();NextExpectedUpdateAt=nextExpectedUpdateAt;UpdatedAt=DateTimeOffset.UtcNow;}
    public void Publish(DateTimeOffset now){if(Status==DatasetStatus.Archived)throw new InvalidOperationException("Dataset arquivado não pode ser publicado.");Status=DatasetStatus.Published;PublishedAt??=now;LastUpdatedAt=now;UpdatedAt=now;}
    public void MarkVersionPublished(DateTimeOffset now){LastUpdatedAt=now;UpdatedAt=now;}
    public void Archive(DateTimeOffset now){Status=DatasetStatus.Archived;UpdatedAt=now;}
}
