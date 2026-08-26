using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace MunicipalPlatform.Api.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260826040500_IdentityHardening")]
public partial class IdentityHardening : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(name: "FailedLoginCount", table: "users", type: "integer", nullable: false, defaultValue: 0);
        migrationBuilder.AddColumn<DateTimeOffset>(name: "LockedUntil", table: "users", type: "timestamp with time zone", nullable: true);
        migrationBuilder.AddColumn<int>(name: "SessionVersion", table: "users", type: "integer", nullable: false, defaultValue: 0);
        migrationBuilder.AddColumn<bool>(name: "MfaEnabled", table: "users", type: "boolean", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<string>(name: "MfaSecretProtected", table: "users", type: "character varying(4096)", maxLength: 4096, nullable: true);
        migrationBuilder.AddColumn<string>(name: "MfaPendingSecretProtected", table: "users", type: "character varying(4096)", maxLength: 4096, nullable: true);
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "FailedLoginCount", table: "users"); migrationBuilder.DropColumn(name: "LockedUntil", table: "users"); migrationBuilder.DropColumn(name: "SessionVersion", table: "users"); migrationBuilder.DropColumn(name: "MfaEnabled", table: "users"); migrationBuilder.DropColumn(name: "MfaSecretProtected", table: "users"); migrationBuilder.DropColumn(name: "MfaPendingSecretProtected", table: "users");
    }
}
