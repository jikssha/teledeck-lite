using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace TgLitePanel.Host.Hubs;

/// <summary>
/// Telegram 消息实时推送 Hub
/// </summary>
[Authorize]
public sealed class TelegramHub : Hub
{
    private readonly ILogger<TelegramHub> _logger;

    public TelegramHub(ILogger<TelegramHub> logger) => _logger = logger;

    /// <summary>
    /// 订阅账号的消息更新
    /// </summary>
    public async Task SubscribeAccount(long accountId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"account:{accountId}");
        _logger.LogDebug("客户端 {ConnectionId} 订阅账号 {AccountId}", Context.ConnectionId, accountId);
    }

    /// <summary>
    /// 取消订阅账号
    /// </summary>
    public async Task UnsubscribeAccount(long accountId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"account:{accountId}");
        _logger.LogDebug("客户端 {ConnectionId} 取消订阅账号 {AccountId}", Context.ConnectionId, accountId);
    }

    /// <summary>
    /// 订阅聊天的消息更新
    /// </summary>
    public async Task SubscribeChat(long accountId, long chatId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"chat:{accountId}:{chatId}");
        _logger.LogDebug("客户端 {ConnectionId} 订阅聊天 {AccountId}:{ChatId}", Context.ConnectionId, accountId, chatId);
    }

    /// <summary>
    /// 取消订阅聊天
    /// </summary>
    public async Task UnsubscribeChat(long accountId, long chatId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"chat:{accountId}:{chatId}");
        _logger.LogDebug("客户端 {ConnectionId} 取消订阅聊天 {AccountId}:{ChatId}", Context.ConnectionId, accountId, chatId);
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogDebug("客户端 {ConnectionId} 已连接", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogDebug("客户端 {ConnectionId} 已断开: {Exception}", Context.ConnectionId, exception?.Message);
        await base.OnDisconnectedAsync(exception);
    }
}

/// <summary>
/// SignalR 推送的消息类型
/// </summary>
public static class TelegramHubMethods
{
    public const string NewMessage = "NewMessage";
    public const string ChatListUpdated = "ChatListUpdated";
    public const string AccountStatusChanged = "AccountStatusChanged";
    public const string LoginRequired = "LoginRequired";
    public const string HealthCheckProgress = "HealthCheckProgress";
    public const string AlertTriggered = "AlertTriggered";
}

/// <summary>
/// 新消息事件
/// </summary>
public sealed class NewMessageEvent
{
    public long AccountId { get; set; }
    public long ChatId { get; set; }
    public long MessageId { get; set; }
    public string? SenderName { get; set; }
    public string? Content { get; set; }
    public bool IsOutgoing { get; set; }
    public DateTime MessageDateUtc { get; set; }
}

/// <summary>
/// 账号状态变更事件
/// </summary>
public sealed class AccountStatusEvent
{
    public long AccountId { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsOnline { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// 告警触发事件
/// </summary>
public sealed class AlertTriggeredEvent
{
    public long? AccountId { get; set; }
    public required string AlertType { get; set; }
    public required string Message { get; set; }
    public DateTime TriggeredAtUtc { get; set; }
}
