namespace TgLitePanel.Infrastructure.Persistence.Entities;

/// <summary>
/// 告警历史记录实体
/// </summary>
public sealed class AlertHistoryEntity
{
    public long Id { get; set; }
    public long? AccountId { get; set; }
    public required string AlertType { get; set; }
    public required string Message { get; set; }
    public string? Details { get; set; }

    /// <summary>
    /// 通知是否成功发送
    /// </summary>
    public bool NotificationSent { get; set; }
    public string? NotificationError { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
