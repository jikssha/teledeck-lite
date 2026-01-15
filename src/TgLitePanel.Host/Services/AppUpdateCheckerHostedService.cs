using Microsoft.Extensions.Options;

namespace TgLitePanel.Host.Services;

/// <summary>
/// 周期性检查 GitHub 最新版本
/// </summary>
public sealed class AppUpdateCheckerHostedService : BackgroundService
{
    private readonly AppUpdateService _updateService;
    private readonly AppUpdateOptions _options;
    private readonly ILogger<AppUpdateCheckerHostedService> _logger;

    public AppUpdateCheckerHostedService(
        AppUpdateService updateService,
        IOptions<AppUpdateOptions> options,
        ILogger<AppUpdateCheckerHostedService> logger)
    {
        _updateService = updateService;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_updateService.IsEnabled)
        {
            _logger.LogInformation("未配置 AppUpdate:RepositoryUrl，跳过更新检查");
            return;
        }

        // 启动后先检查一次
        await SafeCheckAsync(stoppingToken);

        var intervalMinutes = Math.Max(5, _options.CheckIntervalMinutes);
        var interval = TimeSpan.FromMinutes(intervalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(interval, stoppingToken);
                await SafeCheckAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task SafeCheckAsync(CancellationToken ct)
    {
        try
        {
            await _updateService.CheckForUpdatesAsync(ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "更新检查异常");
        }
    }
}

