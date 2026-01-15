namespace TgLitePanel.Core.Modules;

/// <summary>
/// 模块系统配置选项
/// </summary>
public sealed class ModuleOptions
{
    /// <summary>
    /// 模块安装目录（相对于数据目录）
    /// </summary>
    public string ModulesDirectory { get; set; } = "modules";

    /// <summary>
    /// 最大模块 ZIP 文件大小（默认 50MB）
    /// </summary>
    public long MaxZipSizeBytes { get; set; } = 50 * 1024 * 1024;

    /// <summary>
    /// 解压后最大总大小（默认 200MB）
    /// </summary>
    public long MaxExtractedSizeBytes { get; set; } = 200 * 1024 * 1024;

    /// <summary>
    /// 最大解压文件数量
    /// </summary>
    public int MaxFileCount { get; set; } = 500;

    /// <summary>
    /// 压缩比例阈值（用于检测压缩炸弹）
    /// </summary>
    public double MaxCompressionRatio { get; set; } = 50.0;

    /// <summary>
    /// 允许的程序集后缀
    /// </summary>
    public HashSet<string> AllowedAssemblyExtensions { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ".dll"
    };

    /// <summary>
    /// 禁止的文件扩展名
    /// </summary>
    public HashSet<string> BlockedExtensions { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ".exe", ".bat", ".cmd", ".ps1", ".sh", ".vbs", ".js", ".msi"
    };

    /// <summary>
    /// 共享程序集列表（这些程序集由宿主提供，模块不加载自己的版本）
    /// </summary>
    public HashSet<string> SharedAssemblies { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        "TgLitePanel.Core.Abstractions",
        "Microsoft.AspNetCore.Components",
        "Microsoft.Extensions.DependencyInjection.Abstractions",
        "Microsoft.Extensions.Logging.Abstractions",
        "Microsoft.Extensions.Configuration.Abstractions",
        "MudBlazor"
    };
}
