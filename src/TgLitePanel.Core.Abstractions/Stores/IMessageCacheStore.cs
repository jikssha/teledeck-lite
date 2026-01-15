using TgLitePanel.Core.Abstractions.Models;

namespace TgLitePanel.Core.Abstractions.Stores;

/// <summary>
/// 消息缓存存储接口
/// </summary>
public interface IMessageCacheStore
{
    /// <summary>
    /// 缓存消息
    /// </summary>
    Task CacheMessagesAsync(long accountId, long chatId, IEnumerable<CachedMessage> messages, CancellationToken ct);

    /// <summary>
    /// 获取缓存的消息
    /// </summary>
    Task<IReadOnlyList<CachedMessage>> GetCachedMessagesAsync(long accountId, long chatId, int limit, long? beforeMessageId, CancellationToken ct);

    /// <summary>
    /// 搜索消息
    /// </summary>
    Task<MessageSearchResult> SearchAsync(MessageSearchRequest request, CancellationToken ct);

    /// <summary>
    /// 添加单条消息到缓存
    /// </summary>
    Task AddMessageAsync(CachedMessage message, CancellationToken ct);

    /// <summary>
    /// 清除账号的所有缓存
    /// </summary>
    Task ClearAccountCacheAsync(long accountId, CancellationToken ct);

    /// <summary>
    /// 清除指定聊天的缓存
    /// </summary>
    Task ClearChatCacheAsync(long accountId, long chatId, CancellationToken ct);
}
