using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace MunicipalPlatform.Api.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260826035000_TicketComments")]
public partial class TicketComments : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) { migrationBuilder.CreateTable(name: "ticket_comments", columns: table => new { Id = table.Column<Guid>(type: "uuid", nullable: false), MunicipalityId = table.Column<Guid>(type: "uuid", nullable: false), TicketId = table.Column<Guid>(type: "uuid", nullable: false), AuthorId = table.Column<Guid>(type: "uuid", nullable: false), Body = table.Column<string>(type: "text", nullable: false), IsInternal = table.Column<bool>(type: "boolean", nullable: false), CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false) }, constraints: table => table.PrimaryKey("PK_ticket_comments", x => x.Id)); migrationBuilder.CreateIndex(name: "IX_ticket_comments_MunicipalityId_TicketId_CreatedAt", table: "ticket_comments", columns: new[] { "MunicipalityId", "TicketId", "CreatedAt" }); }
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable(name: "ticket_comments");
}
