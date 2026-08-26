using MunicipalPlatform.Api.Platform.Tenancy;

namespace MunicipalPlatform.Api.Modules.Services.Domain;

public sealed class ServiceItem : ITenantEntity
{
    private ServiceItem() { }

    public ServiceItem(Guid municipalityId, string name, string slug, string description, string area, string audience, bool isOnline, string? onlineUrl)
    {
        Id = Guid.NewGuid(); MunicipalityId = municipalityId; Name = Require(name, 180); Slug = Require(slug, 180).ToLowerInvariant(); Description = Require(description, 4000); Area = Require(area, 120); Audience = Require(audience, 240); IsOnline = isOnline; OnlineUrl = Optional(onlineUrl, 1000); Status = "PUBLISHED"; LastReviewedAt = DateTimeOffset.UtcNow;
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

    public void Update(ServiceDetails details)
    {
        Name = Require(details.Name, 180); Description = Require(details.Description, 4000); Area = Require(details.Area, 120); Audience = Require(details.Audience, 240); DepartmentId = details.DepartmentId; Requirements = Optional(details.Requirements, 10_000) ?? string.Empty; Documents = Optional(details.Documents, 10_000) ?? string.Empty; Steps = Optional(details.Steps, 10_000) ?? string.Empty; ExpectedDuration = Optional(details.ExpectedDuration, 240) ?? string.Empty; Cost = Optional(details.Cost, 240) ?? "Gratuito"; Channels = Optional(details.Channels, 1000) ?? string.Empty; IsOnline = details.IsOnline; OnlineUrl = Optional(details.OnlineUrl, 1000); Phone = Optional(details.Phone, 120) ?? string.Empty; Address = Optional(details.Address, 500) ?? string.Empty; OpeningHours = Optional(details.OpeningHours, 500) ?? string.Empty; LegalBasis = Optional(details.LegalBasis, 5000) ?? string.Empty; IsFeatured = details.IsFeatured; LastReviewedAt = DateTimeOffset.UtcNow;
    }

    public void SetPublished(bool published) { Status = published ? "PUBLISHED" : "DRAFT"; LastReviewedAt = DateTimeOffset.UtcNow; }

    private static string Require(string value, int max) { if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Campo obrigatório."); var normalized = value.Trim(); if (normalized.Length > max) throw new ArgumentException($"Campo deve possuir até {max} caracteres."); return normalized; }
    private static string? Optional(string? value, int max) { if (string.IsNullOrWhiteSpace(value)) return null; var normalized = value.Trim(); if (normalized.Length > max) throw new ArgumentException($"Campo deve possuir até {max} caracteres."); return normalized; }
}

public sealed record ServiceDetails(string Name, string Description, string Area, string Audience, Guid? DepartmentId, string? Requirements, string? Documents, string? Steps, string? ExpectedDuration, string? Cost, string? Channels, bool IsOnline, string? OnlineUrl, string? Phone, string? Address, string? OpeningHours, string? LegalBasis, bool IsFeatured);
