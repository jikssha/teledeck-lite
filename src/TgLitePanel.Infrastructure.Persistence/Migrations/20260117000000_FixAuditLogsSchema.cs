using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TgLitePanel.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260117000000_FixAuditLogsSchema")]
    /// <summary>
    /// 修复 audit_logs 表结构，使其与 AuditLogEntity 匹配
    /// </summary>
    public partial class FixAuditLogsSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SQLite 不支持完整的 ALTER TABLE，所以需要重建表
            // 1. 重命名旧表
            migrationBuilder.Sql("ALTER TABLE audit_logs RENAME TO audit_logs_old;");

            // 2. 创建新表，使用正确的结构
            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    user_name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    action = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    target_id = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ip_address = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    user_agent = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    result = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    additional_data = table.Column<string>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.id);
                });

            // 3. 创建索引
            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_user_name",
                table: "audit_logs",
                column: "user_name");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_action",
                table: "audit_logs",
                column: "action");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_created_at",
                table: "audit_logs",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_user_name_created_at",
                table: "audit_logs",
                columns: new[] { "user_name", "created_at" });

            // 4. 尝试迁移旧数据（如果存在）
            // 由于字段名不同，只能迁移部分数据
            migrationBuilder.Sql(@"
                INSERT INTO audit_logs (user_name, action, description, ip_address, result, created_at)
                SELECT
                    COALESCE(CAST(user_id AS TEXT), 'system'),
                    action,
                    summary,
                    ip,
                    'success',
                    created_at_utc
                FROM audit_logs_old;
            ");

            // 5. 删除旧表
            migrationBuilder.Sql("DROP TABLE audit_logs_old;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 回滚：重建旧结构的表
            migrationBuilder.Sql("ALTER TABLE audit_logs RENAME TO audit_logs_new;");

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
                name: "IX_audit_logs_created_at_utc",
                table: "audit_logs",
                column: "created_at_utc");

            // 迁移数据回旧格式
            migrationBuilder.Sql(@"
                INSERT INTO audit_logs (action, summary, ip, created_at_utc)
                SELECT action, description, ip_address, created_at
                FROM audit_logs_new;
            ");

            migrationBuilder.Sql("DROP TABLE audit_logs_new;");
        }
    }
}
