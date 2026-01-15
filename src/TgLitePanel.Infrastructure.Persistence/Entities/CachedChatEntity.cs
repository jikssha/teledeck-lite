namespace TgLitePanel.Infrastructure.Persistence.Entities;

/// <summary>
/// 缓存的聊天实体
/// </summary>
public sealed class CachedChatEntity
{
    public long Id { get; set; }
    public long AccountId { get; set; }
    public long ChatId { get; set; }
    public required string Title { get; set; }
    public string? ChatType { get; set; }
    public int UnreadCount { get; set; }
    public long? LastMessageId { get; set; }
    public string? LastMessagePreview { get; set; }
    public DateTime? LastMessageDateUtc { get; set; }
    public DateTime CachedAtUtc { get; set; } = DateTime.UtcNow;
}
