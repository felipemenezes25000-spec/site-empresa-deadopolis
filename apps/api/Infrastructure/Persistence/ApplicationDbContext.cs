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

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, TenantContext tenantContext) : DbContext(options)
{
    public DbSet<NewsArticle> NewsArticles => Set<NewsArticle>();
    public DbSet<PortalResource> PortalResources => Set<PortalResource>();
    public DbSet<ContentRevision> ContentRevisions => Set<ContentRevision>();
    public DbSet<GazetteEdition> GazetteEditions => Set<GazetteEdition>();
    public DbSet<Municipality> Municipalities => Set<Municipality>();
    public DbSet<UserAccount> Users => Set<UserAccount>();
    public DbSet<RoleCapability> RoleCapabilities => Set<RoleCapability>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<ServiceItem> Services => Set<ServiceItem>();
    public DbSet<TransparencyLink> TransparencyLinks => Set<TransparencyLink>();
    public DbSet<IntegrationStatus> IntegrationStatuses => Set<IntegrationStatus>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<RedirectRule> RedirectRules => Set<RedirectRule>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<SlaPolicy> SlaPolicies => Set<SlaPolicy>();
    public DbSet<Mailbox> Mailboxes => Set<Mailbox>();
    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();
    private Guid CurrentMunicipalityId => tenantContext.RequireMunicipalityId();

    public override int SaveChanges(bool acceptAllChangesOnSuccess) { EnforceTenantBoundary(); return base.SaveChanges(acceptAllChangesOnSuccess); }
    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default) { EnforceTenantBoundary(); return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken); }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<NewsArticle>(entity => { entity.ToTable("news_articles"); entity.HasKey(x => x.Id); entity.Property(x => x.Title).HasMaxLength(180); entity.Property(x => x.Slug).HasMaxLength(180); entity.Property(x => x.Summary).HasMaxLength(320); entity.Property(x => x.Body).HasColumnType("text"); entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(24); entity.HasIndex(x => new { x.MunicipalityId, x.Slug }).IsUnique(); entity.HasIndex(x => new { x.MunicipalityId, x.Status, x.PublishedAt }); });
        modelBuilder.Entity<PortalResource>(entity => { entity.ToTable("portal_resources"); entity.HasKey(x => x.Id); entity.Property(x => x.Kind).HasMaxLength(32); entity.Property(x => x.Slug).HasMaxLength(180); entity.Property(x => x.Title).HasMaxLength(220); entity.Property(x => x.Summary).HasMaxLength(500); entity.Property(x => x.PayloadJson).HasColumnType("jsonb"); entity.Property(x => x.Status).HasMaxLength(24); entity.HasIndex(x => new { x.MunicipalityId, x.Kind, x.Slug }).IsUnique(); entity.HasIndex(x => new { x.MunicipalityId, x.Kind, x.Status, x.DisplayOrder }); });
        modelBuilder.Entity<ContentRevision>(entity => { entity.ToTable("content_revisions"); entity.HasKey(x => x.Id); entity.Property(x => x.ResourceKind).HasMaxLength(32); entity.Property(x => x.SnapshotJson).HasColumnType("jsonb"); entity.HasIndex(x => new { x.MunicipalityId, x.ResourceKind, x.ResourceId, x.CreatedAt }); });
        modelBuilder.Entity<GazetteEdition>(entity => { entity.ToTable("gazette_editions"); entity.HasKey(x => x.Id); entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(24); entity.Property(x => x.Type).HasConversion<string>().HasMaxLength(24); entity.Property(x => x.CompositionJson).HasColumnType("text"); entity.Property(x => x.Sha256).HasMaxLength(64); entity.Property(x => x.VerificationCode).HasMaxLength(64); entity.HasIndex(x => new { x.MunicipalityId, x.Year, x.Number }).IsUnique(); entity.HasIndex(x => new { x.MunicipalityId, x.VerificationCode }).IsUnique(); });
        modelBuilder.Entity<Municipality>(entity => { entity.ToTable("municipalities"); entity.HasKey(x => x.Id); entity.Property(x => x.Name).HasMaxLength(160); entity.Property(x => x.Slug).HasMaxLength(100); entity.Property(x => x.StateCode).HasMaxLength(2); entity.Property(x => x.PrimaryColor).HasMaxLength(32); entity.HasIndex(x => x.Slug).IsUnique(); entity.HasIndex(x => x.Domain).IsUnique(); });
        modelBuilder.Entity<UserAccount>(entity => { entity.ToTable("users"); entity.HasKey(x => x.Id); entity.Property(x => x.Username).HasMaxLength(100); entity.Property(x => x.DisplayName).HasMaxLength(160); entity.Property(x => x.Role).HasMaxLength(64); entity.Property(x => x.PasswordHash).HasMaxLength(512); entity.HasIndex(x => new { x.MunicipalityId, x.Username }).IsUnique(); });
        modelBuilder.Entity<RoleCapability>(entity => { entity.ToTable("role_capabilities"); entity.HasKey(x => x.Id); entity.Property(x => x.Role).HasMaxLength(64); entity.Property(x => x.Capability).HasMaxLength(100); entity.HasIndex(x => new { x.MunicipalityId, x.Role, x.Capability }).IsUnique(); });
        modelBuilder.Entity<Department>(entity => { entity.ToTable("departments"); entity.HasKey(x => x.Id); entity.HasIndex(x => new { x.MunicipalityId, x.Slug }).IsUnique(); });
        modelBuilder.Entity<ServiceItem>(entity => { entity.ToTable("services"); entity.HasKey(x => x.Id); entity.Property(x => x.Description).HasColumnType("text"); entity.Property(x => x.Requirements).HasColumnType("text"); entity.Property(x => x.Documents).HasColumnType("text"); entity.Property(x => x.Steps).HasColumnType("text"); entity.HasIndex(x => new { x.MunicipalityId, x.Slug }).IsUnique(); entity.HasIndex(x => new { x.MunicipalityId, x.Area, x.Status }); });
        modelBuilder.Entity<TransparencyLink>(entity => { entity.ToTable("transparency_links"); entity.HasKey(x => x.Id); entity.HasIndex(x => new { x.MunicipalityId, x.Category, x.DisplayOrder }); });
        modelBuilder.Entity<IntegrationStatus>(entity => { entity.ToTable("integration_statuses"); entity.HasKey(x => x.Id); entity.Property(x => x.State).HasConversion<string>().HasMaxLength(24); entity.HasIndex(x => new { x.MunicipalityId, x.Provider }).IsUnique(); });
        modelBuilder.Entity<AuditEvent>(entity => { entity.ToTable("audit_events"); entity.HasKey(x => x.Id); entity.Property(x => x.SemanticDiff).HasColumnType("jsonb"); entity.HasIndex(x => new { x.MunicipalityId, x.OccurredAt }); entity.HasIndex(x => x.CorrelationId); });
        modelBuilder.Entity<OutboxMessage>(entity => { entity.ToTable("outbox_messages"); entity.HasKey(x => x.Id); entity.Property(x => x.Type).HasMaxLength(120); entity.Property(x => x.PayloadJson).HasColumnType("jsonb"); entity.HasIndex(x => new { x.MunicipalityId, x.ProcessedAt, x.OccurredAt }); });
        modelBuilder.Entity<RedirectRule>(entity => { entity.ToTable("redirect_rules"); entity.HasKey(x => x.Id); entity.HasIndex(x => new { x.MunicipalityId, x.LegacyPath }).IsUnique(); });
        modelBuilder.Entity<Ticket>(entity => { entity.ToTable("tickets"); entity.HasKey(x => x.Id); entity.Property(x => x.Priority).HasConversion<string>().HasMaxLength(16); entity.Property(x => x.Description).HasColumnType("text"); entity.HasIndex(x => new { x.MunicipalityId, x.Protocol }).IsUnique(); entity.HasIndex(x => new { x.MunicipalityId, x.Status, x.ResolutionDueAt }); });
        modelBuilder.Entity<SlaPolicy>(entity => { entity.ToTable("sla_policies"); entity.HasKey(x => x.Id); entity.HasIndex(x => x.MunicipalityId).IsUnique(); });
        modelBuilder.Entity<Mailbox>(entity => { entity.ToTable("mailboxes"); entity.HasKey(x => x.Id); entity.HasIndex(x => new { x.MunicipalityId, x.Address }).IsUnique(); });
        modelBuilder.Entity<MediaAsset>(entity => { entity.ToTable("media_assets"); entity.HasKey(x => x.Id); entity.HasIndex(x => new { x.MunicipalityId, x.Sha256 }); entity.HasIndex(x => new { x.MunicipalityId, x.Status, x.UploadedAt }); });
        ApplyTenantFilters(modelBuilder);
    }

    private void EnforceTenantBoundary()
    {
        var municipalityId = tenantContext.RequireMunicipalityId();
        var invalidEntry = ChangeTracker.Entries<ITenantEntity>().FirstOrDefault(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted && entry.Entity.MunicipalityId != municipalityId);
        if (invalidEntry is not null) throw new TenantPersistenceException($"A operação tentou alterar {invalidEntry.Metadata.ClrType.Name} de outro município.");
    }

    private void ApplyTenantFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes().Where(type => typeof(ITenantEntity).IsAssignableFrom(type.ClrType)))
        {
            var parameter = Expression.Parameter(entityType.ClrType, "entity");
            var municipalityProperty = Expression.Property(parameter, nameof(ITenantEntity.MunicipalityId));
            var contextMunicipality = Expression.Property(Expression.Constant(this), nameof(CurrentMunicipalityId));
            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(Expression.Lambda(Expression.Equal(municipalityProperty, contextMunicipality), parameter));
        }
    }
}
