namespace MunicipalPlatform.Api.Modules.Platform.Domain;

public sealed class Municipality
{
    private Municipality()
    {
    }

    private Municipality(Guid id, string name, string slug, string stateCode, string primaryColor)
    {
        Id = id;
        Name = name;
        Slug = slug;
        StateCode = stateCode;
        PrimaryColor = primaryColor;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string StateCode { get; private set; } = string.Empty;
    public string PrimaryColor { get; private set; } = string.Empty;
    public string? LogoObjectKey { get; private set; }
    public string? Domain { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAt { get; private set; }

    public static Municipality Create(
        Guid id,
        string name,
        string slug,
        string stateCode,
        string primaryColor) =>
        new(id, name.Trim(), slug.Trim().ToLowerInvariant(), stateCode.Trim().ToUpperInvariant(), primaryColor.Trim());
}
