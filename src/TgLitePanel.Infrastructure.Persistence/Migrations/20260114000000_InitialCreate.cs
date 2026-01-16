using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TgLitePanel.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260114000000_InitialCreate")]
public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "accounts",
            columns: table => new
            {
                id = table.Column<long>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                phone = table.Column<string>(type: "TEXT", nullable: false),
                status = table.Column<int>(type: "INTEGER", nullable: false),
                data_dir = table.Column<string>(type: "TEXT", nullable: false),
                api_id_override = table.Column<int>(type: "INTEGER", nullable: true),
                system_chat_id = table.Column<long>(type: "INTEGER", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_accounts", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "app_configs",
            columns: table => new
            {
                key = table.Column<string>(type: "TEXT", nullable: false),
                value = table.Column<string>(type: "TEXT", nullable: false),
                updated_at_utc = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_app_configs", x => x.key);
            });

        migrationBuilder.CreateTable(
            name: "security_ops",
            columns: table => new
            {
                id = table.Column<long>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                kind = table.Column<string>(type: "TEXT", nullable: false),
                status = table.Column<string>(type: "TEXT", nullable: false),
                total = table.Column<int>(type: "INTEGER", nullable: false),
                processed = table.Column<int>(type: "INTEGER", nullable: false),
                created_at_utc = table.Column<DateTime>(type: "TEXT", nullable: false),
                updated_at_utc = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_security_ops", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "shared_code_tokens",
            columns: table => new
            {
                token = table.Column<string>(type: "TEXT", nullable: false),
                account_id = table.Column<long>(type: "INTEGER", nullable: false),
                code = table.Column<string>(type: "TEXT", nullable: false),
                expires_at_utc = table.Column<DateTime>(type: "TEXT", nullable: false),
                created_at_utc = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_shared_code_tokens", x => x.token);
            });

        migrationBuilder.CreateTable(
            name: "users",
            columns: table => new
            {
                id = table.Column<long>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                username = table.Column<string>(type: "TEXT", nullable: false),
                password_hash = table.Column<string>(type: "TEXT", nullable: false),
                role = table.Column<string>(type: "TEXT", nullable: false),
                created_at_utc = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_users", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "audit_logs",
            columns: table => new
            {
                id = table.Column<long>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                user_id = table.Column<long>(type: "INTEGER", nullable: true),
                action = table.Column<string>(type: "TEXT", nullable: false),
                summary = table.Column<string>(type: "TEXT", nullable: false),
                ip = table.Column<string>(type: "TEXT", nullable: true),
                created_at_utc = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_audit_logs", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_accounts_phone",
            table: "accounts",
            column: "phone");

        migrationBuilder.CreateIndex(
            name: "IX_audit_logs_created_at_utc",
            table: "audit_logs",
            column: "created_at_utc");

        migrationBuilder.CreateIndex(
            name: "IX_shared_code_tokens_expires_at_utc",
            table: "shared_code_tokens",
            column: "expires_at_utc");

        migrationBuilder.CreateIndex(
            name: "IX_users_username",
            table: "users",
            column: "username",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "accounts");
        migrationBuilder.DropTable(name: "app_configs");
        migrationBuilder.DropTable(name: "audit_logs");
        migrationBuilder.DropTable(name: "security_ops");
        migrationBuilder.DropTable(name: "shared_code_tokens");
        migrationBuilder.DropTable(name: "users");
    }
}
