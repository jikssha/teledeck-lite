namespace TgLitePanel.Core.Abstractions.Models;

/// <summary>
/// 缓存的消息
/// </summary>
public sealed class CachedMessage
{
    public long Id { get; set; }
    public long AccountId { get; set; }
    public long ChatId { get; set; }
    public long MessageId { get; set; }
    public long SenderId { get; set; }
    public string? SenderName { get; set; }
    public string? Content { get; set; }
    public string? MessageType { get; set; }
    public bool IsOutgoing { get; set; }
    public DateTime MessageDateUtc { get; set; }
}
