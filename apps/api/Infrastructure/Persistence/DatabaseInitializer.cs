using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MunicipalPlatform.Api.Modules.Content.Domain;
using MunicipalPlatform.Api.Modules.Gazette.Domain;
using MunicipalPlatform.Api.Modules.Gazette.Providers;
using MunicipalPlatform.Api.Modules.Gazette.Services;
using MunicipalPlatform.Api.Modules.Identity.Domain;
using MunicipalPlatform.Api.Modules.Operations.Domain;
using MunicipalPlatform.Api.Modules.Platform.Domain;
using MunicipalPlatform.Api.Modules.Services.Domain;
using MunicipalPlatform.Api.Modules.Support.Domain;
using MunicipalPlatform.Api.Modules.Transparency.Domain;
using MunicipalPlatform.Api.Platform.Storage;
using MunicipalPlatform.Api.Platform.Tenancy;

namespace MunicipalPlatform.Api.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    public static readonly Guid DemoMunicipalityId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid DemoActorId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    public static async Task InitializeAsync(IServiceProvider services, IConfiguration configuration, IHostEnvironment environment, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        if (database.Database.IsRelational()) await database.Database.MigrateAsync(cancellationToken); else await database.Database.EnsureCreatedAsync(cancellationToken);
        if (!configuration.GetValue<bool>("PresentationMode")) return;
        var password = configuration["Demo:Password"];
        if (string.IsNullOrWhiteSpace(password) || password.Length < 14) throw new InvalidOperationException("PresentationMode exige Demo__Password com pelo menos 14 caracteres. O valor nunca deve ser versionado.");
        var tenant = scope.ServiceProvider.GetRequiredService<TenantContext>();
        tenant.SetMunicipality(DemoMunicipalityId, "deodapolis");
        await SeedPresentationDataAsync(database, scope.ServiceProvider.GetRequiredService<IPasswordHasher<UserAccount>>(), password, environment, cancellationToken);
        await SeedPresentationGazetteAsync(
            database,
            scope.ServiceProvider.GetRequiredService<GazetteDocumentService>(),
            scope.ServiceProvider.GetRequiredService<IObjectStorageProvider>(),
            scope.ServiceProvider.GetRequiredService<IDigitalSigner>(),
            scope.ServiceProvider.GetRequiredService<ISignatureValidator>(),
            configuration,
            cancellationToken);
    }

    /// <summary>
    /// Publica uma única edição de demonstração percorrendo o fluxo real do Diário: composição, geração de PDF,
    /// hash SHA-256, assinatura e publicação. Nada é sintetizado fora do domínio — o hash e o código de verificação
    /// são os que o próprio serviço produz, de modo que a verificação pública funciona de verdade na apresentação.
    /// </summary>
    private static async Task SeedPresentationGazetteAsync(ApplicationDbContext database, GazetteDocumentService documents, IObjectStorageProvider storage, IDigitalSigner signer, ISignatureValidator validator, IConfiguration configuration, CancellationToken cancellationToken)
    {
        if (await database.GazetteEditions.AnyAsync(cancellationToken)) return;
        // Sem storage e sem assinador não existe edição verificável; a apresentação continua honestamente vazia.
        if (storage.State == "NOT_CONFIGURED" || signer.State == "NOT_CONFIGURED") return;
        var configured = configuration["PublicPortalBaseUrl"];
        if (string.IsNullOrWhiteSpace(configured) || !Uri.TryCreate(configured.Trim(), UriKind.Absolute, out var baseUri)) return;
        if (baseUri.Scheme != Uri.UriSchemeHttp && baseUri.Scheme != Uri.UriSchemeHttps) return;
        var baseUrl = baseUri.ToString().TrimEnd('/');

        var now = DateTimeOffset.UtcNow;
        var publicationDate = DateOnly.FromDateTime(now.UtcDateTime.AddDays(-1));
        var edition = GazetteEdition.Create(DemoMunicipalityId, 1, publicationDate.Year, GazetteEditionType.Ordinary, publicationDate, DemoActorId);
        var composition = new GazetteComposition([
            new GazetteSectionInput("Secretaria Municipal de Administração", [
                new GazetteActInput("[DEMONSTRAÇÃO] Portaria de designação de equipe de atendimento", "Ato sintético usado exclusivamente para demonstrar composição, geração de PDF, hash e verificação pública. Não possui valor normativo nem efeito jurídico.", "Secretaria Municipal de Administração", "Portaria nº 001/DEMONSTRAÇÃO")]),
            new GazetteSectionInput("Secretaria Municipal de Saúde", [
                new GazetteActInput("[DEMONSTRAÇÃO] Extrato de cronograma de atendimento nas unidades básicas", "Conteúdo sintético de apresentação, sem valor de ato oficial, publicado apenas para validar o fluxo do Diário Oficial Eletrônico.", "Secretaria Municipal de Saúde", "Extrato nº 002/DEMONSTRAÇÃO")])]);
        edition.SetComposition(documents.NormalizeComposition(composition), DemoActorId, now.AddHours(-6));
        edition.SubmitForReview(DemoActorId, now.AddHours(-5));
        edition.Approve(DemoActorId, now.AddHours(-4));

        var document = documents.Generate(edition, baseUrl);
        var objectKey = $"gazette/{edition.Year}/{edition.Number:D5}-{document.Sha256[..12]}.pdf";
        await storage.SaveAsync(objectKey, document.PdfBytes, cancellationToken);
        edition.RegisterGeneratedDocument(objectKey, document.Sha256, document.VerificationCode, DemoActorId, now.AddHours(-3));

        var signature = await signer.SignHashAsync(edition.Sha256!, cancellationToken);
        var validation = await validator.ValidateAsync(edition.Sha256!, signature.SignatureBase64, cancellationToken);
        if (!validation.IsValid) return;
        edition.RegisterSignature(signature.Certificate.Serial, signature.Certificate.Subject, signature.Certificate.Issuer, signature.SignedAt, DemoActorId);
        edition.Publish(DemoActorId, signature.SignedAt);

        database.GazetteEditions.Add(edition);
        database.GazetteSignatures.Add(new GazetteSignature(DemoMunicipalityId, edition.Id, signature.Provider, signature.SignatureBase64, signature.Certificate.Serial, signature.Certificate.Subject, signature.Certificate.Issuer, signature.Certificate.ValidFrom, signature.Certificate.ValidTo, signature.Certificate.IsIcpBrasil, signature.SignedAt, $"VALID:{validation.Provider}:{validation.Detail}"));
        database.GazettePublications.Add(new GazettePublication(DemoMunicipalityId, edition.Id, edition.PublishedAt!.Value, edition.Sha256!, edition.VerificationCode!, $"{baseUrl}/api/v1/gazette/{edition.Id}/document"));
        database.AuditEvents.Add(new AuditEvent(DemoMunicipalityId, null, "presentation.seed.gazette", "GazetteEdition", edition.Id.ToString(), $"{{\"classification\":\"DEMONSTRATION\",\"containsOfficialActs\":false,\"icpBrasil\":{signature.Certificate.IsIcpBrasil.ToString().ToLowerInvariant()}}}", "startup-seed"));
        await database.SaveChangesAsync(cancellationToken);
    }

    public static async Task SeedPresentationDataAsync(ApplicationDbContext database, IPasswordHasher<UserAccount> passwordHasher, string password, IHostEnvironment environment, CancellationToken cancellationToken = default)
    {
        if (!await database.Municipalities.AnyAsync(x => x.Id == DemoMunicipalityId, cancellationToken)) { database.Municipalities.Add(Municipality.Create(DemoMunicipalityId, "Prefeitura Municipal de Deodápolis", "deodapolis", "MS", "#176B4D")); await database.SaveChangesAsync(cancellationToken); }
        if (!await database.Users.AnyAsync(cancellationToken)) AddDemoUsers(database, passwordHasher, password);
        if (!await database.Departments.AnyAsync(cancellationToken)) database.Departments.AddRange(new Department(DemoMunicipalityId, "Secretaria Municipal de Saúde", "saude", "SMS", 1), new Department(DemoMunicipalityId, "Secretaria Municipal de Educação", "educacao", "SEMED", 2), new Department(DemoMunicipalityId, "Secretaria Municipal de Administração", "administracao", "SEMAD", 3));
        if (!await database.Services.AnyAsync(cancellationToken)) database.Services.AddRange(DemoServices());
        if (!await database.TransparencyLinks.AnyAsync(cancellationToken)) database.TransparencyLinks.AddRange(new TransparencyLink(DemoMunicipalityId, "Portal da Transparência", "Controle social", "/transparencia", 1), new TransparencyLink(DemoMunicipalityId, "Acesso à Informação (e-SIC)", "Participação", "/acesso-a-informacao", 2), new TransparencyLink(DemoMunicipalityId, "Dados Abertos", "Dados", "/dados-abertos", 3));
        if (!await database.IntegrationStatuses.AnyAsync(cancellationToken)) { const string message = "Dependência externa sem credencial; integração não está operacional."; database.IntegrationStatuses.AddRange(new IntegrationStatus(DemoMunicipalityId, "Assinatura ICP-Brasil", IntegrationState.NotConfigured, message), new IntegrationStatus(DemoMunicipalityId, "E-mail institucional", IntegrationState.NotConfigured, message), new IntegrationStatus(DemoMunicipalityId, "Storage S3", IntegrationState.NotConfigured, message), new IntegrationStatus(DemoMunicipalityId, "Importação do portal legado", IntegrationState.NotConfigured, message)); }
        var slaPolicy = await database.SlaPolicies.FirstOrDefaultAsync(cancellationToken);
        if (slaPolicy is null) { slaPolicy = SlaPolicy.CreateDefault(DemoMunicipalityId); database.SlaPolicies.Add(slaPolicy); }
        if (!await database.NewsArticles.AnyAsync(cancellationToken)) { var now = DateTimeOffset.UtcNow; database.NewsArticles.Add(CreatePublishedDemoArticle("[DEMONSTRAÇÃO] Prefeitura reúne serviços em atendimento integrado", "demonstracao-atendimento-integrado", "Conteúdo sintético mostra como notícias aprovadas aparecem no portal.", true, now.AddDays(-1))); database.NewsArticles.Add(CreatePublishedDemoArticle("[DEMONSTRAÇÃO] Agenda municipal ganha consulta simplificada", "demonstracao-agenda-municipal", "Exemplo fictício, sem valor de comunicado ou ato oficial.", false, now.AddDays(-3))); database.NewsArticles.AddRange(EditorialPipelineDemoArticles(now)); }
        if (!await database.Tickets.AnyAsync(cancellationToken)) database.Tickets.AddRange(DemoTickets(slaPolicy));
        if (!await database.PortalResources.AnyAsync(cancellationToken)) database.PortalResources.AddRange(DemoResources());
        if (!await database.Datasets.AnyAsync(cancellationToken)) { var dataset = new Dataset(DemoMunicipalityId, "[DEMONSTRAÇÃO] Catálogo de unidades municipais", "demonstracao-unidades-municipais", "Dataset sintético para demonstrar metadados e versionamento. Não representa base oficial.", "Administração", "SEMAD", "Dados de demonstração — sem valor oficial", "Mensal"); database.Datasets.Add(dataset); }
        database.AuditEvents.Add(new AuditEvent(DemoMunicipalityId, null, "presentation.seed", "Platform", environment.EnvironmentName, "{\"classification\":\"DEMONSTRATION\",\"containsOfficialActs\":false}", "startup-seed"));
        await database.SaveChangesAsync(cancellationToken);
    }

    private static void AddDemoUsers(ApplicationDbContext database, IPasswordHasher<UserAccount> passwordHasher, string password)
    {
        var users = new[] { ("admin.demo", "Administração — Demonstração", "SUPER_ADMIN"), ("comunicacao.demo", "Comunicação — Demonstração", "COMMUNICATION"), ("secretaria.demo", "Secretaria — Demonstração", "DEPARTMENT_EDITOR"), ("diario.demo", "Diário Oficial — Demonstração", "GAZETTE_EDITOR"), ("auditor.demo", "Auditoria — Demonstração", "AUDITOR") };
        foreach (var (username, displayName, role) in users) { var subject = new UserAccount(DemoMunicipalityId, username, displayName, role, "pending"); database.Users.Add(new UserAccount(DemoMunicipalityId, username, displayName, role, passwordHasher.HashPassword(subject, password))); }
        AddCapabilities(database, "SUPER_ADMIN", "audit.read", "content.write", "content.review", "content.publish", "resources.manage", "services.manage", "datasets.manage", "gazette.write", "gazette.sign", "gazette.publish", "media.manage", "mail.manage", "migration.manage", "support.write", "changes.manage", "operations.manage", "users.manage", "settings.manage");
        AddCapabilities(database, "COMMUNICATION", "content.write", "content.review", "content.publish", "resources.manage", "media.manage", "datasets.manage");
        AddCapabilities(database, "DEPARTMENT_EDITOR", "content.write");
        AddCapabilities(database, "GAZETTE_EDITOR", "gazette.write", "gazette.sign", "gazette.publish");
        AddCapabilities(database, "AUDITOR", "audit.read");
    }

    private static void AddCapabilities(ApplicationDbContext database, string role, params string[] capabilities) => database.RoleCapabilities.AddRange(capabilities.Select(capability => new RoleCapability(DemoMunicipalityId, role, capability)));

    private static ServiceItem[] DemoServices() => [
        new(DemoMunicipalityId, "Emitir guia do IPTU", "emitir-guia-iptu", "Consulte o caminho para emissão da guia e atendimento tributário.", "Tributos", "Cidadão e empresa", true, "/servicos/emitir-guia-iptu"),
        new(DemoMunicipalityId, "Emitir nota fiscal de serviço", "emitir-nota-fiscal", "Acesse orientações e o sistema externo de nota fiscal eletrônica.", "Empresas", "Empresa e profissional autônomo", true, "/servicos/emitir-nota-fiscal"),
        new(DemoMunicipalityId, "Solicitar matrícula escolar", "matricula-escolar", "Veja períodos, documentos e canais da rede municipal de ensino.", "Educação", "Famílias e estudantes", true, "/servicos/matricula-escolar"),
        new(DemoMunicipalityId, "Encontrar unidade de saúde", "encontrar-unidade-saude", "Localize UBS, contatos e horários de atendimento.", "Saúde", "Toda a população", false, null),
        new(DemoMunicipalityId, "Solicitar poda de árvore", "solicitar-poda-arvore", "Abra uma solicitação e acompanhe o protocolo.", "Meio Ambiente", "Cidadão", true, "/ouvidoria"),
        new(DemoMunicipalityId, "Consultar licitações", "consultar-licitacoes", "Encontre processos e acesse o sistema oficial de licitações.", "Compras públicas", "Cidadão e fornecedor", true, "/licitacoes"),
        new(DemoMunicipalityId, "Consultar Diário Oficial", "consultar-diario-oficial", "Pesquise edições e verifique a autenticidade de documentos.", "Administração", "Toda a população", true, "/diario-oficial"),
        new(DemoMunicipalityId, "Falar com a Ouvidoria", "falar-com-ouvidoria", "Registre solicitação, reclamação, elogio ou denúncia.", "Atendimento", "Toda a população", true, "/ouvidoria")];

    private static PortalResource[] DemoResources()
    {
        var resources = new[]
        {
            new PortalResource(DemoMunicipalityId, "PAGE", "acesso-a-informacao", "Acesso à Informação", "Orientações para pedidos de informação e consulta ao e-SIC.", "{\"sections\":[\"Como solicitar\",\"Prazos\",\"Consulta eletrônica\",\"Consulta presencial\"],\"classification\":\"DEMONSTRATION\"}", 1, DemoActorId),
            new PortalResource(DemoMunicipalityId, "PAGE", "ouvidoria", "Ouvidoria Municipal", "Canal para solicitação, reclamação, denúncia, sugestão e elogio.", "{\"types\":[\"Solicitação\",\"Reclamação\",\"Denúncia\",\"Sugestão\",\"Elogio\"],\"classification\":\"DEMONSTRATION\"}", 2, DemoActorId),
            new PortalResource(DemoMunicipalityId, "DATASET", "catalogo-demonstracao", "Catálogo de dados — demonstração", "Exemplo legado do catálogo genérico preservado durante a migração para Dataset estruturado.", "{\"format\":[\"CSV\",\"JSON\"],\"updateFrequency\":\"Mensal\",\"license\":\"Dados abertos - demonstração\",\"files\":[],\"classification\":\"DEMONSTRATION\"}", 1, DemoActorId),
            new PortalResource(DemoMunicipalityId, "EVENT", "feira-servicos-demo", "[DEMONSTRAÇÃO] Feira de Serviços", "Evento sintético usado somente na apresentação.", "{\"date\":\"2026-09-15\",\"location\":\"Local de demonstração\",\"classification\":\"DEMONSTRATION\"}", 1, DemoActorId),
            new PortalResource(DemoMunicipalityId, "LOCATION", "unidade-saude-demo", "[DEMONSTRAÇÃO] Unidade Municipal", "Registro sintético para demonstrar o diretório de locais.", "{\"category\":\"UBS\",\"address\":\"Endereço de demonstração\",\"hours\":\"7h às 17h\",\"classification\":\"DEMONSTRATION\"}", 1, DemoActorId),
            new PortalResource(DemoMunicipalityId, "LEGISLATION", "legislacao-municipal", "Legislação Municipal", "Hub para pesquisa e direcionamento ao acervo legislativo.", "{\"externalSystemState\":\"NOT_CONFIGURED\",\"classification\":\"DEMONSTRATION\"}", 1, DemoActorId)
        };
        foreach (var resource in resources) resource.Publish(DemoActorId, DateTimeOffset.UtcNow.AddDays(-1));
        return resources;
    }

    /// <summary>
    /// Deixa uma pauta em cada estágio do fluxo editorial para que o painel executivo mostre trabalho real em
    /// andamento em vez de três contadores zerados. São registros de demonstração, não comunicados oficiais.
    /// </summary>
    private static NewsArticle[] EditorialPipelineDemoArticles(DateTimeOffset now)
    {
        var draft = NewsArticle.Create(DemoMunicipalityId, "[DEMONSTRAÇÃO] Mutirão de limpeza urbana nos bairros", "demonstracao-mutirao-limpeza-urbana", DemoActorId);
        draft.UpdateDraft("[DEMONSTRAÇÃO] Mutirão de limpeza urbana nos bairros", "Rascunho sintético usado para demonstrar a etapa de redação do fluxo editorial.", "Conteúdo sintético sem valor de comunicado oficial, mantido em rascunho para a apresentação.", null, null, "MEIO_AMBIENTE", false, DemoActorId, now.AddHours(-9));

        var review = NewsArticle.Create(DemoMunicipalityId, "[DEMONSTRAÇÃO] Calendário de matrículas da rede municipal", "demonstracao-calendario-matriculas", DemoActorId);
        review.UpdateDraft("[DEMONSTRAÇÃO] Calendário de matrículas da rede municipal", "Pauta sintética aguardando revisão editorial na apresentação.", "Conteúdo sintético sem valor de comunicado oficial, mantido em revisão para a apresentação.", null, null, "EDUCACAO", false, DemoActorId, now.AddHours(-8));
        review.SubmitForReview(DemoActorId, now.AddHours(-7));

        var scheduled = NewsArticle.Create(DemoMunicipalityId, "[DEMONSTRAÇÃO] Campanha de vacinação nas unidades básicas", "demonstracao-campanha-vacinacao", DemoActorId);
        scheduled.UpdateDraft("[DEMONSTRAÇÃO] Campanha de vacinação nas unidades básicas", "Pauta sintética já aprovada e agendada para publicação automática.", "Conteúdo sintético sem valor de comunicado oficial, agendado para demonstrar a publicação programada.", null, null, "SAUDE", false, DemoActorId, now.AddHours(-6));
        scheduled.SubmitForReview(DemoActorId, now.AddHours(-5));
        scheduled.Approve(DemoActorId, now.AddHours(-4));
        scheduled.Schedule(now.AddDays(7), DemoActorId, now.AddHours(-4));

        return [draft, review, scheduled];
    }

    /// <summary>
    /// Manifestações sintéticas em estados reais do atendimento. Os prazos saem da própria política de SLA, de modo
    /// que nenhum indicador de violação é fabricado — a demonstração começa dentro do prazo, como deve ser.
    /// </summary>
    private static Ticket[] DemoTickets(SlaPolicy policy)
    {
        var now = DateTimeOffset.UtcNow;
        var open = new Ticket(DemoMunicipalityId, "2026-000131", "Cidadã de demonstração", "contato.demo@exemplo.invalid", "Solicitação", TicketPriority.Normal, "[DEMONSTRAÇÃO] Solicitação de poda de árvore em via pública. Registro sintético para a apresentação.", policy.CalculateDeadlines(TicketPriority.Normal, now));
        var inProgress = new Ticket(DemoMunicipalityId, "2026-000132", "Cidadão de demonstração", "contato.demo@exemplo.invalid", "Reclamação", TicketPriority.High, "[DEMONSTRAÇÃO] Reclamação sobre iluminação pública intermitente. Registro sintético para a apresentação.", policy.CalculateDeadlines(TicketPriority.High, now));
        inProgress.RecordResponse(now.AddHours(-2));
        var resolved = new Ticket(DemoMunicipalityId, "2026-000133", "Cidadã de demonstração", "contato.demo@exemplo.invalid", "Elogio", TicketPriority.Low, "[DEMONSTRAÇÃO] Elogio ao atendimento presencial na unidade de saúde. Registro sintético para a apresentação.", policy.CalculateDeadlines(TicketPriority.Low, now));
        resolved.Resolve(now.AddHours(-1));
        return [open, inProgress, resolved];
    }

    private static NewsArticle CreatePublishedDemoArticle(string title, string slug, string summary, bool featured, DateTimeOffset publishedAt)
    {
        var article = NewsArticle.Create(DemoMunicipalityId, title, slug, DemoActorId);
        article.UpdateDraft(title, summary, "Este é um conteúdo sintético usado exclusivamente para demonstrar o fluxo editorial da plataforma.", null, null, "GERAL", featured, DemoActorId, publishedAt.AddMinutes(-30));
        article.SubmitForReview(DemoActorId, publishedAt.AddMinutes(-20)); article.Approve(DemoActorId, publishedAt.AddMinutes(-10)); article.Publish(DemoActorId, publishedAt);
        return article;
    }
}
