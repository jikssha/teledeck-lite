using TgLitePanel.Core.Abstractions.Models;

namespace TgLitePanel.Core.Abstractions.Services;

public interface IAppConfigService
{
    Task<TelegramApiConfigDto?> GetTelegramApiConfigAsync(CancellationToken cancellationToken);
    Task SetTelegramApiConfigAsync(TelegramApiConfigDto config, CancellationToken cancellationToken);
}

