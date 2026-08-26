using MunicipalPlatform.Api.Platform.Tenancy;

namespace MunicipalPlatform.Api.Tests.Tenancy;

public sealed class TenantContextTests
{
    [Fact]
    public void RequireMunicipalityIdThrowsWhenTenantWasNotResolved()
    {
        var context = new TenantContext();

        var error = Assert.Throws<TenantResolutionException>(
            () => context.RequireMunicipalityId());

        Assert.Equal("O município não foi identificado para esta requisição.", error.Message);
    }

    [Fact]
    public void SetMunicipalityRejectsChangingTenantDuringSameRequest()
    {
        var context = new TenantContext();
        var deodapolis = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var anotherMunicipality = Guid.Parse("22222222-2222-2222-2222-222222222222");

        context.SetMunicipality(deodapolis, "deodapolis");

        var error = Assert.Throws<TenantResolutionException>(
            () => context.SetMunicipality(anotherMunicipality, "outro-municipio"));

        Assert.Equal("O município da requisição não pode ser alterado.", error.Message);
        Assert.Equal(deodapolis, context.RequireMunicipalityId());
    }

    [Fact]
    public void SetMunicipalityStoresNormalizedSlugAndId()
    {
        var context = new TenantContext();
        var municipalityId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        context.SetMunicipality(municipalityId, "  Deodapolis  ");

        Assert.Equal(municipalityId, context.RequireMunicipalityId());
        Assert.Equal("deodapolis", context.MunicipalitySlug);
    }
}
