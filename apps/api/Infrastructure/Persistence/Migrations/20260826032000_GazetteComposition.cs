using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MunicipalPlatform.Api.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260826032000_GazetteComposition")]
public partial class GazetteComposition : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "CompositionJson",
            table: "gazette_editions",
            type: "text",
            nullable: false,
            defaultValue: "{\"sections\":[]}");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "CompositionJson", table: "gazette_editions");
    }
}
