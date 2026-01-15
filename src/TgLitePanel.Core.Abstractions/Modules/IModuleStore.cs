namespace TgLitePanel.Core.Abstractions.Modules;

/// <summary>
/// 模块数据存储接口
/// </summary>
public interface IModuleStore
{
    /// <summary>
    /// 获取模块
    /// </summary>
    Task<ModuleInfo?> GetModuleAsync(string moduleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取所有模块
    /// </summary>
    Task<IReadOnlyList<ModuleInfo>> GetAllModulesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取已启用的模块
    /// </summary>
    Task<IReadOnlyList<ModuleInfo>> GetEnabledModulesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 保存模块
    /// </summary>
    Task SaveModuleAsync(ModuleInfo module, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新模块状态
    /// </summary>
    Task UpdateModuleStatusAsync(
        string moduleId,
        ModuleStatus status,
        string? errorMessage = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新模块最后加载时间
    /// </summary>
    Task UpdateLastLoadedTimeAsync(
        string moduleId,
        DateTime loadedAt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除模块
    /// </summary>
    Task DeleteModuleAsync(string moduleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 添加审计日志
    /// </summary>
    Task AddAuditLogAsync(
        string moduleId,
        string eventType,
        object? eventData,
        string? userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取模块审计日志
    /// </summary>
    Task<IReadOnlyList<ModuleAuditLog>> GetAuditLogsAsync(
        string? moduleId = null,
        int limit = 100,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 模块审计日志
/// </summary>
public sealed class ModuleAuditLog
{
    public long Id { get; set; }
    public string ModuleId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string? EventData { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? UserId { get; set; }
}
