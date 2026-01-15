using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using TgLitePanel.Core.Abstractions.Models;
using TgLitePanel.Core.Abstractions.Services;
using TgLitePanel.Core.Abstractions.Stores;
using TgLitePanel.Core.Abstractions.TdLib;
using TgLitePanel.Host.Hubs;

namespace TgLitePanel.Host.Services;

/// <summary>
/// 账号状态监控后台服务（增强版）
/// </summary>
public sealed class AccountHealthCheckService : BackgroundService, IAccountHealthCheckService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ITdClientManager _clientManager;
    private readonly ILogger<AccountHealthCheckService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(2);

    private readonly ConcurrentQueue<HealthCheckRequest> _pendingRequests = new();
    private HealthCheckProgressEvent? _currentProgress;
    private readonly object _progressLock = new();

    public bool IsRunning => _currentProgress is { IsFinished: false };

    public AccountHealthCheckService(
        IServiceScopeFactory scopeFactory,
        ITdClientManager clientManager,
        ILogger<AccountHealthCheckService> logger)
    {
        _scopeFactory = scopeFactory;
        _clientManager = clientManager;
        _logger = logger;
    }

    public Task<string> TriggerCheckAsync(HealthCheckRequest request, CancellationToken ct)
    {
        var batchId = Guid.NewGuid().ToString("N")[..8];
        var requestWithId = request with { BatchId = batchId };
        _pendingRequests.Enqueue(requestWithId);
        _logger.LogInformation("手动健康检查已加入队列，批次 ID: {BatchId}", batchId);
        return Task.FromResult(batchId);
    }

    public HealthCheckProgressEvent? GetCurrentProgress() => _currentProgress;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("账号状态监控服务已启动，检查间隔: {Interval}", _checkInterval);

        var lastAutoCheck = DateTime.UtcNow;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // 优先处理手动请求
                if (_pendingRequests.TryDequeue(out var manualRequest))
                {
                    await CheckAccountsAsync(manualRequest, stoppingToken);
                }
                // 定时自动检查
                else if (DateTime.UtcNow - lastAutoCheck >= _checkInterval)
                {
                    await CheckAccountsAsync(new HealthCheckRequest { Source = "auto" }, stoppingToken);
                    lastAutoCheck = DateTime.UtcNow;
                }
                else
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "账号状态检查异常");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        _logger.LogInformation("账号状态监控服务已停止");
    }

    private async Task CheckAccountsAsync(HealthCheckRequest request, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var accountStore = scope.ServiceProvider.GetRequiredService<IAccountStore>();
        var statusLogStore = scope.ServiceProvider.GetRequiredService<IAccountStatusLogStore>();
        var alertConfigStore = scope.ServiceProvider.GetRequiredService<IAlertConfigStore>();
        var alertHistoryStore = scope.ServiceProvider.GetRequiredService<IAlertHistoryStore>();
        var webhookService = scope.ServiceProvider.GetRequiredService<IWebhookService>();
        var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<TelegramHub>>();

        // 获取要检查的账号列表
        var allAccounts = await accountStore.ListAsync(ct);
        var accounts = FilterAccounts(allAccounts, request);

        if (accounts.Count == 0)
        {
            _logger.LogDebug("没有需要检查的账号");
            return;
        }

        var batchId = string.IsNullOrWhiteSpace(request.BatchId)
            ? Guid.NewGuid().ToString("N")[..8]
            : request.BatchId;
        var startedAt = DateTime.UtcNow;
        var onlineCount = 0;
        var offlineCount = 0;

        _logger.LogInformation("开始健康检查，批次 ID: {BatchId}，账号数: {Count}，来源: {Source}",
            batchId, accounts.Count, request.Source);

        // 获取告警配置
        var alertConfigs = await alertConfigStore.ListAsync(ct);

        for (var i = 0; i < accounts.Count; i++)
        {
            var account = accounts[i];

            // 更新进度
            lock (_progressLock)
            {
                _currentProgress = new HealthCheckProgressEvent
                {
                    BatchId = batchId,
                    CurrentAccountId = account.AccountId,
                    Completed = i,
                    Total = accounts.Count,
                    IsFinished = false
                };
            }

            // 推送进度到前端
            await hubContext.Clients.All.SendAsync(
                TelegramHubMethods.HealthCheckProgress,
                _currentProgress,
                ct);

            try
            {
                var (isOnline, error) = await CheckSingleAccountAsync(account.AccountId, ct);

                // 写入状态日志
                await statusLogStore.WriteAsync(account.AccountId, isOnline, error, request.Source, ct);

                if (isOnline)
                    onlineCount++;
                else
                    offlineCount++;

                // 推送状态更新
                var statusEvent = new AccountStatusEvent
                {
                    AccountId = account.AccountId,
                    Status = isOnline ? "online" : "offline",
                    IsOnline = isOnline,
                    Error = error
                };
                await hubContext.Clients.All.SendAsync(TelegramHubMethods.AccountStatusChanged, statusEvent, ct);

                // 检查告警
                if (!isOnline)
                {
                    await CheckAndTriggerAlertsAsync(
                        account.AccountId,
                        error,
                        alertConfigs,
                        statusLogStore,
                        alertHistoryStore,
                        webhookService,
                        hubContext,
                        ct);
                }

                // 更新进度（包含结果）
                lock (_progressLock)
                {
                    _currentProgress = new HealthCheckProgressEvent
                    {
                        BatchId = batchId,
                        CurrentAccountId = account.AccountId,
                        Completed = i + 1,
                        Total = accounts.Count,
                        IsFinished = false,
                        IsOnline = isOnline,
                        Error = error
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "检查账号 {AccountId} 状态失败", account.AccountId);
                await statusLogStore.WriteAsync(account.AccountId, false, ex.Message, request.Source, ct);
                offlineCount++;
            }
        }

        // 完成
        lock (_progressLock)
        {
            _currentProgress = new HealthCheckProgressEvent
            {
                BatchId = batchId,
                CurrentAccountId = 0,
                Completed = accounts.Count,
                Total = accounts.Count,
                IsFinished = true
            };
        }

        await hubContext.Clients.All.SendAsync(TelegramHubMethods.HealthCheckProgress, _currentProgress, ct);

        _logger.LogInformation(
            "健康检查完成，批次 ID: {BatchId}，在线: {Online}，离线: {Offline}，耗时: {Duration}",
            batchId, onlineCount, offlineCount, DateTime.UtcNow - startedAt);
    }

    private static IReadOnlyList<AccountRuntimeConfig> FilterAccounts(
        IReadOnlyList<AccountRuntimeConfig> allAccounts,
        HealthCheckRequest request)
    {
        var accounts = allAccounts.AsEnumerable();

        // 按账号 ID 过滤
        if (request.AccountIds is { Length: > 0 })
        {
            var ids = request.AccountIds.ToHashSet();
            accounts = accounts.Where(a => ids.Contains(a.AccountId));
        }

        // 按分组过滤
        if (request.GroupIds is { Length: > 0 })
        {
            var groupIds = request.GroupIds.ToHashSet();
            accounts = accounts.Where(a => a.GroupId.HasValue && groupIds.Contains(a.GroupId.Value));
        }

        return accounts.ToList();
    }

    private async Task<(bool isOnline, string? error)> CheckSingleAccountAsync(long accountId, CancellationToken ct)
    {
        try
        {
            await using var lease = await _clientManager.AcquireAsync(accountId, ct);

            var result = await lease.Client.ExecuteAsync(
                """{"@type":"getMe"}""",
                TimeSpan.FromSeconds(10),
                ct);

            var isOnline = !IsTdErrorJson(result, out var errorMessage);
            return (isOnline, isOnline ? null : (string.IsNullOrWhiteSpace(errorMessage) ? "API 返回错误" : errorMessage));
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private static bool IsTdErrorJson(string json, out string? errorMessage)
    {
        errorMessage = null;

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("@type", out var typeEl))
                return false;

            if (!string.Equals(typeEl.GetString(), "error", StringComparison.Ordinal))
                return false;

            if (doc.RootElement.TryGetProperty("message", out var msgEl))
                errorMessage = msgEl.GetString();

            return true;
        }
        catch
        {
            // 非 JSON 或格式异常时，保守处理为“非明确 error”
            return false;
        }
    }

    private async Task CheckAndTriggerAlertsAsync(
        long accountId,
        string? error,
        IReadOnlyList<AlertConfig> alertConfigs,
        IAccountStatusLogStore statusLogStore,
        IAlertHistoryStore alertHistoryStore,
        IWebhookService webhookService,
        IHubContext<TelegramHub> hubContext,
        CancellationToken ct)
    {
        foreach (var config in alertConfigs.Where(c => c.IsEnabled))
        {
            // 检查是否适用于此账号
            if (config.AccountIds is { Length: > 0 } && !config.AccountIds.Contains(accountId))
                continue;

            // 检查冷却时间
            var lastAlert = await alertHistoryStore.GetLastAlertTimeAsync(accountId, config.AlertType, ct);
            if (lastAlert.HasValue && (DateTime.UtcNow - lastAlert.Value).TotalMinutes < config.CooldownMinutes)
                continue;

            var shouldAlert = false;
            string alertMessage;

            switch (config.AlertType)
            {
                case AlertTypes.AccountOffline:
                    shouldAlert = true;
                    alertMessage = $"账号 {accountId} 已离线";
                    break;

                case AlertTypes.ConsecutiveFailures:
                    var failureCount = await statusLogStore.GetConsecutiveFailureCountAsync(accountId, ct);
                    if (failureCount >= config.ConsecutiveFailureThreshold)
                    {
                        shouldAlert = true;
                        alertMessage = $"账号 {accountId} 连续 {failureCount} 次检查失败";
                    }
                    else
                    {
                        alertMessage = string.Empty;
                    }
                    break;

                default:
                    continue;
            }

            if (!shouldAlert)
                continue;

            // 触发告警
            var notificationSent = false;
            string? notificationError = null;

            try
            {
                if (config.NotifyMethods.Contains("webhook"))
                {
                    await webhookService.TriggerAccountStatusChangedAsync(accountId, "offline", error, ct);
                    notificationSent = true;
                }
            }
            catch (Exception ex)
            {
                notificationError = ex.Message;
            }

            // 记录告警历史
            await alertHistoryStore.WriteAsync(
                accountId,
                config.AlertType,
                alertMessage,
                error,
                notificationSent,
                notificationError,
                ct);

            // 推送告警到前端
            var alertEvent = new AlertTriggeredEvent
            {
                AccountId = accountId,
                AlertType = config.AlertType,
                Message = alertMessage,
                TriggeredAtUtc = DateTime.UtcNow
            };
            await hubContext.Clients.All.SendAsync(TelegramHubMethods.AlertTriggered, alertEvent, ct);

            _logger.LogWarning("告警触发: {AlertType} - {Message}", config.AlertType, alertMessage);
        }
    }
}
