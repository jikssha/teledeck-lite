using TgLitePanel.Core.Abstractions.Models;

namespace TgLitePanel.Core.Abstractions.Services;

/// <summary>
/// 账号分组服务接口
/// </summary>
public interface IAccountGroupService
{
    Task<IReadOnlyList<AccountGroup>> GetGroupsAsync(CancellationToken ct);
    Task<AccountGroup?> GetGroupAsync(long id, CancellationToken ct);
    Task<AccountGroup> CreateGroupAsync(string name, string? description, string color, CancellationToken ct);
    Task UpdateGroupAsync(long id, string name, string? description, string color, int sortOrder, CancellationToken ct);
    Task DeleteGroupAsync(long id, CancellationToken ct);
    Task SetAccountGroupAsync(long accountId, long? groupId, CancellationToken ct);
    Task<IReadOnlyList<Account>> GetAccountsByGroupAsync(long? groupId, CancellationToken ct);
}
