using TgLitePanel.Core.Abstractions.Models;
using TgLitePanel.Core.Abstractions.Services;
using TgLitePanel.Core.Abstractions.Stores;

namespace TgLitePanel.Host.Services;

public sealed class AppConfigService : IAppConfigService
{
    private readonly IAppConfigStore _store;

    public AppConfigService(IAppConfigStore store) => _store = store;

    public Task<TelegramApiConfigDto?> GetTelegramApiConfigAsync(CancellationToken cancellationToken)
        => _store.GetTelegramApiConfigAsync(cancellationToken);

    public Task SetTelegramApiConfigAsync(TelegramApiConfigDto config, CancellationToken cancellationToken)
        => _store.SetTelegramApiConfigAsync(config, cancellationToken);
}

