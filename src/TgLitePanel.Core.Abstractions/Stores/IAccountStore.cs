using TgLitePanel.Core.Abstractions.Models;

namespace TgLitePanel.Core.Abstractions.Stores;

public interface IAccountStore
{
    Task<IReadOnlyList<AccountRuntimeConfig>> ListAsync(CancellationToken cancellationToken);
    Task<AccountRuntimeConfig?> GetAsync(long accountId, CancellationToken cancellationToken);
    Task<long> CreateAsync(string phone, string dataDir, AccountStatus status, CancellationToken cancellationToken);

    Task UpdateDataDirAsync(long accountId, string dataDir, CancellationToken cancellationToken);
    Task UpdateStatusAsync(long accountId, AccountStatus status, long? systemChatId, CancellationToken cancellationToken);
}
