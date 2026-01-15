using TgLitePanel.Core.Abstractions.Models;

namespace TgLitePanel.Core.Abstractions.Stores;

/// <summary>
/// 账号状态日志存储接口
/// </summary>
public interface IAccountStatusLogStore
{
    /// <summary>
    /// 写入状态日志
    /// </summary>
    Task WriteAsync(long accountId, bool isOnline, string? error, string source, CancellationToken ct);

    /// <summary>
    /// 获取账号的状态历史
    /// </summary>
    Task<IReadOnlyList<AccountStatusLog>> GetByAccountAsync(long accountId, int limit, CancellationToken ct);

    /// <summary>
    /// 获取账号最近的连续失败次数
    /// </summary>
    Task<int> GetConsecutiveFailureCountAsync(long accountId, CancellationToken ct);

    /// <summary>
    /// 获取所有账号的最近状态
    /// </summary>
    Task<IReadOnlyDictionary<long, AccountStatusLog>> GetLatestForAllAsync(CancellationToken ct);

    /// <summary>
    /// 清理过期日志
    /// </summary>
    Task CleanupAsync(TimeSpan retention, CancellationToken ct);
}
