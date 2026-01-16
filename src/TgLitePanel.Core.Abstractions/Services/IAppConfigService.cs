using TgLitePanel.Core.Abstractions.Models;

namespace TgLitePanel.Core.Abstractions.Services;

public interface IAppConfigService
{
    Task<string?> GetStringAsync(string key, CancellationToken cancellationToken);
    Task SetStringAsync(string key, string value, CancellationToken cancellationToken);

    Task<TelegramApiConfigDto?> GetTelegramApiConfigAsync(CancellationToken cancellationToken);
    Task SetTelegramApiConfigAsync(TelegramApiConfigDto config, CancellationToken cancellationToken);
}
