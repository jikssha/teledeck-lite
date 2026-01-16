using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TgLitePanel.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260115000000_AddNewFeatures")]
    /// <inheritdoc />
    public partial class AddNewFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 账号分组表
            migrationBuilder.CreateTable(
                name: "account_groups",
                columns: table => new
                {
                    id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    color = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    sort_order = table.Column<int>(type: "INTEGER", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_account_groups", x => x.id);
                });

            // 消息缓存表
            migrationBuilder.CreateTable(
                name: "cached_messages",
                columns: table => new
                {
                    id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    account_id = table.Column<long>(type: "INTEGER", nullable: false),
                    chat_id = table.Column<long>(type: "INTEGER", nullable: false),
                    message_id = table.Column<long>(type: "INTEGER", nullable: false),
                    sender_id = table.Column<long>(type: "INTEGER", nullable: false),
                    sender_name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    content = table.Column<string>(type: "TEXT", nullable: true),
                    message_type = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    is_outgoing = table.Column<bool>(type: "INTEGER", nullable: false),
                    message_date_utc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    cached_at_utc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cached_messages", x => x.id);
                });

            // 聊天缓存表
            migrationBuilder.CreateTable(
                name: "cached_chats",
                columns: table => new
                {
                    id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    account_id = table.Column<long>(type: "INTEGER", nullable: false),
                    chat_id = table.Column<long>(type: "INTEGER", nullable: false),
                    title = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    chat_type = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    unread_count = table.Column<int>(type: "INTEGER", nullable: false),
                    last_message_id = table.Column<long>(type: "INTEGER", nullable: true),
                    last_message_preview = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    last_message_date_utc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    cached_at_utc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cached_chats", x => x.id);
                });

            // Webhook 配置表
            migrationBuilder.CreateTable(
                name: "webhook_configs",
                columns: table => new
                {
                    id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    url = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    secret = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    is_enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    events = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    account_ids = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    retry_count = table.Column<int>(type: "INTEGER", nullable: false),
                    timeout_seconds = table.Column<int>(type: "INTEGER", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    last_triggered_at_utc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    last_error = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_webhook_configs", x => x.id);
                });

            // 账号表新增字段
            migrationBuilder.AddColumn<long>(
                name: "group_id",
                table: "accounts",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_online_utc",
                table: "accounts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_checked_utc",
                table: "accounts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "last_error",
                table: "accounts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_online",
                table: "accounts",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "display_name",
                table: "accounts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "username",
                table: "accounts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at_utc",
                table: "accounts",
                type: "TEXT",
                nullable: false,
                defaultValueSql: "datetime('now')");

            // 索引
            migrationBuilder.CreateIndex(
                name: "IX_account_groups_name",
                table: "account_groups",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_accounts_group_id",
                table: "accounts",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "IX_cached_messages_account_chat_message",
                table: "cached_messages",
                columns: new[] { "account_id", "chat_id", "message_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cached_messages_account_chat_date",
                table: "cached_messages",
                columns: new[] { "account_id", "chat_id", "message_date_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_cached_messages_content",
                table: "cached_messages",
                column: "content");

            migrationBuilder.CreateIndex(
                name: "IX_cached_chats_account_chat",
                table: "cached_chats",
                columns: new[] { "account_id", "chat_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "account_groups");
            migrationBuilder.DropTable(name: "cached_messages");
            migrationBuilder.DropTable(name: "cached_chats");
            migrationBuilder.DropTable(name: "webhook_configs");

            migrationBuilder.DropIndex(name: "IX_accounts_group_id", table: "accounts");

            migrationBuilder.DropColumn(name: "group_id", table: "accounts");
            migrationBuilder.DropColumn(name: "last_online_utc", table: "accounts");
            migrationBuilder.DropColumn(name: "last_checked_utc", table: "accounts");
            migrationBuilder.DropColumn(name: "last_error", table: "accounts");
            migrationBuilder.DropColumn(name: "is_online", table: "accounts");
            migrationBuilder.DropColumn(name: "display_name", table: "accounts");
            migrationBuilder.DropColumn(name: "username", table: "accounts");
            migrationBuilder.DropColumn(name: "created_at_utc", table: "accounts");
        }
    }
}
