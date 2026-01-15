using TgLitePanel.Core.Abstractions.Models;

namespace TgLitePanel.Core.Abstractions.Stores;

/// <summary>
/// 告警历史存储接口
/// </summary>
public interface IAlertHistoryStore
{
    /// <summary>
    /// 写入告警记录
    /// </summary>
    Task<long> WriteAsync(
        long? accountId,
        string alertType,
        string message,
        string? details,
        bool notificationSent,
        string? notificationError,
        CancellationToken ct);

    /// <summary>
    /// 获取告警历史
    /// </summary>
    Task<IReadOnlyList<AlertHistory>> ListAsync(int limit, long? accountId, string? alertType, CancellationToken ct);

    /// <summary>
    /// 获取账号最近的告警时间（用于冷却检查）
    /// </summary>
    Task<DateTime?> GetLastAlertTimeAsync(long accountId, string alertType, CancellationToken ct);

    /// <summary>
    /// 清理过期告警
    /// </summary>
    Task CleanupAsync(TimeSpan retention, CancellationToken ct);
}
