using Microsoft.AspNetCore.SignalR;
using TgLitePanel.Core.Abstractions.Models;
using TgLitePanel.Core.Abstractions.Services;
using TgLitePanel.Core.Abstractions.Stores;
using TgLitePanel.Host.Hubs;

namespace TgLitePanel.Host.Services;

/// <summary>
/// 消息缓存服务实现
/// </summary>
public sealed class MessageCacheService : IMessageCacheService
{
    private readonly IMessageCacheStore _store;
    private readonly IChatService _chatService;
    private readonly IHubContext<TelegramHub> _hubContext;
    private readonly ILogger<MessageCacheService> _logger;

    public MessageCacheService(
        IMessageCacheStore store,
        IChatService chatService,
        IHubContext<TelegramHub> hubContext,
        ILogger<MessageCacheService> logger)
    {
        _store = store;
        _chatService = chatService;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task<IReadOnlyList<CachedMessage>> GetMessagesAsync(long accountId, long chatId, int limit, long? beforeMessageId, CancellationToken ct)
    {
        // 先尝试从缓存获取
        var cached = await _store.GetCachedMessagesAsync(accountId, chatId, limit, beforeMessageId, ct);

        if (cached.Count >= limit)
        {
            return cached;
        }

        // 缓存不足，从远程获取并缓存
        try
        {
            var messages = await _chatService.GetHistoryAsync(accountId, chatId, limit, ct);
            var cachedMessages = messages.Select(m => new CachedMessage
            {
                AccountId = accountId,
                ChatId = chatId,
                MessageId = m.MessageId,
                SenderId = 0, // ChatMessageDto doesn't include sender info
                SenderName = null,
                Content = m.Text,
                MessageType = "text",
                IsOutgoing = m.IsOutgoing,
                MessageDateUtc = m.Date.UtcDateTime
            }).ToList();

            await _store.CacheMessagesAsync(accountId, chatId, cachedMessages, ct);

            return cachedMessages;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "无法从远程获取消息，返回缓存数据");
            return cached;
        }
    }

    public async Task<MessageSearchResult> SearchMessagesAsync(MessageSearchRequest request, CancellationToken ct)
    {
        return await _store.SearchAsync(request, ct);
    }

    public async Task RefreshCacheAsync(long accountId, long chatId, int limit, CancellationToken ct)
    {
        // 清除旧缓存
        await _store.ClearChatCacheAsync(accountId, chatId, ct);

        // 重新获取
        await GetMessagesAsync(accountId, chatId, limit, null, ct);
    }

    public async Task OnNewMessageAsync(long accountId, long chatId, CachedMessage message, CancellationToken ct)
    {
        // 缓存消息
        await _store.AddMessageAsync(message, ct);

        // 推送到 SignalR
        var evt = new NewMessageEvent
        {
            AccountId = accountId,
            ChatId = chatId,
            MessageId = message.MessageId,
            SenderName = message.SenderName,
            Content = message.Content,
            IsOutgoing = message.IsOutgoing,
            MessageDateUtc = message.MessageDateUtc
        };

        // 推送到订阅该聊天的客户端
        await _hubContext.Clients.Group($"chat:{accountId}:{chatId}").SendAsync(TelegramHubMethods.NewMessage, evt, ct);

        // 同时推送到订阅该账号的客户端
        await _hubContext.Clients.Group($"account:{accountId}").SendAsync(TelegramHubMethods.NewMessage, evt, ct);
    }
}
