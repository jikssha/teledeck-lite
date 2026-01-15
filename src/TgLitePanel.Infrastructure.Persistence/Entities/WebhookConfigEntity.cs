namespace TgLitePanel.Infrastructure.Persistence.Entities;

/// <summary>
/// Webhook 配置实体
/// </summary>
public sealed class WebhookConfigEntity
{
    public long Id { get; set; }
    public required string Name { get; set; }
    public required string Url { get; set; }
    public string? Secret { get; set; }
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 触发事件类型（逗号分隔）：new_message,account_status,login_required
    /// </summary>
    public string Events { get; set; } = "new_message";

    /// <summary>
    /// 过滤的账号ID（逗号分隔，为空表示所有账号）
    /// </summary>
    public string? AccountIds { get; set; }

    public int RetryCount { get; set; } = 3;
    public int TimeoutSeconds { get; set; } = 30;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastTriggeredAtUtc { get; set; }
    public string? LastError { get; set; }
}
