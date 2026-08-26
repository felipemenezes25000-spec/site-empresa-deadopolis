using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MunicipalPlatform.Api.Modules.Content.Domain;
using MunicipalPlatform.Api.Modules.Identity.Domain;
using MunicipalPlatform.Api.Modules.Operations.Domain;
using MunicipalPlatform.Api.Modules.Platform.Domain;
using MunicipalPlatform.Api.Modules.Services.Domain;
using MunicipalPlatform.Api.Modules.Support.Domain;
using MunicipalPlatform.Api.Modules.Transparency.Domain;
using MunicipalPlatform.Api.Platform.Tenancy;

namespace MunicipalPlatform.Api.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    public static readonly Guid DemoMunicipalityId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid DemoActorId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    public static async Task InitializeAsync(
        IServiceProvider services,
        IConfiguration configuration,
        IHostEnvironment environment,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        if (database.Database.IsRelational())
        {
            await database.Database.MigrateAsync(cancellationToken);
        }
        else
        {
            await database.Database.EnsureCreatedAsync(cancellationToken);
        }

        if (!configuration.GetValue<bool>("PresentationMode"))
        {
            return;
        }

        var password = configuration["Demo:Password"];
        if (string.IsNullOrWhiteSpace(password) || password.Length < 14)
        {
            throw new InvalidOperationException(
                "PresentationMode exige Demo__Password com pelo menos 14 caracteres. O valor nunca deve ser versionado.");
        }

        var tenant = scope.ServiceProvider.GetRequiredService<TenantContext>();
        tenant.SetMunicipality(DemoMunicipalityId, "deodapolis");
        await SeedPresentationDataAsync(
            database,
            scope.ServiceProvider.GetRequiredService<IPasswordHasher<UserAccount>>(),
            password,
            environment,
            cancellationToken);
    }

    public static async Task SeedPresentationDataAsync(
        ApplicationDbContext database,
        IPasswordHasher<UserAccount> passwordHasher,
        string password,
        IHostEnvironment environment,
        CancellationToken cancellationToken = default)
    {
        if (!await database.Municipalities.AnyAsync(item => item.Id == DemoMunicipalityId, cancellationToken))
        {
            database.Municipalities.Add(Municipality.Create(
                DemoMunicipalityId,
                "Prefeitura Municipal de Deodápolis",
                "deodapolis",
                "MS",
                "#176B4D"));
            await database.SaveChangesAsync(cancellationToken);
        }

        if (!await database.Users.AnyAsync(cancellationToken))
        {
            AddDemoUsers(database, passwordHasher, password);
        }

        if (!await database.Services.AnyAsync(cancellationToken))
        {
            database.Services.AddRange(DemoServices());
        }

        if (!await database.TransparencyLinks.AnyAsync(cancellationToken))
        {
            database.TransparencyLinks.AddRange(
                new TransparencyLink(DemoMunicipalityId, "Portal da Transparência", "Controle social", "/transparencia", 1),
                new TransparencyLink(DemoMunicipalityId, "Acesso à Informação (e-SIC)", "Participação", "/acesso-a-informacao", 2),
                new TransparencyLink(DemoMunicipalityId, "Dados Abertos", "Dados", "/dados-abertos", 3));
        }

        if (!await database.IntegrationStatuses.AnyAsync(cancellationToken))
        {
            const string message = "Dependência externa sem credencial; integração não está operacional.";
            database.IntegrationStatuses.AddRange(
                new IntegrationStatus(DemoMunicipalityId, "Assinatura ICP-Brasil", IntegrationState.NotConfigured, message),
                new IntegrationStatus(DemoMunicipalityId, "E-mail institucional", IntegrationState.NotConfigured, message),
                new IntegrationStatus(DemoMunicipalityId, "Storage S3", IntegrationState.NotConfigured, message),
                new IntegrationStatus(DemoMunicipalityId, "Importação do portal legado", IntegrationState.NotConfigured, message));
        }

        if (!await database.SlaPolicies.AnyAsync(cancellationToken))
        {
            database.SlaPolicies.Add(SlaPolicy.CreateDefault(DemoMunicipalityId));
        }

        if (!await database.NewsArticles.AnyAsync(cancellationToken))
        {
            var now = DateTimeOffset.UtcNow;
            database.NewsArticles.Add(CreatePublishedDemoArticle(
                "[DEMONSTRAÇÃO] Prefeitura reúne serviços em atendimento integrado",
                "demonstracao-atendimento-integrado",
                "Conteúdo sintético mostra como notícias aprovadas aparecem no portal.",
                true,
                now.AddDays(-1)));
            database.NewsArticles.Add(CreatePublishedDemoArticle(
                "[DEMONSTRAÇÃO] Agenda municipal ganha consulta simplificada",
                "demonstracao-agenda-municipal",
                "Exemplo fictício, sem valor de comunicado ou ato oficial.",
                false,
                now.AddDays(-3)));
        }

        database.AuditEvents.Add(new AuditEvent(
            DemoMunicipalityId,
            null,
            "presentation.seed",
            "Platform",
            environment.EnvironmentName,
            "{\"classification\":\"DEMONSTRATION\",\"containsOfficialActs\":false}",
            "startup-seed"));
        await database.SaveChangesAsync(cancellationToken);
    }

    private static void AddDemoUsers(
        ApplicationDbContext database,
        IPasswordHasher<UserAccount> passwordHasher,
        string password)
    {
        var users = new[]
        {
            ("admin.demo", "Administração — Demonstração", "SUPER_ADMIN"),
            ("comunicacao.demo", "Comunicação — Demonstração", "COMMUNICATION"),
            ("secretaria.demo", "Secretaria — Demonstração", "DEPARTMENT_EDITOR"),
            ("diario.demo", "Diário Oficial — Demonstração", "GAZETTE_EDITOR"),
            ("auditor.demo", "Auditoria — Demonstração", "AUDITOR")
        };

        foreach (var (username, displayName, role) in users)
        {
            var hashSubject = new UserAccount(DemoMunicipalityId, username, displayName, role, "pending");
            var hash = passwordHasher.HashPassword(hashSubject, password);
            database.Users.Add(new UserAccount(DemoMunicipalityId, username, displayName, role, hash));
        }

        AddCapabilities(database, "SUPER_ADMIN", "audit.read", "content.write", "content.review", "content.publish", "gazette.write", "gazette.publish", "support.write", "users.manage", "settings.manage");
        AddCapabilities(database, "COMMUNICATION", "content.write", "content.review", "content.publish", "media.write");
        AddCapabilities(database, "DEPARTMENT_EDITOR", "content.write");
        AddCapabilities(database, "GAZETTE_EDITOR", "gazette.write", "gazette.publish");
        AddCapabilities(database, "AUDITOR", "audit.read");
    }

    private static void AddCapabilities(ApplicationDbContext database, string role, params string[] capabilities)
    {
        database.RoleCapabilities.AddRange(capabilities.Select(capability =>
            new RoleCapability(DemoMunicipalityId, role, capability)));
    }

    private static ServiceItem[] DemoServices() => new[]
    {
        new ServiceItem(DemoMunicipalityId, "Emitir guia do IPTU", "emitir-guia-iptu", "Consulte o caminho para emissão da guia e atendimento tributário.", "Tributos", "Cidadão e empresa", true, "/servicos/emitir-guia-iptu"),
        new ServiceItem(DemoMunicipalityId, "Emitir nota fiscal de serviço", "emitir-nota-fiscal", "Acesse orientações e o sistema externo de nota fiscal eletrônica.", "Empresas", "Empresa e profissional autônomo", true, "/servicos/emitir-nota-fiscal"),
        new ServiceItem(DemoMunicipalityId, "Solicitar matrícula escolar", "matricula-escolar", "Veja períodos, documentos e canais da rede municipal de ensino.", "Educação", "Famílias e estudantes", true, "/servicos/matricula-escolar"),
        new ServiceItem(DemoMunicipalityId, "Encontrar unidade de saúde", "encontrar-unidade-saude", "Localize UBS, contatos e horários de atendimento.", "Saúde", "Toda a população", false, null),
        new ServiceItem(DemoMunicipalityId, "Solicitar poda de árvore", "solicitar-poda-arvore", "Abra uma solicitação e acompanhe o protocolo.", "Meio Ambiente", "Cidadão", true, "/ouvidoria"),
        new ServiceItem(DemoMunicipalityId, "Consultar licitações", "consultar-licitacoes", "Encontre processos e acesse o sistema oficial de licitações.", "Compras públicas", "Cidadão e fornecedor", true, "/licitacoes"),
        new ServiceItem(DemoMunicipalityId, "Consultar Diário Oficial", "consultar-diario-oficial", "Pesquise edições e verifique a autenticidade de documentos.", "Administração", "Toda a população", true, "/diario-oficial"),
        new ServiceItem(DemoMunicipalityId, "Falar com a Ouvidoria", "falar-com-ouvidoria", "Registre solicitação, reclamação, elogio ou denúncia.", "Atendimento", "Toda a população", true, "/ouvidoria")
    };

    private static NewsArticle CreatePublishedDemoArticle(
        string title,
        string slug,
        string summary,
        bool featured,
        DateTimeOffset publishedAt)
    {
        var article = NewsArticle.Create(DemoMunicipalityId, title, slug, DemoActorId);
        article.UpdateDraft(
            title,
            summary,
            "Este é um conteúdo sintético usado exclusivamente para demonstrar o fluxo editorial da plataforma.",
            null,
            null,
            featured,
            DemoActorId,
            publishedAt.AddMinutes(-30));
        article.SubmitForReview(DemoActorId, publishedAt.AddMinutes(-20));
        article.Approve(DemoActorId, publishedAt.AddMinutes(-10));
        article.Publish(DemoActorId, publishedAt);
        return article;
    }
}
