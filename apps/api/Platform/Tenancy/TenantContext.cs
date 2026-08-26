namespace MunicipalPlatform.Api.Platform.Tenancy;

public sealed class TenantContext
{
    private Guid? _municipalityId;

    public string? MunicipalitySlug { get; private set; }

    public void SetMunicipality(Guid municipalityId, string municipalitySlug)
    {
        if (municipalityId == Guid.Empty)
        {
            throw new TenantResolutionException("O município informado é inválido.");
        }

        if (_municipalityId.HasValue && _municipalityId.Value != municipalityId)
        {
            throw new TenantResolutionException("O município da requisição não pode ser alterado.");
        }

        var normalizedSlug = municipalitySlug.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalizedSlug))
        {
            throw new TenantResolutionException("O identificador do município é obrigatório.");
        }

        _municipalityId = municipalityId;
        MunicipalitySlug = normalizedSlug;
    }

    public Guid RequireMunicipalityId() =>
        _municipalityId ?? throw new TenantResolutionException(
            "O município não foi identificado para esta requisição.");
}
