using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MunicipalPlatform.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialPlatform : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "audit_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MunicipalityId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: true),
                    Action = table.Column<string>(type: "text", nullable: false),
                    Resource = table.Column<string>(type: "text", nullable: false),
                    ResourceId = table.Column<string>(type: "text", nullable: false),
                    SemanticDiff = table.Column<string>(type: "jsonb", nullable: false),
                    CorrelationId = table.Column<string>(type: "text", nullable: false),
                    IpAddressHash = table.Column<string>(type: "text", nullable: true),
                    UserAgent = table.Column<string>(type: "text", nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_events", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "departments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MunicipalityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Slug = table.Column<string>(type: "text", nullable: false),
                    Acronym = table.Column<string>(type: "text", nullable: false),
                    ManagerName = table.Column<string>(type: "text", nullable: false),
                    Phone = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Address = table.Column<string>(type: "text", nullable: false),
                    OpeningHours = table.Column<string>(type: "text", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_departments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "gazette_editions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MunicipalityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<int>(type: "integer", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    PublicationDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    IsLegacy = table.Column<bool>(type: "boolean", nullable: false),
                    DocumentObjectKey = table.Column<string>(type: "text", nullable: true),
                    Sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    VerificationCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CertificateSerial = table.Column<string>(type: "text", nullable: true),
                    CertificateSubject = table.Column<string>(type: "text", nullable: true),
                    CertificateIssuer = table.Column<string>(type: "text", nullable: true),
                    SignedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gazette_editions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "integration_statuses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MunicipalityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "text", nullable: false),
                    State = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    LastErrorCode = table.Column<string>(type: "text", nullable: true),
                    LastCheckedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_integration_statuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "mailboxes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MunicipalityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Address = table.Column<string>(type: "text", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: false),
                    QuotaMegabytes = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ExternalId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mailboxes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "media_assets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MunicipalityId = table.Column<Guid>(type: "uuid", nullable: false),
                    ObjectKey = table.Column<string>(type: "text", nullable: false),
                    OriginalFileName = table.Column<string>(type: "text", nullable: false),
                    MimeType = table.Column<string>(type: "text", nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Sha256 = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    AltText = table.Column<string>(type: "text", nullable: false),
                    Caption = table.Column<string>(type: "text", nullable: false),
                    Credit = table.Column<string>(type: "text", nullable: false),
                    UploadedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    UploadedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_media_assets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "municipalities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    StateCode = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    PrimaryColor = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    LogoObjectKey = table.Column<string>(type: "text", nullable: true),
                    Domain = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_municipalities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "news_articles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MunicipalityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    Slug = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    Summary = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    CoverImageUrl = table.Column<string>(type: "text", nullable: true),
                    CoverImageAlt = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    IsFeatured = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    PublishedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ScheduledFor = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_news_articles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "redirect_rules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MunicipalityId = table.Column<Guid>(type: "uuid", nullable: false),
                    LegacyPath = table.Column<string>(type: "text", nullable: false),
                    DestinationPath = table.Column<string>(type: "text", nullable: false),
                    StatusCode = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastValidatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_redirect_rules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "role_capabilities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MunicipalityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Capability = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role_capabilities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "services",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MunicipalityId = table.Column<Guid>(type: "uuid", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Slug = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Area = table.Column<string>(type: "text", nullable: false),
                    Audience = table.Column<string>(type: "text", nullable: false),
                    Requirements = table.Column<string>(type: "text", nullable: false),
                    Documents = table.Column<string>(type: "text", nullable: false),
                    Steps = table.Column<string>(type: "text", nullable: false),
                    ExpectedDuration = table.Column<string>(type: "text", nullable: false),
                    Cost = table.Column<string>(type: "text", nullable: false),
                    Channels = table.Column<string>(type: "text", nullable: false),
                    IsOnline = table.Column<bool>(type: "boolean", nullable: false),
                    OnlineUrl = table.Column<string>(type: "text", nullable: true),
                    Phone = table.Column<string>(type: "text", nullable: false),
                    Address = table.Column<string>(type: "text", nullable: false),
                    OpeningHours = table.Column<string>(type: "text", nullable: false),
                    LegalBasis = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    IsFeatured = table.Column<bool>(type: "boolean", nullable: false),
                    LastReviewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_services", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "sla_policies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MunicipalityId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sla_policies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tickets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MunicipalityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Protocol = table.Column<string>(type: "text", nullable: false),
                    RequesterName = table.Column<string>(type: "text", nullable: false),
                    Contact = table.Column<string>(type: "text", nullable: false),
                    TrackingCode = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    Priority = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    OpenedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FirstResponseDueAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ResolutionDueAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FirstResponseAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PrivacyConsentAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tickets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "transparency_links",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MunicipalityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    Url = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsExternal = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transparency_links", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MunicipalityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Role = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastLoginAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_audit_events_CorrelationId",
                table: "audit_events",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_audit_events_MunicipalityId_OccurredAt",
                table: "audit_events",
                columns: new[] { "MunicipalityId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_departments_MunicipalityId_Slug",
                table: "departments",
                columns: new[] { "MunicipalityId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_gazette_editions_MunicipalityId_VerificationCode",
                table: "gazette_editions",
                columns: new[] { "MunicipalityId", "VerificationCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_gazette_editions_MunicipalityId_Year_Number",
                table: "gazette_editions",
                columns: new[] { "MunicipalityId", "Year", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_integration_statuses_MunicipalityId_Provider",
                table: "integration_statuses",
                columns: new[] { "MunicipalityId", "Provider" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_mailboxes_MunicipalityId_Address",
                table: "mailboxes",
                columns: new[] { "MunicipalityId", "Address" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_media_assets_MunicipalityId_Sha256",
                table: "media_assets",
                columns: new[] { "MunicipalityId", "Sha256" });

            migrationBuilder.CreateIndex(
                name: "IX_media_assets_MunicipalityId_Status_UploadedAt",
                table: "media_assets",
                columns: new[] { "MunicipalityId", "Status", "UploadedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_municipalities_Domain",
                table: "municipalities",
                column: "Domain",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_municipalities_Slug",
                table: "municipalities",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_news_articles_MunicipalityId_Slug",
                table: "news_articles",
                columns: new[] { "MunicipalityId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_news_articles_MunicipalityId_Status_PublishedAt",
                table: "news_articles",
                columns: new[] { "MunicipalityId", "Status", "PublishedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_redirect_rules_MunicipalityId_LegacyPath",
                table: "redirect_rules",
                columns: new[] { "MunicipalityId", "LegacyPath" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_role_capabilities_MunicipalityId_Role_Capability",
                table: "role_capabilities",
                columns: new[] { "MunicipalityId", "Role", "Capability" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_services_MunicipalityId_Area_Status",
                table: "services",
                columns: new[] { "MunicipalityId", "Area", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_services_MunicipalityId_Slug",
                table: "services",
                columns: new[] { "MunicipalityId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sla_policies_MunicipalityId",
                table: "sla_policies",
                column: "MunicipalityId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tickets_MunicipalityId_Protocol",
                table: "tickets",
                columns: new[] { "MunicipalityId", "Protocol" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tickets_MunicipalityId_Status_ResolutionDueAt",
                table: "tickets",
                columns: new[] { "MunicipalityId", "Status", "ResolutionDueAt" });

            migrationBuilder.CreateIndex(
                name: "IX_transparency_links_MunicipalityId_Category_DisplayOrder",
                table: "transparency_links",
                columns: new[] { "MunicipalityId", "Category", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_users_MunicipalityId_Username",
                table: "users",
                columns: new[] { "MunicipalityId", "Username" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_events");

            migrationBuilder.DropTable(
                name: "departments");

            migrationBuilder.DropTable(
                name: "gazette_editions");

            migrationBuilder.DropTable(
                name: "integration_statuses");

            migrationBuilder.DropTable(
                name: "mailboxes");

            migrationBuilder.DropTable(
                name: "media_assets");

            migrationBuilder.DropTable(
                name: "municipalities");

            migrationBuilder.DropTable(
                name: "news_articles");

            migrationBuilder.DropTable(
                name: "redirect_rules");

            migrationBuilder.DropTable(
                name: "role_capabilities");

            migrationBuilder.DropTable(
                name: "services");

            migrationBuilder.DropTable(
                name: "sla_policies");

            migrationBuilder.DropTable(
                name: "tickets");

            migrationBuilder.DropTable(
                name: "transparency_links");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
