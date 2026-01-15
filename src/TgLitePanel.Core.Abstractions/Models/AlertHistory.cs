namespace TgLitePanel.Core.Abstractions.Models;

/// <summary>
/// 告警历史记录
/// </summary>
public sealed record AlertHistory
{
    public long Id { get; init; }
    public long? AccountId { get; init; }
    public required string AlertType { get; init; }
    public required string Message { get; init; }
    public string? Details { get; init; }
    public bool NotificationSent { get; init; }
    public string? NotificationError { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}
