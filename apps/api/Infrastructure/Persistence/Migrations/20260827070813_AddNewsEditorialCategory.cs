using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MunicipalPlatform.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNewsEditorialCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_news_articles_MunicipalityId_Status_PublishedAt",
                table: "news_articles");

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "news_articles",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "GERAL");

            migrationBuilder.CreateIndex(
                name: "IX_news_articles_MunicipalityId_Status_Category_PublishedAt",
                table: "news_articles",
                columns: new[] { "MunicipalityId", "Status", "Category", "PublishedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_news_articles_MunicipalityId_Status_Category_PublishedAt",
                table: "news_articles");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "news_articles");

            migrationBuilder.CreateIndex(
                name: "IX_news_articles_MunicipalityId_Status_PublishedAt",
                table: "news_articles",
                columns: new[] { "MunicipalityId", "Status", "PublishedAt" });
        }
    }
}
