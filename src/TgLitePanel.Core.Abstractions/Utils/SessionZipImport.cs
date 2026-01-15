using System.IO.Compression;
using System.Text.Json;
using TgLitePanel.Core.Abstractions.Exceptions;

namespace TgLitePanel.Core.Abstractions.Utils;

/// <summary>
/// WTelegramClient 会话文件导入导出工具
/// 支持格式：
/// 1. 单账号：ZIP 根目录包含 .json + .session 文件
/// 2. 批量导入：每个账号一个子文件夹，包含 .json + .session + 可选 2fa.txt
/// </summary>
public static class SessionZipImport
{
    /// <summary>
    /// 最大 ZIP 文件大小 (512 MB)
    /// </summary>
    public const long MaxZipBytes = 512L * 1024 * 1024;

    /// <summary>
    /// 最大解压后总大小 (1 GB)
    /// </summary>
    public const long MaxExtractedBytes = 1024L * 1024 * 1024;

    /// <summary>
    /// 最大文件数量限制（防止大量小文件的 ZIP 炸弹）
    /// </summary>
    public const int MaxFileCount = 10000;

    /// <summary>
    /// 单个文件最大大小 (256 MB)
    /// </summary>
    public const long MaxSingleFileBytes = 256L * 1024 * 1024;

    /// <summary>
    /// 最大压缩比率（解压大小/压缩大小），超过此比率视为可疑
    /// </summary>
    public const double MaxCompressionRatio = 100.0;

    /// <summary>
    /// 导入的账号信息
    /// </summary>
    public sealed record ImportedAccountInfo(
        string Phone,
        string SessionFilePath,
        string? TwoFactorPassword,
        JsonElement? MetadataJson);

    /// <summary>
    /// 验证 ZIP 归档的安全性（在解压前调用）
    /// </summary>
    public static void ValidateZipSecurity(ZipArchive zip, long compressedSize)
    {
        long totalUncompressedSize = 0;
        var fileCount = 0;

        foreach (var entry in zip.Entries)
        {
            if (entry.FullName.EndsWith("/", StringComparison.Ordinal))
                continue;

            fileCount++;
            if (fileCount > MaxFileCount)
                throw new ValidationException($"ZIP 炸弹检测：文件数量超出限制（最大 {MaxFileCount} 个）。");

            // 检查单个文件大小
            if (entry.Length > MaxSingleFileBytes)
                throw new ValidationException($"ZIP 炸弹检测：单个文件过大（最大 {MaxSingleFileBytes / 1024 / 1024} MB）。");

            totalUncompressedSize += entry.Length;

            // 检查总解压大小
            if (totalUncompressedSize > MaxExtractedBytes)
                throw new ValidationException($"ZIP 炸弹检测：解压总大小超出限制（最大 {MaxExtractedBytes / 1024 / 1024} MB）。");
        }

        // 检查压缩比率
        if (compressedSize > 0)
        {
            var ratio = (double)totalUncompressedSize / compressedSize;
            if (ratio > MaxCompressionRatio)
                throw new ValidationException($"ZIP 炸弹检测：压缩比率异常（{ratio:F1}x，最大允许 {MaxCompressionRatio}x）。");
        }
    }

    /// <summary>
    /// 解析 ZIP 归档中的账号信息
    /// 支持两种格式：
    /// 1. 根目录直接包含 .json + .session（单账号）
    /// 2. 子文件夹包含 .json + .session + 可选 2fa.txt（批量）
    /// </summary>
    public static async Task<List<ImportedAccountInfo>> ParseAccountsFromZipAsync(
        ZipArchive zip,
        string extractDir,
        CancellationToken cancellationToken)
    {
        var accounts = new List<ImportedAccountInfo>();
        var extractedTotal = 0L;

        Directory.CreateDirectory(extractDir);
        var fullExtractDir = Path.GetFullPath(extractDir);

        // 首先检查根目录是否直接包含 .session 文件（单账号模式）
        var rootSessionEntry = zip.Entries.FirstOrDefault(e =>
            !e.FullName.Contains('/') &&
            e.FullName.EndsWith(".session", StringComparison.OrdinalIgnoreCase));

        if (rootSessionEntry is not null)
        {
            // 单账号模式：根目录直接包含 .session
            var account = await ExtractSingleAccountAsync(
                zip, rootSessionEntry, extractDir, fullExtractDir, extractedTotal, cancellationToken);
            if (account is not null)
                accounts.Add(account);
        }
        else
        {
            // 批量模式：查找子文件夹
            var folderGroups = zip.Entries
                .Where(e => e.FullName.Contains('/') && !e.FullName.EndsWith("/"))
                .GroupBy(e => e.FullName.Split('/')[0])
                .Where(g => !string.IsNullOrEmpty(g.Key));

            foreach (var group in folderGroups)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var folderName = group.Key;
                var entries = group.ToList();

                // 查找 .session 文件
                var sessionEntry = entries.FirstOrDefault(e =>
                    e.FullName.EndsWith(".session", StringComparison.OrdinalIgnoreCase));

                if (sessionEntry is null)
                    continue;

                // 查找 .json 文件
                var jsonEntry = entries.FirstOrDefault(e =>
                    e.FullName.EndsWith(".json", StringComparison.OrdinalIgnoreCase));

                // 查找 2fa.txt 文件
                var twoFaEntry = entries.FirstOrDefault(e =>
                    e.FullName.EndsWith("/2fa.txt", StringComparison.OrdinalIgnoreCase) ||
                    e.FullName.Equals($"{folderName}/2fa.txt", StringComparison.OrdinalIgnoreCase));

                // 创建账号目录
                var accountDir = Path.Combine(extractDir, folderName);
                Directory.CreateDirectory(accountDir);

                // 提取 .session 文件
                var sessionPath = Path.Combine(accountDir, "session.dat");
                extractedTotal = await ExtractEntrySecurelyAsync(
                    sessionEntry, sessionPath, fullExtractDir, extractedTotal, cancellationToken);

                // 读取 JSON 元数据
                JsonElement? metadata = null;
                string? phone = null;
                if (jsonEntry is not null)
                {
                    try
                    {
                        await using var jsonStream = jsonEntry.Open();
                        using var doc = await JsonDocument.ParseAsync(jsonStream, cancellationToken: cancellationToken);
                        metadata = doc.RootElement.Clone();

                        // 尝试从 JSON 读取手机号
                        if (doc.RootElement.TryGetProperty("phone", out var phoneEl))
                            phone = phoneEl.GetString();
                        else if (doc.RootElement.TryGetProperty("phone_number", out var phoneNumEl))
                            phone = phoneNumEl.GetString();
                    }
                    catch
                    {
                        // JSON 解析失败，忽略
                    }
                }

                // 如果 JSON 中没有手机号，使用文件夹名作为手机号
                if (string.IsNullOrEmpty(phone))
                    phone = folderName;

                // 读取 2FA 密码
                string? twoFaPassword = null;
                if (twoFaEntry is not null)
                {
                    try
                    {
                        await using var twoFaStream = twoFaEntry.Open();
                        using var reader = new StreamReader(twoFaStream);
                        twoFaPassword = (await reader.ReadToEndAsync(cancellationToken))?.Trim();
                    }
                    catch
                    {
                        // 读取失败，忽略
                    }
                }

                accounts.Add(new ImportedAccountInfo(phone, sessionPath, twoFaPassword, metadata));
            }
        }

        return accounts;
    }

    /// <summary>
    /// 提取单账号（根目录模式）
    /// </summary>
    private static async Task<ImportedAccountInfo?> ExtractSingleAccountAsync(
        ZipArchive zip,
        ZipArchiveEntry sessionEntry,
        string extractDir,
        string fullExtractDir,
        long extractedTotal,
        CancellationToken cancellationToken)
    {
        // 查找对应的 .json 文件
        var baseName = Path.GetFileNameWithoutExtension(sessionEntry.FullName);
        var jsonEntry = zip.Entries.FirstOrDefault(e =>
            !e.FullName.Contains('/') &&
            e.FullName.EndsWith(".json", StringComparison.OrdinalIgnoreCase));

        // 提取 .session 文件
        var sessionPath = Path.Combine(extractDir, "session.dat");
        await ExtractEntrySecurelyAsync(sessionEntry, sessionPath, fullExtractDir, extractedTotal, cancellationToken);

        // 读取 JSON 元数据
        JsonElement? metadata = null;
        string? phone = null;
        if (jsonEntry is not null)
        {
            try
            {
                await using var jsonStream = jsonEntry.Open();
                using var doc = await JsonDocument.ParseAsync(jsonStream, cancellationToken: cancellationToken);
                metadata = doc.RootElement.Clone();

                if (doc.RootElement.TryGetProperty("phone", out var phoneEl))
                    phone = phoneEl.GetString();
                else if (doc.RootElement.TryGetProperty("phone_number", out var phoneNumEl))
                    phone = phoneNumEl.GetString();
            }
            catch
            {
                // JSON 解析失败，忽略
            }
        }

        // 如果没有手机号，使用 session 文件名
        if (string.IsNullOrEmpty(phone))
            phone = baseName;

        // 查找 2fa.txt
        var twoFaEntry = zip.Entries.FirstOrDefault(e =>
            !e.FullName.Contains('/') &&
            e.FullName.Equals("2fa.txt", StringComparison.OrdinalIgnoreCase));

        string? twoFaPassword = null;
        if (twoFaEntry is not null)
        {
            try
            {
                await using var twoFaStream = twoFaEntry.Open();
                using var reader = new StreamReader(twoFaStream);
                twoFaPassword = (await reader.ReadToEndAsync(cancellationToken))?.Trim();
            }
            catch
            {
                // 读取失败，忽略
            }
        }

        return new ImportedAccountInfo(phone, sessionPath, twoFaPassword, metadata);
    }

    /// <summary>
    /// 安全地提取单个文件
    /// </summary>
    private static async Task<long> ExtractEntrySecurelyAsync(
        ZipArchiveEntry entry,
        string destPath,
        string fullDestRoot,
        long extractedTotal,
        CancellationToken cancellationToken)
    {
        // 检查单个文件大小
        if (entry.Length > MaxSingleFileBytes)
            throw new ValidationException($"单个文件过大：{entry.FullName}（最大 {MaxSingleFileBytes / 1024 / 1024} MB）。");

        extractedTotal += entry.Length;
        if (extractedTotal > MaxExtractedBytes)
            throw new ValidationException("解压总大小超出限制。");

        // Zip-Slip 防护
        var fullDestPath = Path.GetFullPath(destPath);
        if (!fullDestPath.StartsWith(fullDestRoot, StringComparison.OrdinalIgnoreCase))
            throw new ValidationException("ZIP 路径非法（Zip-Slip）。");

        Directory.CreateDirectory(Path.GetDirectoryName(fullDestPath)!);

        await using var entryStream = entry.Open();
        await using var limitedStream = new LimitedReadStream(entryStream, entry.Length);
        await using var outStream = new FileStream(fullDestPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await limitedStream.CopyToAsync(outStream, cancellationToken);

        return extractedTotal;
    }
}

/// <summary>
/// 限制读取大小的流包装器，防止解压时读取超出预期大小
/// </summary>
internal sealed class LimitedReadStream : Stream
{
    private readonly Stream _inner;
    private readonly long _maxBytes;
    private long _bytesRead;

    public LimitedReadStream(Stream inner, long maxBytes)
    {
        _inner = inner;
        _maxBytes = maxBytes;
    }

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => _maxBytes;
    public override long Position
    {
        get => _bytesRead;
        set => throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var remaining = _maxBytes - _bytesRead;
        if (remaining <= 0)
            return 0;

        var toRead = (int)Math.Min(count, remaining);
        var read = _inner.Read(buffer, offset, toRead);
        _bytesRead += read;

        if (_bytesRead > _maxBytes)
            throw new ValidationException("ZIP 炸弹检测：实际解压大小超出声明大小。");

        return read;
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        var remaining = _maxBytes - _bytesRead;
        if (remaining <= 0)
            return 0;

        var toRead = (int)Math.Min(count, remaining);
        var read = await _inner.ReadAsync(buffer.AsMemory(offset, toRead), cancellationToken);
        _bytesRead += read;

        if (_bytesRead > _maxBytes)
            throw new ValidationException("ZIP 炸弹检测：实际解压大小超出声明大小。");

        return read;
    }

    public override void Flush() { }
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _inner.Dispose();
        base.Dispose(disposing);
    }
}
