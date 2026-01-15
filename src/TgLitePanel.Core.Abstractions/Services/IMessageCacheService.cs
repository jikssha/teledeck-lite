using TgLitePanel.Core.Abstractions.Models;

namespace TgLitePanel.Core.Abstractions.Services;

/// <summary>
/// 消息缓存服务接口
/// </summary>
public interface IMessageCacheService
{
    /// <summary>
    /// 获取消息（优先从缓存，缓存未命中则从远程获取并缓存）
    /// </summary>
    Task<IReadOnlyList<CachedMessage>> GetMessagesAsync(long accountId, long chatId, int limit, long? beforeMessageId, CancellationToken ct);

    /// <summary>
    /// 搜索消息
    /// </summary>
    Task<MessageSearchResult> SearchMessagesAsync(MessageSearchRequest request, CancellationToken ct);

    /// <summary>
    /// 刷新缓存（从远程重新获取）
    /// </summary>
    Task RefreshCacheAsync(long accountId, long chatId, int limit, CancellationToken ct);

    /// <summary>
    /// 添加新消息到缓存（实时推送时调用）
    /// </summary>
    Task OnNewMessageAsync(long accountId, long chatId, CachedMessage message, CancellationToken ct);
}
