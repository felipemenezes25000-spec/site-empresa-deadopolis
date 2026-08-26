using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MunicipalPlatform.Api.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260826033500_CmsResources")]
public partial class CmsResources : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(name: "portal_resources", columns: table => new
        {
            Id = table.Column<Guid>(type: "uuid", nullable: false), MunicipalityId = table.Column<Guid>(type: "uuid", nullable: false), Kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false), Slug = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false), Title = table.Column<string>(type: "character varying(220)", maxLength: 220, nullable: false), Summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false), PayloadJson = table.Column<string>(type: "jsonb", nullable: false), Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false), DisplayOrder = table.Column<int>(type: "integer", nullable: false), Version = table.Column<int>(type: "integer", nullable: false), StartsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true), EndsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true), PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true), LastReviewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false), CreatedBy = table.Column<Guid>(type: "uuid", nullable: false), UpdatedBy = table.Column<Guid>(type: "uuid", nullable: false), CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false), UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
        }, constraints: table => table.PrimaryKey("PK_portal_resources", x => x.Id));
        migrationBuilder.CreateIndex(name: "IX_portal_resources_MunicipalityId_Kind_Slug", table: "portal_resources", columns: new[] { "MunicipalityId", "Kind", "Slug" }, unique: true);
        migrationBuilder.CreateIndex(name: "IX_portal_resources_MunicipalityId_Kind_Status_DisplayOrder", table: "portal_resources", columns: new[] { "MunicipalityId", "Kind", "Status", "DisplayOrder" });

        migrationBuilder.CreateTable(name: "content_revisions", columns: table => new
        {
            Id = table.Column<Guid>(type: "uuid", nullable: false), MunicipalityId = table.Column<Guid>(type: "uuid", nullable: false), ResourceKind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false), ResourceId = table.Column<Guid>(type: "uuid", nullable: false), Version = table.Column<int>(type: "integer", nullable: false), SnapshotJson = table.Column<string>(type: "jsonb", nullable: false), CreatedBy = table.Column<Guid>(type: "uuid", nullable: false), CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
        }, constraints: table => table.PrimaryKey("PK_content_revisions", x => x.Id));
        migrationBuilder.CreateIndex(name: "IX_content_revisions_MunicipalityId_ResourceKind_ResourceId_CreatedAt", table: "content_revisions", columns: new[] { "MunicipalityId", "ResourceKind", "ResourceId", "CreatedAt" });

        migrationBuilder.CreateTable(name: "outbox_messages", columns: table => new
        {
            Id = table.Column<Guid>(type: "uuid", nullable: false), MunicipalityId = table.Column<Guid>(type: "uuid", nullable: false), Type = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false), PayloadJson = table.Column<string>(type: "jsonb", nullable: false), OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false), ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true), Attempts = table.Column<int>(type: "integer", nullable: false)
        }, constraints: table => table.PrimaryKey("PK_outbox_messages", x => x.Id));
        migrationBuilder.CreateIndex(name: "IX_outbox_messages_MunicipalityId_ProcessedAt_OccurredAt", table: "outbox_messages", columns: new[] { "MunicipalityId", "ProcessedAt", "OccurredAt" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "content_revisions");
        migrationBuilder.DropTable(name: "outbox_messages");
        migrationBuilder.DropTable(name: "portal_resources");
    }
}
