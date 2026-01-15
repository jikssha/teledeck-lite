using TgLitePanel.Core.Abstractions.Models;

namespace TgLitePanel.Core.Abstractions.Stores;

public interface IAppConfigStore
{
    Task<TelegramApiConfigDto?> GetTelegramApiConfigAsync(CancellationToken cancellationToken);
    Task SetTelegramApiConfigAsync(TelegramApiConfigDto config, CancellationToken cancellationToken);
}

