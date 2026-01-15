using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TgLitePanel.Infrastructure.WTelegram;

/// <summary>
/// 后台服务，定期清理空闲的 WTelegram 客户端
/// </summary>
public sealed class WTelegramClientReaperHostedService : BackgroundService
{
    private readonly WTelegramClientManager _manager;
    private readonly ILogger<WTelegramClientReaperHostedService> _logger;
    private readonly TimeSpan _interval;

    public WTelegramClientReaperHostedService(
        WTelegramClientManager manager,
        IOptions<WTelegramRuntimeOptions> options,
        ILogger<WTelegramClientReaperHostedService> logger)
    {
        _manager = manager;
        _logger = logger;
        _interval = options.Value.ReapInterval;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("WTelegram 客户端清理服务已启动，清理间隔: {Interval}秒", _interval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_interval, stoppingToken);
                await _manager.ReapExpiredAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("WTelegram 客户端清理服务正在停止");
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "WTelegram 客户端清理服务异常");
            }
        }
    }
}
