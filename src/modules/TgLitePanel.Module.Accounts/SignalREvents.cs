namespace TgLitePanel.Module.Accounts;

/// <summary>
/// 账号状态变更事件（用于 SignalR 通信）
/// </summary>
public sealed class AccountStatusEvent
{
    public long AccountId { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsOnline { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// 告警触发事件（用于 SignalR 通信）
/// </summary>
public sealed class AlertTriggeredEvent
{
    public long? AccountId { get; set; }
    public required string AlertType { get; set; }
    public required string Message { get; set; }
    public DateTime TriggeredAtUtc { get; set; }
}
