using TgLitePanel.Core.Abstractions.Models;

namespace TgLitePanel.Core.Abstractions.Stores;

/// <summary>
/// 审计日志存储接口
/// </summary>
public interface IAuditLogStore
{
    /// <summary>
    /// 写入审计日志
    /// </summary>
    Task WriteAsync(
        string userName,
        string action,
        string description,
        string? targetId = null,
        string? ipAddress = null,
        string? userAgent = null,
        string result = "success",
        string? additionalData = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 分页查询审计日志
    /// </summary>
    Task<(IReadOnlyList<AuditLog> Items, int TotalCount)> ListPagedAsync(
        int pageIndex,
        int pageSize,
        string? userNameFilter = null,
        string? actionFilter = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取指定用户的最近操作
    /// </summary>
    Task<IReadOnlyList<AuditLog>> GetRecentByUserAsync(
        string userName,
        int count = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 清理过期日志 (保留指定天数)
    /// </summary>
    Task CleanupOldLogsAsync(int retentionDays = 90, CancellationToken cancellationToken = default);
}
