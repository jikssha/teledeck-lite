using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TgLitePanel.Core.Abstractions.Modules;

namespace TgLitePanel.Core.Modules;

/// <summary>
/// 模块验证器 - 负责 ZIP 安全验证和清单解析
/// </summary>
public sealed class ModuleValidator
{
    private readonly ModuleOptions _options;
    private readonly ILogger<ModuleValidator> _logger;

    public ModuleValidator(
        IOptions<ModuleOptions> options,
        ILogger<ModuleValidator> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// 验证模块 ZIP 文件
    /// </summary>
    public async Task<ModuleValidationResult> ValidateAsync(
        Stream zipStream,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        try
        {
            // 1. 检查文件大小
            if (zipStream.Length > _options.MaxZipSizeBytes)
            {
                errors.Add($"ZIP 文件过大：{zipStream.Length / 1024 / 1024}MB，最大允许 {_options.MaxZipSizeBytes / 1024 / 1024}MB");
                return new ModuleValidationResult(false, null, errors, warnings);
            }

            // 重置流位置
            if (zipStream.CanSeek)
            {
                zipStream.Position = 0;
            }

            using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: true);

            // 2. 检查文件数量
            if (archive.Entries.Count > _options.MaxFileCount)
            {
                errors.Add($"文件数量过多：{archive.Entries.Count}，最大允许 {_options.MaxFileCount}");
                return new ModuleValidationResult(false, null, errors, warnings);
            }

            // 3. 查找 manifest.json
            var manifestEntry = archive.Entries.FirstOrDefault(e =>
                e.FullName.Equals("manifest.json", StringComparison.OrdinalIgnoreCase));

            if (manifestEntry == null)
            {
                errors.Add("缺少 manifest.json 文件");
                return new ModuleValidationResult(false, null, errors, warnings);
            }

            // 4. 解析清单
            ModuleManifest? manifest;
            try
            {
                using var manifestStream = manifestEntry.Open();
                manifest = await JsonSerializer.DeserializeAsync<ModuleManifest>(
                    manifestStream,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                    cancellationToken);

                if (manifest == null)
                {
                    errors.Add("manifest.json 解析失败");
                    return new ModuleValidationResult(false, null, errors, warnings);
                }
            }
            catch (JsonException ex)
            {
                errors.Add($"manifest.json 格式错误：{ex.Message}");
                return new ModuleValidationResult(false, null, errors, warnings);
            }

            // 5. 验证清单必填字段
            ValidateManifest(manifest, errors, warnings);

            if (errors.Count > 0)
            {
                return new ModuleValidationResult(false, manifest, errors, warnings);
            }

            // 6. 安全检查每个条目
            long totalUncompressedSize = 0;

            foreach (var entry in archive.Entries)
            {
                // 跳过目录
                if (string.IsNullOrEmpty(entry.Name))
                    continue;

                // Zip-Slip 检测
                if (entry.FullName.Contains("..") ||
                    Path.IsPathRooted(entry.FullName) ||
                    entry.FullName.StartsWith("/") ||
                    entry.FullName.StartsWith("\\"))
                {
                    errors.Add($"检测到路径穿越攻击：{entry.FullName}");
                    continue;
                }

                // 检查禁止的扩展名
                var extension = Path.GetExtension(entry.Name);
                if (_options.BlockedExtensions.Contains(extension))
                {
                    errors.Add($"禁止的文件类型：{entry.FullName}");
                    continue;
                }

                // 压缩炸弹检测
                if (entry.CompressedLength > 0)
                {
                    var ratio = (double)entry.Length / entry.CompressedLength;
                    if (ratio > _options.MaxCompressionRatio)
                    {
                        errors.Add($"可疑的压缩比率（{ratio:F1}x）：{entry.FullName}");
                        continue;
                    }
                }

                totalUncompressedSize += entry.Length;

                if (totalUncompressedSize > _options.MaxExtractedSizeBytes)
                {
                    errors.Add($"解压后总大小超限：{totalUncompressedSize / 1024 / 1024}MB");
                    break;
                }
            }

            // 7. 验证入口程序集存在
            var entryAssembly = manifest.EntryPoint?.Assembly;
            if (!string.IsNullOrEmpty(entryAssembly))
            {
                var hasEntryAssembly = archive.Entries.Any(e =>
                    e.FullName.Equals(entryAssembly, StringComparison.OrdinalIgnoreCase) ||
                    e.FullName.EndsWith("/" + entryAssembly, StringComparison.OrdinalIgnoreCase));

                if (!hasEntryAssembly)
                {
                    errors.Add($"找不到入口程序集：{entryAssembly}");
                }
            }

            // 8. 检查权限
            if (manifest.Permissions?.Any() == true)
            {
                foreach (var permission in manifest.Permissions)
                {
                    if (!ModulePermissions.All.ContainsKey(permission))
                    {
                        warnings.Add($"未知权限：{permission}");
                    }
                    else
                    {
                        var info = ModulePermissions.All[permission];
                        if (info.Level == PermissionLevel.High)
                        {
                            warnings.Add($"高风险权限：{ModulePermissions.GetDescription(permission)}");
                        }
                    }
                }
            }

            return new ModuleValidationResult(
                errors.Count == 0,
                manifest,
                errors.Count > 0 ? errors : null,
                warnings.Count > 0 ? warnings : null);
        }
        catch (InvalidDataException ex)
        {
            errors.Add($"无效的 ZIP 文件：{ex.Message}");
            return new ModuleValidationResult(false, null, errors, warnings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "模块验证过程中发生异常");
            errors.Add($"验证过程中发生错误：{ex.Message}");
            return new ModuleValidationResult(false, null, errors, warnings);
        }
    }

    /// <summary>
    /// 计算文件校验和
    /// </summary>
    public static string ComputeChecksum(Stream stream)
    {
        if (stream.CanSeek)
        {
            stream.Position = 0;
        }

        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(stream);

        if (stream.CanSeek)
        {
            stream.Position = 0;
        }

        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private void ValidateManifest(ModuleManifest manifest, List<string> errors, List<string> warnings)
    {
        if (string.IsNullOrWhiteSpace(manifest.Id))
        {
            errors.Add("manifest.json 缺少 id 字段");
        }
        else if (!IsValidModuleId(manifest.Id))
        {
            errors.Add($"无效的模块 ID 格式：{manifest.Id}（应为 com.example.module 格式）");
        }

        if (string.IsNullOrWhiteSpace(manifest.Name))
        {
            errors.Add("manifest.json 缺少 name 字段");
        }

        if (string.IsNullOrWhiteSpace(manifest.Version))
        {
            errors.Add("manifest.json 缺少 version 字段");
        }
        else if (!Version.TryParse(manifest.Version.Split('-')[0], out _))
        {
            warnings.Add($"版本号格式建议使用 SemVer：{manifest.Version}");
        }

        if (manifest.EntryPoint == null)
        {
            errors.Add("manifest.json 缺少 entryPoint 配置");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(manifest.EntryPoint.Assembly))
            {
                errors.Add("manifest.json 缺少 entryPoint.assembly 字段");
            }

            if (string.IsNullOrWhiteSpace(manifest.EntryPoint.Type))
            {
                errors.Add("manifest.json 缺少 entryPoint.type 字段");
            }
        }
    }

    private static bool IsValidModuleId(string id)
    {
        // 模块 ID 格式：com.example.module 或 my-module
        if (string.IsNullOrWhiteSpace(id) || id.Length > 100)
            return false;

        // 允许字母、数字、点、横线、下划线
        return id.All(c => char.IsLetterOrDigit(c) || c == '.' || c == '-' || c == '_');
    }
}
