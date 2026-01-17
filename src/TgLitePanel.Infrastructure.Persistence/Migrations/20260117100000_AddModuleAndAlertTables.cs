using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TgLitePanel.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260117100000_AddModuleAndAlertTables")]
    /// <summary>
    /// 添加模块管理和告警相关表
    /// </summary>
    public partial class AddModuleAndAlertTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 模块表
            migrationBuilder.CreateTable(
                name: "modules",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    version = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    author_name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    author_email = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    status = table.Column<int>(type: "INTEGER", nullable: false),
                    error_message = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    install_path = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    entry_assembly = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    entry_type = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    installed_at_utc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    last_loaded_at_utc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    installed_by = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    checksum = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    permissions_json = table.Column<string>(type: "TEXT", nullable: true),
                    routes_json = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_modules", x => x.id);
                });

            // 模块审计日志表
            migrationBuilder.CreateTable(
                name: "module_audit_logs",
                columns: table => new
                {
                    id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    module_id = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    event_type = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    event_data = table.Column<string>(type: "TEXT", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    user_id = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_module_audit_logs", x => x.id);
                });

            // 账号状态日志表
            migrationBuilder.CreateTable(
                name: "account_status_logs",
                columns: table => new
                {
                    id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    account_id = table.Column<long>(type: "INTEGER", nullable: false),
                    is_online = table.Column<bool>(type: "INTEGER", nullable: false),
                    error = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    checked_at_utc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    source = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_account_status_logs", x => x.id);
                });

            // 告警配置表
            migrationBuilder.CreateTable(
                name: "alert_configs",
                columns: table => new
                {
                    id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    alert_type = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    is_enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    consecutive_failure_threshold = table.Column<int>(type: "INTEGER", nullable: false),
                    cooldown_minutes = table.Column<int>(type: "INTEGER", nullable: false),
                    notify_methods = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    account_ids_json = table.Column<string>(type: "TEXT", nullable: true),
                    group_ids_json = table.Column<string>(type: "TEXT", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_alert_configs", x => x.id);
                });

            // 告警历史表
            migrationBuilder.CreateTable(
                name: "alert_histories",
                columns: table => new
                {
                    id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    account_id = table.Column<long>(type: "INTEGER", nullable: false),
                    alert_type = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    message = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    details = table.Column<string>(type: "TEXT", nullable: true),
                    notification_sent = table.Column<bool>(type: "INTEGER", nullable: false),
                    notification_error = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_alert_histories", x => x.id);
                });

            // 创建索引
            migrationBuilder.CreateIndex(
                name: "IX_module_audit_logs_module_id",
                table: "module_audit_logs",
                column: "module_id");

            migrationBuilder.CreateIndex(
                name: "IX_module_audit_logs_created_at_utc",
                table: "module_audit_logs",
                column: "created_at_utc");

            migrationBuilder.CreateIndex(
                name: "IX_account_status_logs_account_id",
                table: "account_status_logs",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "IX_account_status_logs_checked_at_utc",
                table: "account_status_logs",
                column: "checked_at_utc");

            migrationBuilder.CreateIndex(
                name: "IX_account_status_logs_account_id_checked_at_utc",
                table: "account_status_logs",
                columns: new[] { "account_id", "checked_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_alert_configs_alert_type",
                table: "alert_configs",
                column: "alert_type");

            migrationBuilder.CreateIndex(
                name: "IX_alert_histories_account_id",
                table: "alert_histories",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "IX_alert_histories_alert_type",
                table: "alert_histories",
                column: "alert_type");

            migrationBuilder.CreateIndex(
                name: "IX_alert_histories_created_at_utc",
                table: "alert_histories",
                column: "created_at_utc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "modules");
            migrationBuilder.DropTable(name: "module_audit_logs");
            migrationBuilder.DropTable(name: "account_status_logs");
            migrationBuilder.DropTable(name: "alert_configs");
            migrationBuilder.DropTable(name: "alert_histories");
        }
    }
}
