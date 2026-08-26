using MunicipalPlatform.Api.Platform.Tenancy;

namespace MunicipalPlatform.Api.Modules.Services.Domain;

public sealed class Department : ITenantEntity
{
    private Department() { }
    public Department(Guid municipalityId, string name, string slug, string acronym, int displayOrder) { Id = Guid.NewGuid(); MunicipalityId = municipalityId; Name = name.Trim(); Slug = slug.Trim().ToLowerInvariant(); Acronym = acronym.Trim().ToUpperInvariant(); DisplayOrder = displayOrder; }
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
    public void Update(string name, string acronym, string managerName, string phone, string email, string address, string openingHours, int displayOrder) { Name = Required(name, 180); Acronym = Required(acronym, 32).ToUpperInvariant(); ManagerName = Optional(managerName, 180); Phone = Optional(phone, 80); Email = Optional(email, 180); Address = Optional(address, 500); OpeningHours = Optional(openingHours, 500); DisplayOrder = displayOrder; }
    public void SetActive(bool active) => IsActive = active;
    private static string Required(string value, int max) { if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Campo obrigatório."); var n = value.Trim(); if (n.Length > max) throw new ArgumentException($"Máximo de {max} caracteres."); return n; }
    private static string Optional(string? value, int max) { if (string.IsNullOrWhiteSpace(value)) return string.Empty; var n = value.Trim(); if (n.Length > max) throw new ArgumentException($"Máximo de {max} caracteres."); return n; }
}
