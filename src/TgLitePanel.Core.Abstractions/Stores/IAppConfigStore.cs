using TgLitePanel.Core.Abstractions.Models;

namespace TgLitePanel.Core.Abstractions.Stores;

public interface IAppConfigStore
{
    Task<string?> GetStringAsync(string key, CancellationToken cancellationToken);
    Task SetStringAsync(string key, string value, CancellationToken cancellationToken);

    Task<TelegramApiConfigDto?> GetTelegramApiConfigAsync(CancellationToken cancellationToken);
    Task SetTelegramApiConfigAsync(TelegramApiConfigDto config, CancellationToken cancellationToken);
}
