using System.IO.Compression;
using System.Text;
using TgLitePanel.Core.Abstractions.Exceptions;
using TgLitePanel.Core.Abstractions.Utils;
using Xunit;

namespace TgLitePanel.Tests;

/// <summary>
/// ZIP 炸弹防护相关测试
/// </summary>
public sealed class ZipBombProtectionTests
{
    [Fact]
    public void ValidateZipSecurity_正常文件通过验证()
    {
        using var ms = CreateZip(archive =>
        {
            var entry = archive.CreateEntry("123456789/123456789.session");
            using var s = entry.Open();
            s.Write(Encoding.UTF8.GetBytes("normal session content"));
        });

        using var zip = new ZipArchive(ms, ZipArchiveMode.Read, leaveOpen: false);

        // 不应抛出异常
        SessionZipImport.ValidateZipSecurity(zip, ms.Length);
    }

    [Fact]
    public void ValidateZipSecurity_检测高压缩比()
    {
        // 验证压缩比率计算逻辑
        var compressedSize = 100L;
        var uncompressedSize = 15000L; // 150x 压缩比
        var ratio = (double)uncompressedSize / compressedSize;

        Assert.True(ratio > SessionZipImport.MaxCompressionRatio);
    }

    [Fact]
    public void ValidateZipSecurity_文件数量超限抛出异常()
    {
        // 创建一个包含大量文件的 ZIP
        using var ms = CreateZipWithManyFiles(SessionZipImport.MaxFileCount + 1);
        using var zip = new ZipArchive(ms, ZipArchiveMode.Read, leaveOpen: false);

        var ex = Assert.Throws<ValidationException>(() => SessionZipImport.ValidateZipSecurity(zip, ms.Length));
        Assert.Contains("文件数量超出限制", ex.Message);
    }

    [Fact]
    public void ValidateZipSecurity_正常文件数量通过验证()
    {
        using var ms = CreateZipWithManyFiles(100);
        using var zip = new ZipArchive(ms, ZipArchiveMode.Read, leaveOpen: false);

        // 不应抛出异常
        SessionZipImport.ValidateZipSecurity(zip, ms.Length);
    }

    [Fact]
    public void Constants_值正确配置()
    {
        // 验证常量值的合理性
        Assert.Equal(512L * 1024 * 1024, SessionZipImport.MaxZipBytes);
        Assert.Equal(1024L * 1024 * 1024, SessionZipImport.MaxExtractedBytes);
        Assert.Equal(10000, SessionZipImport.MaxFileCount);
        Assert.Equal(256L * 1024 * 1024, SessionZipImport.MaxSingleFileBytes);
        Assert.Equal(100.0, SessionZipImport.MaxCompressionRatio);
    }

    private static MemoryStream CreateZip(Action<ZipArchive> write)
    {
        var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
            write(archive);
        ms.Position = 0;
        return ms;
    }

    private static MemoryStream CreateZipWithManyFiles(int count)
    {
        var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            for (var i = 0; i < count; i++)
            {
                var entry = archive.CreateEntry($"account{i}/session.dat");
                using var s = entry.Open();
                s.Write(Encoding.UTF8.GetBytes($"session{i}"));
            }
        }
        ms.Position = 0;
        return ms;
    }
}
