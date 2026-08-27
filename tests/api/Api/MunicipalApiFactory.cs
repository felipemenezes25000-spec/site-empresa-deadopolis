using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MunicipalPlatform.Api.Infrastructure.Persistence;
using MunicipalPlatform.Api.Modules.Content.Domain;
using MunicipalPlatform.Api.Modules.Identity.Domain;
using MunicipalPlatform.Api.Modules.Operations.Domain;
using MunicipalPlatform.Api.Modules.Platform.Domain;
using MunicipalPlatform.Api.Modules.Services.Domain;
using MunicipalPlatform.Api.Modules.Transparency.Domain;
using MunicipalPlatform.Api.Platform.Tenancy;

namespace MunicipalPlatform.Api.Tests.Api;

public sealed class MunicipalApiFactory : WebApplicationFactory<Program>
{
    public static readonly Guid MunicipalityId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly string _databaseName = $"api-contract-{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("DefaultMunicipalitySlug", "deodapolis");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<ApplicationDbContext>();
            services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(_databaseName));
        });
    }

    public async Task SeedAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var tenant = scope.ServiceProvider.GetRequiredService<TenantContext>();
        tenant.SetMunicipality(MunicipalApiFactory.MunicipalityId, "deodapolis");
        var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await database.Database.EnsureCreatedAsync();
        if (await database.Municipalities.AnyAsync()) return;

        var actor = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        database.Municipalities.Add(Municipality.Create(MunicipalityId, "Prefeitura Municipal de Deodápolis", "deodapolis", "MS", "#176B4D"));
        database.Services.Add(new ServiceItem(MunicipalityId, "Emitir guia do IPTU", "emitir-guia-iptu", "Emita ou consulte a guia do IPTU no sistema tributário municipal.", "Tributos", "Cidadão", true, "https://example.test/iptu"));
        database.TransparencyLinks.Add(new TransparencyLink(MunicipalityId, "Portal da Transparência", "Transparência", "https://example.test/transparencia", 1));
        database.IntegrationStatuses.Add(new IntegrationStatus(MunicipalityId, "Email institucional", IntegrationState.NotConfigured, "Credencial do provedor ainda não configurada."));
        var temporaryUser = new UserAccount(MunicipalityId, "admin.demo", "Administração Demo", "SUPER_ADMIN", "pending");
        var passwordHash = new PasswordHasher<UserAccount>().HashPassword(temporaryUser, "Demo-Local-2026!");
        database.Users.Add(new UserAccount(MunicipalityId, "admin.demo", "Administração Demo", "SUPER_ADMIN", passwordHash));
        database.RoleCapabilities.AddRange(
            new RoleCapability(MunicipalityId, "SUPER_ADMIN", "audit.read"),
            new RoleCapability(MunicipalityId, "SUPER_ADMIN", "content.write"),
            new RoleCapability(MunicipalityId, "SUPER_ADMIN", "content.review"),
            new RoleCapability(MunicipalityId, "SUPER_ADMIN", "content.publish"),
            new RoleCapability(MunicipalityId, "SUPER_ADMIN", "gazette.write"),
            new RoleCapability(MunicipalityId, "SUPER_ADMIN", "gazette.sign"),
            new RoleCapability(MunicipalityId, "SUPER_ADMIN", "gazette.publish"),
            new RoleCapability(MunicipalityId, "SUPER_ADMIN", "media.manage"),
            new RoleCapability(MunicipalityId, "SUPER_ADMIN", "mail.manage"),
            new RoleCapability(MunicipalityId, "SUPER_ADMIN", "support.write"),
            new RoleCapability(MunicipalityId, "SUPER_ADMIN", "settings.manage"),
            new RoleCapability(MunicipalityId, "SUPER_ADMIN", "users.manage"),
            new RoleCapability(MunicipalityId, "COMMUNICATION", "content.write"));
        var article = NewsArticle.Create(MunicipalityId, "Feira de serviços aproxima Prefeitura e moradores", "feira-de-servicos", actor);
        article.UpdateDraft("Feira de serviços aproxima Prefeitura e moradores", "Atendimentos municipais reunidos em um único local.", "Conteúdo sintético de demonstração.", null, null, "PREFEITURA", true, actor, DateTimeOffset.UtcNow);
        article.SubmitForReview(actor, DateTimeOffset.UtcNow); article.Approve(actor, DateTimeOffset.UtcNow); article.Publish(actor, DateTimeOffset.UtcNow);
        database.NewsArticles.Add(article);
        await database.SaveChangesAsync();

        await using var verificationScope = Services.CreateAsyncScope();
        var verificationTenant = verificationScope.ServiceProvider.GetRequiredService<TenantContext>();
        verificationTenant.SetMunicipality(MunicipalityId, "deodapolis");
        var verificationDatabase = verificationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        if (await verificationDatabase.Municipalities.CountAsync() != 1) throw new InvalidOperationException("O seed não foi compartilhado entre escopos.");
    }
}
