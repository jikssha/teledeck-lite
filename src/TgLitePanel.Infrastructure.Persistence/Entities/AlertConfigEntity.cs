namespace TgLitePanel.Infrastructure.Persistence.Entities;

/// <summary>
/// 告警配置实体
/// </summary>
public sealed class AlertConfigEntity
{
    public long Id { get; set; }

    /// <summary>
    /// 告警类型：account_offline（账号离线）、consecutive_failures（连续失败）
    /// </summary>
    public required string AlertType { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 连续失败次数阈值（触发告警前需连续失败多少次）
    /// </summary>
    public int ConsecutiveFailureThreshold { get; set; } = 3;

    /// <summary>
    /// 告警冷却时间（分钟），避免重复告警
    /// </summary>
    public int CooldownMinutes { get; set; } = 30;

    /// <summary>
    /// 通知方式：webhook、email（逗号分隔）
    /// </summary>
    public string NotifyMethods { get; set; } = "webhook";

    /// <summary>
    /// 适用的账号 ID 列表（JSON 数组，空表示全部）
    /// </summary>
    public string? AccountIdsJson { get; set; }

    /// <summary>
    /// 适用的分组 ID 列表（JSON 数组，空表示全部）
    /// </summary>
    public string? GroupIdsJson { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}
