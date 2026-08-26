using MunicipalPlatform.Api.Modules.Content.Domain;

namespace MunicipalPlatform.Api.Tests.Content;

public sealed class PortalResourceTests
{
    [Fact]
    public void Update_requires_expected_version_to_be_checked_by_application_and_increments_version()
    {
        var actor = Guid.NewGuid();
        var resource = new PortalResource(Guid.NewGuid(), "PAGE", "sobre", "Sobre", "Resumo", "{}", 1, actor);
        var initialVersion = resource.Version;
        resource.Update("Sobre a Prefeitura", "Resumo atualizado", "{\"section\":1}", 2, null, null, actor, DateTimeOffset.UtcNow);
        Assert.Equal(initialVersion + 1, resource.Version);
        resource.Publish(actor, DateTimeOffset.UtcNow);
        Assert.Equal("PUBLISHED", resource.Status);
    }
}
