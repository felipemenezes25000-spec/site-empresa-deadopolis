using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MunicipalPlatform.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MunicipalDepthPreview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CropHeight",
                table: "media_assets",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CropWidth",
                table: "media_assets",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CropX",
                table: "media_assets",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CropY",
                table: "media_assets",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FocalPointX",
                table: "media_assets",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FocalPointY",
                table: "media_assets",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TagsCsv",
                table: "media_assets",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CropHeight",
                table: "media_assets");

            migrationBuilder.DropColumn(
                name: "CropWidth",
                table: "media_assets");

            migrationBuilder.DropColumn(
                name: "CropX",
                table: "media_assets");

            migrationBuilder.DropColumn(
                name: "CropY",
                table: "media_assets");

            migrationBuilder.DropColumn(
                name: "FocalPointX",
                table: "media_assets");

            migrationBuilder.DropColumn(
                name: "FocalPointY",
                table: "media_assets");

            migrationBuilder.DropColumn(
                name: "TagsCsv",
                table: "media_assets");
        }
    }
}
