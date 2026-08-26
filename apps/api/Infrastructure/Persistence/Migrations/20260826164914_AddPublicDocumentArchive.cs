using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MunicipalPlatform.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPublicDocumentArchive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "public_documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MunicipalityId = table.Column<Guid>(type: "uuid", nullable: false),
                    LegacyUrlId = table.Column<Guid>(type: "uuid", nullable: false),
                    MigrationJobId = table.Column<Guid>(type: "uuid", nullable: false),
                    MediaAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Subcategory = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Title = table.Column<string>(type: "character varying(220)", maxLength: 220, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    DocumentNumber = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ProcessNumber = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ReferencePeriod = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    PublicationDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ResponsibleDepartment = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    DocumentType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    SourceUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    NormalizedLegacyPath = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    MimeType = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SourceSystem = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_public_documents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_public_documents_legacy_urls_LegacyUrlId",
                        column: x => x.LegacyUrlId,
                        principalTable: "legacy_urls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_public_documents_media_assets_MediaAssetId",
                        column: x => x.MediaAssetId,
                        principalTable: "media_assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_public_documents_migration_jobs_MigrationJobId",
                        column: x => x.MigrationJobId,
                        principalTable: "migration_jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_public_documents_LegacyUrlId",
                table: "public_documents",
                column: "LegacyUrlId");

            migrationBuilder.CreateIndex(
                name: "IX_public_documents_MediaAssetId",
                table: "public_documents",
                column: "MediaAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_public_documents_MigrationJobId",
                table: "public_documents",
                column: "MigrationJobId");

            migrationBuilder.CreateIndex(
                name: "IX_public_documents_MunicipalityId_DocumentType_ProcessNumber",
                table: "public_documents",
                columns: new[] { "MunicipalityId", "DocumentType", "ProcessNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_public_documents_MunicipalityId_LegacyUrlId",
                table: "public_documents",
                columns: new[] { "MunicipalityId", "LegacyUrlId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_public_documents_MunicipalityId_MediaAssetId",
                table: "public_documents",
                columns: new[] { "MunicipalityId", "MediaAssetId" });

            migrationBuilder.CreateIndex(
                name: "IX_public_documents_MunicipalityId_Status_Category_Publication~",
                table: "public_documents",
                columns: new[] { "MunicipalityId", "Status", "Category", "PublicationDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "public_documents");
        }
    }
}
