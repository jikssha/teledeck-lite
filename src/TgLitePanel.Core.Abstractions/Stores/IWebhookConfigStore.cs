using TgLitePanel.Core.Abstractions.Models;

namespace TgLitePanel.Core.Abstractions.Stores;

/// <summary>
/// Webhook 配置存储接口
/// </summary>
public interface IWebhookConfigStore
{
    Task<IReadOnlyList<WebhookConfig>> ListAsync(CancellationToken ct);
    Task<IReadOnlyList<WebhookConfig>> ListEnabledAsync(CancellationToken ct);
    Task<WebhookConfig?> GetAsync(long id, CancellationToken ct);
    Task<WebhookConfig> CreateAsync(WebhookConfig config, CancellationToken ct);
    Task UpdateAsync(WebhookConfig config, CancellationToken ct);
    Task DeleteAsync(long id, CancellationToken ct);
    Task UpdateLastTriggeredAsync(long id, DateTime triggeredAtUtc, string? error, CancellationToken ct);
}
