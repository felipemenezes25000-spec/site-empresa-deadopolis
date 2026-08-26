using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using MunicipalPlatform.Api.Modules.Content.Domain;
using MunicipalPlatform.Api.Modules.Gazette.Domain;
using MunicipalPlatform.Api.Modules.Identity.Domain;
using MunicipalPlatform.Api.Modules.Mail.Domain;
using MunicipalPlatform.Api.Modules.Media.Domain;
using MunicipalPlatform.Api.Modules.Migration.Domain;
using MunicipalPlatform.Api.Modules.Operations.Domain;
using MunicipalPlatform.Api.Modules.Platform.Domain;
using MunicipalPlatform.Api.Modules.Services.Domain;
using MunicipalPlatform.Api.Modules.Support.Domain;
using MunicipalPlatform.Api.Modules.Transparency.Domain;
using MunicipalPlatform.Api.Platform.Tenancy;

namespace MunicipalPlatform.Api.Infrastructure.Persistence;

public sealed class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    TenantContext tenantContext) : DbContext(options)
{
    public DbSet<NewsArticle> NewsArticles => Set<NewsArticle>();
    public DbSet<GazetteEdition> GazetteEditions => Set<GazetteEdition>();
    public DbSet<Municipality> Municipalities => Set<Municipality>();
    public DbSet<UserAccount> Users => Set<UserAccount>();
    public DbSet<RoleCapability> RoleCapabilities => Set<RoleCapability>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<ServiceItem> Services => Set<ServiceItem>();
    public DbSet<TransparencyLink> TransparencyLinks => Set<TransparencyLink>();
    public DbSet<IntegrationStatus> IntegrationStatuses => Set<IntegrationStatus>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
    public DbSet<RedirectRule> RedirectRules => Set<RedirectRule>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<SlaPolicy> SlaPolicies => Set<SlaPolicy>();
    public DbSet<Mailbox> Mailboxes => Set<Mailbox>();
    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();

    private Guid CurrentMunicipalityId => tenantContext.RequireMunicipalityId();

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        EnforceTenantBoundary();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        EnforceTenantBoundary();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<NewsArticle>(entity =>
        {
            entity.ToTable("news_articles");
            entity.HasKey(article => article.Id);
            entity.Property(article => article.Title).HasMaxLength(180);
            entity.Property(article => article.Slug).HasMaxLength(180);
            entity.Property(article => article.Summary).HasMaxLength(320);
            entity.Property(article => article.Body).HasColumnType("text");
            entity.Property(article => article.Status).HasConversion<string>().HasMaxLength(24);
            entity.HasIndex(article => new { article.MunicipalityId, article.Slug }).IsUnique();
            entity.HasIndex(article => new { article.MunicipalityId, article.Status, article.PublishedAt });
        });

        modelBuilder.Entity<GazetteEdition>(entity =>
        {
            entity.ToTable("gazette_editions");
            entity.HasKey(edition => edition.Id);
            entity.Property(edition => edition.Status).HasConversion<string>().HasMaxLength(24);
            entity.Property(edition => edition.Type).HasConversion<string>().HasMaxLength(24);
            entity.Property(edition => edition.Sha256).HasMaxLength(64);
            entity.Property(edition => edition.VerificationCode).HasMaxLength(64);
            entity.HasIndex(edition => new { edition.MunicipalityId, edition.Year, edition.Number }).IsUnique();
            entity.HasIndex(edition => new { edition.MunicipalityId, edition.VerificationCode }).IsUnique();
        });

        modelBuilder.Entity<Municipality>(entity =>
        {
            entity.ToTable("municipalities");
            entity.HasKey(municipality => municipality.Id);
            entity.Property(municipality => municipality.Name).HasMaxLength(160);
            entity.Property(municipality => municipality.Slug).HasMaxLength(100);
            entity.Property(municipality => municipality.StateCode).HasMaxLength(2);
            entity.Property(municipality => municipality.PrimaryColor).HasMaxLength(32);
            entity.HasIndex(municipality => municipality.Slug).IsUnique();
            entity.HasIndex(municipality => municipality.Domain).IsUnique();
        });

        modelBuilder.Entity<UserAccount>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(user => user.Id);
            entity.Property(user => user.Username).HasMaxLength(100);
            entity.Property(user => user.DisplayName).HasMaxLength(160);
            entity.Property(user => user.Role).HasMaxLength(64);
            entity.Property(user => user.PasswordHash).HasMaxLength(512);
            entity.HasIndex(user => new { user.MunicipalityId, user.Username }).IsUnique();
        });

        modelBuilder.Entity<RoleCapability>(entity =>
        {
            entity.ToTable("role_capabilities");
            entity.HasKey(capability => capability.Id);
            entity.Property(capability => capability.Role).HasMaxLength(64);
            entity.Property(capability => capability.Capability).HasMaxLength(100);
            entity.HasIndex(capability => new { capability.MunicipalityId, capability.Role, capability.Capability }).IsUnique();
        });

        modelBuilder.Entity<Department>(entity =>
        {
            entity.ToTable("departments");
            entity.HasKey(department => department.Id);
            entity.HasIndex(department => new { department.MunicipalityId, department.Slug }).IsUnique();
        });

        modelBuilder.Entity<ServiceItem>(entity =>
        {
            entity.ToTable("services");
            entity.HasKey(service => service.Id);
            entity.Property(service => service.Description).HasColumnType("text");
            entity.Property(service => service.Requirements).HasColumnType("text");
            entity.Property(service => service.Documents).HasColumnType("text");
            entity.Property(service => service.Steps).HasColumnType("text");
            entity.HasIndex(service => new { service.MunicipalityId, service.Slug }).IsUnique();
            entity.HasIndex(service => new { service.MunicipalityId, service.Area, service.Status });
        });

        modelBuilder.Entity<TransparencyLink>(entity =>
        {
            entity.ToTable("transparency_links");
            entity.HasKey(link => link.Id);
            entity.HasIndex(link => new { link.MunicipalityId, link.Category, link.DisplayOrder });
        });

        modelBuilder.Entity<IntegrationStatus>(entity =>
        {
            entity.ToTable("integration_statuses");
            entity.HasKey(status => status.Id);
            entity.Property(status => status.State).HasConversion<string>().HasMaxLength(24);
            entity.HasIndex(status => new { status.MunicipalityId, status.Provider }).IsUnique();
        });

        modelBuilder.Entity<AuditEvent>(entity =>
        {
            entity.ToTable("audit_events");
            entity.HasKey(audit => audit.Id);
            entity.Property(audit => audit.SemanticDiff).HasColumnType("jsonb");
            entity.HasIndex(audit => new { audit.MunicipalityId, audit.OccurredAt });
            entity.HasIndex(audit => audit.CorrelationId);
        });

        modelBuilder.Entity<RedirectRule>(entity =>
        {
            entity.ToTable("redirect_rules");
            entity.HasKey(rule => rule.Id);
            entity.HasIndex(rule => new { rule.MunicipalityId, rule.LegacyPath }).IsUnique();
        });

        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.ToTable("tickets");
            entity.HasKey(ticket => ticket.Id);
            entity.Property(ticket => ticket.Priority).HasConversion<string>().HasMaxLength(16);
            entity.Property(ticket => ticket.Description).HasColumnType("text");
            entity.HasIndex(ticket => new { ticket.MunicipalityId, ticket.Protocol }).IsUnique();
            entity.HasIndex(ticket => new { ticket.MunicipalityId, ticket.Status, ticket.ResolutionDueAt });
        });

        modelBuilder.Entity<SlaPolicy>(entity =>
        {
            entity.ToTable("sla_policies");
            entity.HasKey(policy => policy.Id);
            entity.HasIndex(policy => policy.MunicipalityId).IsUnique();
        });

        modelBuilder.Entity<Mailbox>(entity =>
        {
            entity.ToTable("mailboxes");
            entity.HasKey(mailbox => mailbox.Id);
            entity.HasIndex(mailbox => new { mailbox.MunicipalityId, mailbox.Address }).IsUnique();
        });

        modelBuilder.Entity<MediaAsset>(entity =>
        {
            entity.ToTable("media_assets");
            entity.HasKey(asset => asset.Id);
            entity.HasIndex(asset => new { asset.MunicipalityId, asset.Sha256 });
            entity.HasIndex(asset => new { asset.MunicipalityId, asset.Status, asset.UploadedAt });
        });

        ApplyTenantFilters(modelBuilder);
    }

    private void EnforceTenantBoundary()
    {
        var municipalityId = tenantContext.RequireMunicipalityId();
        var invalidEntry = ChangeTracker.Entries<ITenantEntity>()
            .FirstOrDefault(entry =>
                entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted
                && entry.Entity.MunicipalityId != municipalityId);

        if (invalidEntry is not null)
        {
            throw new TenantPersistenceException(
                $"A operação tentou alterar {invalidEntry.Metadata.ClrType.Name} de outro município.");
        }
    }

    private void ApplyTenantFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                     .Where(type => typeof(ITenantEntity).IsAssignableFrom(type.ClrType)))
        {
            var parameter = Expression.Parameter(entityType.ClrType, "entity");
            var municipalityProperty = Expression.Property(parameter, nameof(ITenantEntity.MunicipalityId));
            var contextMunicipality = Expression.Property(
                Expression.Constant(this),
                nameof(CurrentMunicipalityId));
            var body = Expression.Equal(municipalityProperty, contextMunicipality);
            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(Expression.Lambda(body, parameter));
        }
    }
}
