namespace TgLitePanel.Core.Abstractions.Services;

/// <summary>
/// Webhook 服务接口
/// </summary>
public interface IWebhookService
{
    /// <summary>
    /// 触发新消息事件
    /// </summary>
    Task TriggerNewMessageAsync(long accountId, long chatId, long messageId, string? content, CancellationToken ct);

    /// <summary>
    /// 触发账号状态变更事件
    /// </summary>
    Task TriggerAccountStatusChangedAsync(long accountId, string status, string? error, CancellationToken ct);

    /// <summary>
    /// 触发需要登录事件
    /// </summary>
    Task TriggerLoginRequiredAsync(long accountId, string reason, CancellationToken ct);
}
