using System.IO.Compression;
using System.Text;
using System.Text.Json;
using TgLitePanel.Core.Abstractions.Utils;
using Xunit;

namespace TgLitePanel.Tests;

public sealed class ZipImportSecurityTests
{
    [Fact]
    public async Task ParseAccountsFromZipAsync_可解析单账号格式()
    {
        var phone = "1234567890";
        using var ms = CreateZip(archive =>
        {
            // 创建 .session 文件
            var sessionEntry = archive.CreateEntry($"{phone}.session");
            using (var s = sessionEntry.Open())
                s.Write(Encoding.UTF8.GetBytes("session-data"));

            // 创建 .json 文件
            var jsonEntry = archive.CreateEntry($"{phone}.json");
            using (var s = jsonEntry.Open())
            {
                var json = JsonSerializer.Serialize(new { phone });
                s.Write(Encoding.UTF8.GetBytes(json));
            }
        });

        using var zip = new ZipArchive(ms, ZipArchiveMode.Read, leaveOpen: false);
        var extractDir = Path.Combine(AppContext.BaseDirectory, "test-data-single", Guid.NewGuid().ToString("N"));

        try
        {
            var accounts = await SessionZipImport.ParseAccountsFromZipAsync(zip, extractDir, CancellationToken.None);

            Assert.Single(accounts);
            Assert.Equal(phone, accounts[0].Phone);
            Assert.True(File.Exists(accounts[0].SessionFilePath));
        }
        finally
        {
            if (Directory.Exists(extractDir))
                Directory.Delete(extractDir, recursive: true);
        }
    }

    [Fact]
    public async Task ParseAccountsFromZipAsync_可解析批量导入格式()
    {
        using var ms = CreateZip(archive =>
        {
            // 账号1
            var session1 = archive.CreateEntry("123456789/123456789.session");
            using (var s = session1.Open())
                s.Write(Encoding.UTF8.GetBytes("session1"));

            var json1 = archive.CreateEntry("123456789/123456789.json");
            using (var s = json1.Open())
                s.Write(Encoding.UTF8.GetBytes("{\"phone\":\"123456789\"}"));

            // 账号2 带 2FA
            var session2 = archive.CreateEntry("987654321/987654321.session");
            using (var s = session2.Open())
                s.Write(Encoding.UTF8.GetBytes("session2"));

            var json2 = archive.CreateEntry("987654321/987654321.json");
            using (var s = json2.Open())
                s.Write(Encoding.UTF8.GetBytes("{\"phone\":\"987654321\"}"));

            var twoFa = archive.CreateEntry("987654321/2fa.txt");
            using (var s = twoFa.Open())
                s.Write(Encoding.UTF8.GetBytes("my2fapassword"));
        });

        using var zip = new ZipArchive(ms, ZipArchiveMode.Read, leaveOpen: false);
        var extractDir = Path.Combine(AppContext.BaseDirectory, "test-data-batch", Guid.NewGuid().ToString("N"));

        try
        {
            var accounts = await SessionZipImport.ParseAccountsFromZipAsync(zip, extractDir, CancellationToken.None);

            Assert.Equal(2, accounts.Count);

            var account1 = accounts.First(a => a.Phone == "123456789");
            Assert.Null(account1.TwoFactorPassword);
            Assert.True(File.Exists(account1.SessionFilePath));

            var account2 = accounts.First(a => a.Phone == "987654321");
            Assert.Equal("my2fapassword", account2.TwoFactorPassword);
            Assert.True(File.Exists(account2.SessionFilePath));
        }
        finally
        {
            if (Directory.Exists(extractDir))
                Directory.Delete(extractDir, recursive: true);
        }
    }

    [Fact]
    public async Task ParseAccountsFromZipAsync_忽略无session的文件夹()
    {
        using var ms = CreateZip(archive =>
        {
            // 有效账号
            var session = archive.CreateEntry("valid/valid.session");
            using (var s = session.Open())
                s.Write(Encoding.UTF8.GetBytes("session"));

            // 无效文件夹（没有 session）
            var otherFile = archive.CreateEntry("invalid/some.txt");
            using (var s = otherFile.Open())
                s.Write(Encoding.UTF8.GetBytes("not a session"));
        });

        using var zip = new ZipArchive(ms, ZipArchiveMode.Read, leaveOpen: false);
        var extractDir = Path.Combine(AppContext.BaseDirectory, "test-data-ignore", Guid.NewGuid().ToString("N"));

        try
        {
            var accounts = await SessionZipImport.ParseAccountsFromZipAsync(zip, extractDir, CancellationToken.None);

            Assert.Single(accounts);
            Assert.Equal("valid", accounts[0].Phone);
        }
        finally
        {
            if (Directory.Exists(extractDir))
                Directory.Delete(extractDir, recursive: true);
        }
    }

    [Fact]
    public void ValidateZipSecurity_正常ZIP通过验证()
    {
        using var ms = CreateZip(archive =>
        {
            var entry = archive.CreateEntry("phone/phone.session");
            using var s = entry.Open();
            s.Write(Encoding.UTF8.GetBytes("session data"));
        });

        using var zip = new ZipArchive(ms, ZipArchiveMode.Read, leaveOpen: false);

        // 不应抛出异常
        SessionZipImport.ValidateZipSecurity(zip, ms.Length);
    }

    private static MemoryStream CreateZip(Action<ZipArchive> write)
    {
        var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
            write(archive);
        ms.Position = 0;
        return ms;
    }
}
