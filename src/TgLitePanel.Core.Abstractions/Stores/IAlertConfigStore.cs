using TgLitePanel.Core.Abstractions.Models;

namespace TgLitePanel.Core.Abstractions.Stores;

/// <summary>
/// 告警配置存储接口
/// </summary>
public interface IAlertConfigStore
{
    /// <summary>
    /// 获取所有告警配置
    /// </summary>
    Task<IReadOnlyList<AlertConfig>> ListAsync(CancellationToken ct);

    /// <summary>
    /// 根据类型获取告警配置
    /// </summary>
    Task<AlertConfig?> GetByTypeAsync(string alertType, CancellationToken ct);

    /// <summary>
    /// 保存或更新告警配置
    /// </summary>
    Task<long> SaveAsync(AlertConfig config, CancellationToken ct);

    /// <summary>
    /// 删除告警配置
    /// </summary>
    Task DeleteAsync(long id, CancellationToken ct);

    /// <summary>
    /// 确保默认配置存在
    /// </summary>
    Task EnsureDefaultsAsync(CancellationToken ct);
}
