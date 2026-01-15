namespace TgLitePanel.Core.Abstractions.Models;

/// <summary>
/// 消息搜索请求
/// </summary>
public sealed class MessageSearchRequest
{
    public long AccountId { get; set; }
    public long? ChatId { get; set; }
    public string? Keyword { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Limit { get; set; } = 50;
    public int Offset { get; set; } = 0;
}

/// <summary>
/// 消息搜索结果
/// </summary>
public sealed class MessageSearchResult
{
    public List<CachedMessage> Messages { get; set; } = new();
    public int TotalCount { get; set; }
}
