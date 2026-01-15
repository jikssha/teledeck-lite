namespace TgLitePanel.Core.Abstractions.Models;

/// <summary>
/// 告警类型
/// </summary>
public static class AlertTypes
{
    /// <summary>
    /// 账号离线告警
    /// </summary>
    public const string AccountOffline = "account_offline";

    /// <summary>
    /// 连续失败告警
    /// </summary>
    public const string ConsecutiveFailures = "consecutive_failures";
}

/// <summary>
/// 告警配置
/// </summary>
public sealed record AlertConfig
{
    public long Id { get; init; }

    /// <summary>
    /// 告警类型
    /// </summary>
    public required string AlertType { get; init; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; init; } = true;

    /// <summary>
    /// 连续失败次数阈值
    /// </summary>
    public int ConsecutiveFailureThreshold { get; init; } = 3;

    /// <summary>
    /// 告警冷却时间（分钟）
    /// </summary>
    public int CooldownMinutes { get; init; } = 30;

    /// <summary>
    /// 通知方式
    /// </summary>
    public string NotifyMethods { get; init; } = "webhook";

    /// <summary>
    /// 适用的账号 ID 列表
    /// </summary>
    public long[]? AccountIds { get; init; }

    /// <summary>
    /// 适用的分组 ID 列表
    /// </summary>
    public long[]? GroupIds { get; init; }

    public DateTime CreatedAtUtc { get; init; }
    public DateTime? UpdatedAtUtc { get; init; }
}
