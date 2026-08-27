using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MunicipalPlatform.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MailArchiveInspectionEvidence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CandidateMessages",
                table: "mail_migration_jobs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "InspectedAt",
                table: "mail_migration_jobs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SourceBytes",
                table: "mail_migration_jobs",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "SourceSha256",
                table: "mail_migration_jobs",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CandidateMessages",
                table: "mail_migration_jobs");

            migrationBuilder.DropColumn(
                name: "InspectedAt",
                table: "mail_migration_jobs");

            migrationBuilder.DropColumn(
                name: "SourceBytes",
                table: "mail_migration_jobs");

            migrationBuilder.DropColumn(
                name: "SourceSha256",
                table: "mail_migration_jobs");
        }
    }
}
