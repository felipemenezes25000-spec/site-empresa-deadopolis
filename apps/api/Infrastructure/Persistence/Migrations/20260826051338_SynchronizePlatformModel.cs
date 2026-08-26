using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MunicipalPlatform.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SynchronizePlatformModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "backup_evidence",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MunicipalityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "text", nullable: false),
                    BackupType = table.Column<string>(type: "text", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Reference = table.Column<string>(type: "text", nullable: true),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    RestoreTestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Error = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_backup_evidence", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "change_requests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MunicipalityId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequesterId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    BusinessReason = table.Column<string>(type: "text", nullable: false),
                    Impact = table.Column<string>(type: "text", nullable: false),
                    Priority = table.Column<string>(type: "text", nullable: false),
                    State = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    Decision = table.Column<string>(type: "text", nullable: true),
                    PlannedRelease = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_change_requests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "changelog_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MunicipalityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<string>(type: "text", nullable: false),
                    ReleaseDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Summary = table.Column<string>(type: "text", nullable: false),
                    Details = table.Column<string>(type: "text", nullable: false),
                    Impact = table.Column<string>(type: "text", nullable: false),
                    Audience = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_changelog_entries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "dataset_versions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MunicipalityId = table.Column<Guid>(type: "uuid", nullable: false),
                    DatasetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    ObjectKey = table.Column<string>(type: "text", nullable: false),
                    MimeType = table.Column<string>(type: "text", nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Sha256 = table.Column<string>(type: "text", nullable: false),
                    Format = table.Column<string>(type: "text", nullable: false),
                    MetadataJson = table.Column<string>(type: "jsonb", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dataset_versions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "datasets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MunicipalityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Slug = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    ResponsibleDepartment = table.Column<string>(type: "text", nullable: false),
                    License = table.Column<string>(type: "text", nullable: false),
                    UpdateFrequency = table.Column<string>(type: "text", nullable: false),
                    ReferencePeriod = table.Column<string>(type: "text", nullable: true),
                    LastUpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    NextExpectedUpdateAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    Source = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_datasets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "gazette_acts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MunicipalityId = table.Column<Guid>(type: "uuid", nullable: false),
                    GazetteEditionId = table.Column<Guid>(type: "uuid", nullable: false),
                    GazetteSectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActType = table.Column<string>(type: "text", nullable: false),
                    Number = table.Column<string>(type: "text", nullable: true),
                    Year = table.Column<int>(type: "integer", nullable: true),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    Issuer = table.Column<string>(type: "text", nullable: false),
                    ActDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gazette_acts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "gazette_attachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MunicipalityId = table.Column<Guid>(type: "uuid", nullable: false),
                    GazetteEditionId = table.Column<Guid>(type: "uuid", nullable: false),
                    GazetteActId = table.Column<Guid>(type: "uuid", nullable: true),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    ObjectKey = table.Column<string>(type: "text", nullable: false),
                    MimeType = table.Column<string>(type: "text", nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Sha256 = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gazette_attachments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "gazette_corrections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MunicipalityId = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginalEditionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CorrectionEditionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gazette_corrections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "gazette_publications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MunicipalityId = table.Column<Guid>(type: "uuid", nullable: false),
                    GazetteEditionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Sha256 = table.Column<string>(type: "text", nullable: false),
                    VerificationCode = table.Column<string>(type: "text", nullable: false),
                    PublicUrl = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gazette_publications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "gazette_sections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MunicipalityId = table.Column<Guid>(type: "uuid", nullable: false),
                    GazetteEditionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gazette_sections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "gazette_signatures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MunicipalityId = table.Column<Guid>(type: "uuid", nullable: false),
                    GazetteEditionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "text", nullable: false),
                    SignatureBase64 = table.Column<string>(type: "text", nullable: false),
                    CertificateSerial = table.Column<string>(type: "text", nullable: false),
                    CertificateSubject = table.Column<string>(type: "text", nullable: false),
                    CertificateIssuer = table.Column<string>(type: "text", nullable: false),
                    CertificateValidFrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CertificateValidTo = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsIcpBrasil = table.Column<bool>(type: "boolean", nullable: false),
                    SignedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ValidationState = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gazette_signatures", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "imported_contents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MunicipalityId = table.Column<Guid>(type: "uuid", nullable: false),
                    MigrationJobId = table.Column<Guid>(type: "uuid", nullable: false),
                    LegacyUrlId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetType = table.Column<string>(type: "text", nullable: false),
                    TargetReference = table.Column<string>(type: "text", nullable: false),
                    SourceSha256 = table.Column<string>(type: "text", nullable: false),
                    EvidenceJson = table.Column<string>(type: "jsonb", nullable: false),
                    ImportedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_imported_contents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "legacy_urls",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MunicipalityId = table.Column<Guid>(type: "uuid", nullable: false),
                    MigrationJobId = table.Column<Guid>(type: "uuid", nullable: false),
                    Url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    NormalizedPath = table.Column<string>(type: "text", nullable: false),
                    Depth = table.Column<int>(type: "integer", nullable: false),
                    ContentType = table.Column<string>(type: "text", nullable: true),
                    ContentLength = table.Column<long>(type: "bigint", nullable: true),
                    Sha256 = table.Column<string>(type: "text", nullable: true),
                    Classification = table.Column<string>(type: "text", nullable: false),
                    State = table.Column<string>(type: "text", nullable: false),
                    FailureReason = table.Column<string>(type: "text", nullable: true),
                    DiscoveredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_legacy_urls", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "link_checks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MunicipalityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    StatusCode = table.Column<int>(type: "integer", nullable: true),
                    State = table.Column<string>(type: "text", nullable: false),
                    CheckedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LatencyMilliseconds = table.Column<long>(type: "bigint", nullable: true),
                    FailureReason = table.Column<string>(type: "text", nullable: true),
                    ConsecutiveFailures = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_link_checks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "mail_aliases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MunicipalityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Address = table.Column<string>(type: "text", nullable: false),
                    TargetAddress = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mail_aliases", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "mail_domains",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MunicipalityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Domain = table.Column<string>(type: "text", nullable: false),
                    State = table.Column<string>(type: "text", nullable: false),
                    ExternalId = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mail_domains", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "mail_migration_jobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MunicipalityId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceType = table.Column<string>(type: "text", nullable: false),
                    SourceReference = table.Column<string>(type: "text", nullable: false),
                    TargetAddress = table.Column<string>(type: "text", nullable: false),
                    State = table.Column<string>(type: "text", nullable: false),
                    ImportedMessages = table.Column<int>(type: "integer", nullable: false),
                    FailedMessages = table.Column<int>(type: "integer", nullable: false),
                    LastError = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mail_migration_jobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "migration_evidence",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MunicipalityId = table.Column<Guid>(type: "uuid", nullable: false),
                    MigrationJobId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "text", nullable: false),
                    Reference = table.Column<string>(type: "text", nullable: false),
                    PayloadJson = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_migration_evidence", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "migration_jobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MunicipalityId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceBaseUrl = table.Column<string>(type: "text", nullable: false),
                    AllowedHost = table.Column<string>(type: "text", nullable: false),
                    MaxDepth = table.Column<int>(type: "integer", nullable: false),
                    MaxPages = table.Column<int>(type: "integer", nullable: false),
                    State = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DiscoveredCount = table.Column<int>(type: "integer", nullable: false),
                    ImportedCount = table.Column<int>(type: "integer", nullable: false),
                    FailedCount = table.Column<int>(type: "integer", nullable: false),
                    LastError = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_migration_jobs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_backup_evidence_MunicipalityId_StartedAt",
                table: "backup_evidence",
                columns: new[] { "MunicipalityId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_change_requests_MunicipalityId_State_UpdatedAt",
                table: "change_requests",
                columns: new[] { "MunicipalityId", "State", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_changelog_entries_MunicipalityId_ReleaseDate",
                table: "changelog_entries",
                columns: new[] { "MunicipalityId", "ReleaseDate" });

            migrationBuilder.CreateIndex(
                name: "IX_dataset_versions_MunicipalityId_DatasetId_Version",
                table: "dataset_versions",
                columns: new[] { "MunicipalityId", "DatasetId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_dataset_versions_MunicipalityId_Sha256",
                table: "dataset_versions",
                columns: new[] { "MunicipalityId", "Sha256" });

            migrationBuilder.CreateIndex(
                name: "IX_datasets_MunicipalityId_Slug",
                table: "datasets",
                columns: new[] { "MunicipalityId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_datasets_MunicipalityId_Status_LastUpdatedAt",
                table: "datasets",
                columns: new[] { "MunicipalityId", "Status", "LastUpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_gazette_acts_MunicipalityId_GazetteEditionId_GazetteSection~",
                table: "gazette_acts",
                columns: new[] { "MunicipalityId", "GazetteEditionId", "GazetteSectionId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_gazette_attachments_MunicipalityId_GazetteEditionId",
                table: "gazette_attachments",
                columns: new[] { "MunicipalityId", "GazetteEditionId" });

            migrationBuilder.CreateIndex(
                name: "IX_gazette_corrections_MunicipalityId_OriginalEditionId_Correc~",
                table: "gazette_corrections",
                columns: new[] { "MunicipalityId", "OriginalEditionId", "CorrectionEditionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_gazette_publications_MunicipalityId_GazetteEditionId",
                table: "gazette_publications",
                columns: new[] { "MunicipalityId", "GazetteEditionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_gazette_publications_MunicipalityId_VerificationCode",
                table: "gazette_publications",
                columns: new[] { "MunicipalityId", "VerificationCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_gazette_sections_MunicipalityId_GazetteEditionId_DisplayOrd~",
                table: "gazette_sections",
                columns: new[] { "MunicipalityId", "GazetteEditionId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_gazette_signatures_MunicipalityId_GazetteEditionId_SignedAt",
                table: "gazette_signatures",
                columns: new[] { "MunicipalityId", "GazetteEditionId", "SignedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_imported_contents_MunicipalityId_MigrationJobId_LegacyUrlId",
                table: "imported_contents",
                columns: new[] { "MunicipalityId", "MigrationJobId", "LegacyUrlId" });

            migrationBuilder.CreateIndex(
                name: "IX_legacy_urls_MunicipalityId_MigrationJobId_NormalizedPath",
                table: "legacy_urls",
                columns: new[] { "MunicipalityId", "MigrationJobId", "NormalizedPath" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_link_checks_MunicipalityId_State_CheckedAt",
                table: "link_checks",
                columns: new[] { "MunicipalityId", "State", "CheckedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_link_checks_MunicipalityId_Url",
                table: "link_checks",
                columns: new[] { "MunicipalityId", "Url" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_mail_aliases_MunicipalityId_Address",
                table: "mail_aliases",
                columns: new[] { "MunicipalityId", "Address" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_mail_domains_MunicipalityId_Domain",
                table: "mail_domains",
                columns: new[] { "MunicipalityId", "Domain" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_mail_migration_jobs_MunicipalityId_State_UpdatedAt",
                table: "mail_migration_jobs",
                columns: new[] { "MunicipalityId", "State", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_migration_evidence_MunicipalityId_MigrationJobId_CreatedAt",
                table: "migration_evidence",
                columns: new[] { "MunicipalityId", "MigrationJobId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_migration_jobs_MunicipalityId_State_UpdatedAt",
                table: "migration_jobs",
                columns: new[] { "MunicipalityId", "State", "UpdatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "backup_evidence");

            migrationBuilder.DropTable(
                name: "change_requests");

            migrationBuilder.DropTable(
                name: "changelog_entries");

            migrationBuilder.DropTable(
                name: "dataset_versions");

            migrationBuilder.DropTable(
                name: "datasets");

            migrationBuilder.DropTable(
                name: "gazette_acts");

            migrationBuilder.DropTable(
                name: "gazette_attachments");

            migrationBuilder.DropTable(
                name: "gazette_corrections");

            migrationBuilder.DropTable(
                name: "gazette_publications");

            migrationBuilder.DropTable(
                name: "gazette_sections");

            migrationBuilder.DropTable(
                name: "gazette_signatures");

            migrationBuilder.DropTable(
                name: "imported_contents");

            migrationBuilder.DropTable(
                name: "legacy_urls");

            migrationBuilder.DropTable(
                name: "link_checks");

            migrationBuilder.DropTable(
                name: "mail_aliases");

            migrationBuilder.DropTable(
                name: "mail_domains");

            migrationBuilder.DropTable(
                name: "mail_migration_jobs");

            migrationBuilder.DropTable(
                name: "migration_evidence");

            migrationBuilder.DropTable(
                name: "migration_jobs");
        }
    }
}
