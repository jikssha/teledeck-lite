using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TgLitePanel.Infrastructure.Persistence;
using Xunit;

namespace TgLitePanel.Tests;

/// <summary>
/// EF Core 迁移发现回归测试：
/// 防止迁移类缺少特性导致运行时认为“无迁移”，从而首次启动不建表。
/// </summary>
public sealed class EfMigrationsDiscoveryTests
{
    [Fact]
    public async Task GetMigrations_应能发现至少一个迁移()
    {
        await using var conn = new SqliteConnection("Data Source=:memory:;Cache=Shared");
        await conn.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(conn)
            .Options;

        await using var db = new AppDbContext(options);
        var migrations = db.Database.GetMigrations();

        Assert.NotEmpty(migrations);
    }
}
