using System.Net.Http.Headers;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using TgLitePanel.Core.Abstractions.Services;
using TgLitePanel.Core.Abstractions.Stores;

namespace TgLitePanel.Host.Services;

/// <summary>
/// Webhook 服务实现
/// </summary>
public sealed class WebhookService : IWebhookService
{
    private const int MaxParallelSends = 8;
    private static readonly SemaphoreSlim SendConcurrency = new(MaxParallelSends, MaxParallelSends);

    private readonly IWebhookConfigStore _configStore;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WebhookService> _logger;

    public WebhookService(
        IWebhookConfigStore configStore,
        IHttpClientFactory httpClientFactory,
        ILogger<WebhookService> logger)
    {
        _configStore = configStore;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task TriggerNewMessageAsync(long accountId, long chatId, long messageId, string? content, CancellationToken ct)
    {
        var payload = new
        {
            @event = "new_message",
            timestamp = DateTime.UtcNow,
            data = new
            {
                account_id = accountId,
                chat_id = chatId,
                message_id = messageId,
                content
            }
        };

        await TriggerWebhooksAsync("new_message", accountId, payload, ct);
    }

    public async Task TriggerAccountStatusChangedAsync(long accountId, string status, string? error, CancellationToken ct)
    {
        var payload = new
        {
            @event = "account_status",
            timestamp = DateTime.UtcNow,
            data = new
            {
                account_id = accountId,
                status,
                error
            }
        };

        await TriggerWebhooksAsync("account_status", accountId, payload, ct);
    }

    public async Task TriggerLoginRequiredAsync(long accountId, string reason, CancellationToken ct)
    {
        var payload = new
        {
            @event = "login_required",
            timestamp = DateTime.UtcNow,
            data = new
            {
                account_id = accountId,
                reason
            }
        };

        await TriggerWebhooksAsync("login_required", accountId, payload, ct);
    }

    private async Task TriggerWebhooksAsync(string eventType, long accountId, object payload, CancellationToken ct)
    {
        var configs = await _configStore.ListEnabledAsync(ct);

        foreach (var config in configs)
        {
            // 检查事件类型
            if (!config.Events.Contains(eventType))
                continue;

            // 检查账号过滤
            if (config.AccountIds.Count > 0 && !config.AccountIds.Contains(accountId))
                continue;

            // 异步触发，不阻塞（受并发限制）
            _ = SendWebhookAsync(config.Id, config.Url, config.Secret, config.TimeoutSeconds, config.RetryCount, payload, ct);
        }
    }

    private async Task SendWebhookAsync(long configId, string url, string? secret, int timeoutSeconds, int maxRetries, object payload, CancellationToken ct)
    {
        if (!TryValidateWebhookUrl(url, out var normalizedUrl, out var urlError))
        {
            _logger.LogWarning("Webhook {ConfigId} URL 非法，已跳过: {Url} ({Error})", configId, url, urlError);
            await _configStore.UpdateLastTriggeredAsync(configId, DateTime.UtcNow, urlError, ct);
            return;
        }

        var jsonPayload = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        string? lastError = null;

        await SendConcurrency.WaitAsync(ct);
        try
        {
            for (int attempt = 0; attempt <= maxRetries; attempt++)
            {
                try
                {
                    using var client = _httpClientFactory.CreateClient();
                    client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);

                    using var request = new HttpRequestMessage(HttpMethod.Post, normalizedUrl);
                    request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                    // 添加签名
                    if (!string.IsNullOrEmpty(secret))
                    {
                        var signature = ComputeHmacSha256(jsonPayload, secret);
                        request.Headers.Add("X-Webhook-Signature", signature);
                    }

                    request.Headers.Add("X-Webhook-Event", "telegram");
                    request.Headers.Add("X-Webhook-Timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());

                    using var response = await client.SendAsync(request, ct);

                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogDebug("Webhook {ConfigId} 发送成功: {Url}", configId, normalizedUrl);
                        await _configStore.UpdateLastTriggeredAsync(configId, DateTime.UtcNow, null, ct);
                        return;
                    }

                    lastError = $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}";
                    _logger.LogWarning("Webhook {ConfigId} 发送失败 (尝试 {Attempt}/{MaxRetries}): {Error}",
                        configId, attempt + 1, maxRetries + 1, lastError);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    lastError = ex.Message;
                    _logger.LogWarning(ex, "Webhook {ConfigId} 发送异常 (尝试 {Attempt}/{MaxRetries})",
                        configId, attempt + 1, maxRetries + 1);
                }

                if (attempt < maxRetries)
                {
                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), ct); // 指数退避
                }
            }

            await _configStore.UpdateLastTriggeredAsync(configId, DateTime.UtcNow, lastError, ct);
        }
        finally
        {
            SendConcurrency.Release();
        }
    }

    private static bool TryValidateWebhookUrl(string url, out string normalizedUrl, out string error)
    {
        normalizedUrl = string.Empty;
        error = string.Empty;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            error = "URL 格式无效";
            return false;
        }

        if (uri.Scheme is not ("http" or "https"))
        {
            error = "仅允许 http/https";
            return false;
        }

        if (string.IsNullOrWhiteSpace(uri.Host))
        {
            error = "URL 缺少主机名";
            return false;
        }

        // 仅基于主机名做快速拦截（避免 DNS 解析带来复杂性/副作用）
        if (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
            uri.Host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase))
        {
            error = "禁止 localhost";
            return false;
        }

        if (IPAddress.TryParse(uri.Host, out var ip))
        {
            if (IPAddress.IsLoopback(ip) || IsPrivateIp(ip))
            {
                error = "禁止内网/回环 IP";
                return false;
            }
        }

        normalizedUrl = uri.ToString();
        return true;
    }

    private static string ComputeHmacSha256(string data, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static bool IsPrivateIp(IPAddress ip)
    {
        if (ip.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
            return false;

        var bytes = ip.GetAddressBytes();
        return bytes[0] switch
        {
            10 => true,
            127 => true,
            172 => bytes[1] is >= 16 and <= 31,
            192 => bytes[1] == 168,
            _ => false
        };
    }
}
