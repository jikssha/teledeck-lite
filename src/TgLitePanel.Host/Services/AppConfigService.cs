using TgLitePanel.Core.Abstractions.Models;
using TgLitePanel.Core.Abstractions.Services;
using TgLitePanel.Core.Abstractions.Stores;

namespace TgLitePanel.Host.Services;

public sealed class AppConfigService : IAppConfigService
{
    private readonly IAppConfigStore _store;

    public AppConfigService(IAppConfigStore store) => _store = store;

    public Task<string?> GetStringAsync(string key, CancellationToken cancellationToken)
        => _store.GetStringAsync(key, cancellationToken);

    public Task SetStringAsync(string key, string value, CancellationToken cancellationToken)
        => _store.SetStringAsync(key, value, cancellationToken);

    public Task<TelegramApiConfigDto?> GetTelegramApiConfigAsync(CancellationToken cancellationToken)
        => _store.GetTelegramApiConfigAsync(cancellationToken);

    public Task SetTelegramApiConfigAsync(TelegramApiConfigDto config, CancellationToken cancellationToken)
        => _store.SetTelegramApiConfigAsync(config, cancellationToken);
}
