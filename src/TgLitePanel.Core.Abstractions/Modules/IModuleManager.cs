using System.Reflection;

namespace TgLitePanel.Core.Abstractions.Modules;

/// <summary>
/// 模块管理器接口
/// </summary>
public interface IModuleManager
{
    /// <summary>
    /// 验证模块 ZIP 文件
    /// </summary>
    Task<ModuleValidationResult> ValidateModuleAsync(
        Stream zipStream,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 安装模块
    /// </summary>
    Task<ModuleInstallResult> InstallModuleAsync(
        Stream zipStream,
        string? userId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 卸载模块
    /// </summary>
    Task<bool> UninstallModuleAsync(
        string moduleId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 启用模块
    /// </summary>
    Task<bool> EnableModuleAsync(
        string moduleId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 禁用模块
    /// </summary>
    Task<bool> DisableModuleAsync(
        string moduleId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取所有模块
    /// </summary>
    Task<IReadOnlyList<ModuleInfo>> GetAllModulesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取模块信息
    /// </summary>
    Task<ModuleInfo?> GetModuleAsync(
        string moduleId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取已加载模块的程序集（用于 Blazor Router）
    /// </summary>
    IEnumerable<Assembly> GetLoadedModuleAssemblies();

    /// <summary>
    /// 获取所有模块菜单项
    /// </summary>
    IEnumerable<ModuleMenuItem> GetModuleMenuItems();

    /// <summary>
    /// 在启动时加载所有已启用的模块
    /// </summary>
    Task LoadEnabledModulesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 模块状态变更事件
    /// </summary>
    event EventHandler<ModuleStatusChangedEventArgs>? ModuleStatusChanged;
}

/// <summary>
/// 模块验证结果
/// </summary>
public sealed record ModuleValidationResult(
    bool IsValid,
    ModuleManifest? Manifest = null,
    IReadOnlyList<string>? Errors = null,
    IReadOnlyList<string>? Warnings = null
);

/// <summary>
/// 模块安装结果
/// </summary>
public sealed record ModuleInstallResult(
    bool Success,
    ModuleInfo? Module = null,
    IReadOnlyList<string>? Errors = null,
    IReadOnlyList<string>? Warnings = null
);

/// <summary>
/// 模块信息
/// </summary>
public sealed class ModuleInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? AuthorName { get; set; }
    public string? AuthorEmail { get; set; }
    public ModuleStatus Status { get; set; } = ModuleStatus.Disabled;
    public string? ErrorMessage { get; set; }
    public string InstallPath { get; set; } = string.Empty;
    public string EntryAssembly { get; set; } = string.Empty;
    public string EntryType { get; set; } = string.Empty;
    public DateTime InstalledAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? LastLoadedAt { get; set; }
    public string? InstalledBy { get; set; }
    public string Checksum { get; set; } = string.Empty;
    public List<string> Permissions { get; set; } = new();
    public List<ModuleRoute> Routes { get; set; } = new();
}

/// <summary>
/// 模块状态
/// </summary>
public enum ModuleStatus
{
    /// <summary>
    /// 已禁用
    /// </summary>
    Disabled = 0,

    /// <summary>
    /// 已启用
    /// </summary>
    Enabled = 1,

    /// <summary>
    /// 加载错误
    /// </summary>
    Error = 2
}

/// <summary>
/// 模块状态变更事件参数
/// </summary>
public sealed class ModuleStatusChangedEventArgs : EventArgs
{
    public string ModuleId { get; }
    public ModuleStatus NewStatus { get; }
    public ModuleStatus? OldStatus { get; }

    public ModuleStatusChangedEventArgs(string moduleId, ModuleStatus newStatus, ModuleStatus? oldStatus = null)
    {
        ModuleId = moduleId;
        NewStatus = newStatus;
        OldStatus = oldStatus;
    }
}
