using TgLitePanel.Core.Abstractions.Models;

namespace TgLitePanel.Core.Abstractions.Stores;

/// <summary>
/// 账号分组存储接口
/// </summary>
public interface IAccountGroupStore
{
    Task<IReadOnlyList<AccountGroup>> ListAsync(CancellationToken ct);
    Task<AccountGroup?> GetAsync(long id, CancellationToken ct);
    Task<AccountGroup> CreateAsync(string name, string? description, string color, CancellationToken ct);
    Task UpdateAsync(long id, string name, string? description, string color, int sortOrder, CancellationToken ct);
    Task DeleteAsync(long id, CancellationToken ct);
    Task SetAccountGroupAsync(long accountId, long? groupId, CancellationToken ct);
}
