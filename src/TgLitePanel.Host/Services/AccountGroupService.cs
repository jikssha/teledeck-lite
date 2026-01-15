using TgLitePanel.Core.Abstractions.Models;
using TgLitePanel.Core.Abstractions.Services;
using TgLitePanel.Core.Abstractions.Stores;

namespace TgLitePanel.Host.Services;

/// <summary>
/// 账号分组服务实现
/// </summary>
public sealed class AccountGroupService : IAccountGroupService
{
    private readonly IAccountGroupStore _store;
    private readonly IAccountStore _accountStore;
    private readonly ILogger<AccountGroupService> _logger;

    public AccountGroupService(
        IAccountGroupStore store,
        IAccountStore accountStore,
        ILogger<AccountGroupService> logger)
    {
        _store = store;
        _accountStore = accountStore;
        _logger = logger;
    }

    public async Task<IReadOnlyList<AccountGroup>> GetGroupsAsync(CancellationToken ct)
    {
        return await _store.ListAsync(ct);
    }

    public async Task<AccountGroup?> GetGroupAsync(long id, CancellationToken ct)
    {
        return await _store.GetAsync(id, ct);
    }

    public async Task<AccountGroup> CreateGroupAsync(string name, string? description, string color, CancellationToken ct)
    {
        _logger.LogInformation("创建分组: {Name}", name);
        return await _store.CreateAsync(name, description, color, ct);
    }

    public async Task UpdateGroupAsync(long id, string name, string? description, string color, int sortOrder, CancellationToken ct)
    {
        _logger.LogInformation("更新分组 {Id}: {Name}", id, name);
        await _store.UpdateAsync(id, name, description, color, sortOrder, ct);
    }

    public async Task DeleteGroupAsync(long id, CancellationToken ct)
    {
        _logger.LogInformation("删除分组: {Id}", id);
        await _store.DeleteAsync(id, ct);
    }

    public async Task SetAccountGroupAsync(long accountId, long? groupId, CancellationToken ct)
    {
        _logger.LogInformation("设置账号 {AccountId} 分组为 {GroupId}", accountId, groupId);
        await _store.SetAccountGroupAsync(accountId, groupId, ct);
    }

    public async Task<IReadOnlyList<Account>> GetAccountsByGroupAsync(long? groupId, CancellationToken ct)
    {
        var allAccounts = await _accountStore.ListAsync(ct);
        // 这里需要扩展 IAccountStore 来支持按分组查询
        // 暂时返回空列表
        return new List<Account>();
    }
}
