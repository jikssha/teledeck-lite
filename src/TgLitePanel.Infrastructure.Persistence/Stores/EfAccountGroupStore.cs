using Microsoft.EntityFrameworkCore;
using TgLitePanel.Core.Abstractions.Models;
using TgLitePanel.Core.Abstractions.Stores;
using TgLitePanel.Infrastructure.Persistence.Entities;

namespace TgLitePanel.Infrastructure.Persistence.Stores;

public sealed class EfAccountGroupStore : IAccountGroupStore
{
    private readonly AppDbContext _db;

    public EfAccountGroupStore(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<AccountGroup>> ListAsync(CancellationToken ct)
    {
        var groups = await _db.AccountGroups.AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Id)
            .ToListAsync(ct);

        var accountCounts = await _db.Accounts
            .Where(a => a.GroupId != null)
            .GroupBy(a => a.GroupId)
            .Select(g => new { GroupId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.GroupId!.Value, x => x.Count, ct);

        return groups.Select(g => new AccountGroup
        {
            Id = g.Id,
            Name = g.Name,
            Description = g.Description,
            Color = g.Color,
            SortOrder = g.SortOrder,
            AccountCount = accountCounts.GetValueOrDefault(g.Id, 0)
        }).ToList();
    }

    public async Task<AccountGroup?> GetAsync(long id, CancellationToken ct)
    {
        var entity = await _db.AccountGroups.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (entity is null)
            return null;

        var count = await _db.Accounts.CountAsync(a => a.GroupId == id, ct);

        return new AccountGroup
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            Color = entity.Color,
            SortOrder = entity.SortOrder,
            AccountCount = count
        };
    }

    public async Task<AccountGroup> CreateAsync(string name, string? description, string color, CancellationToken ct)
    {
        var maxOrder = await _db.AccountGroups.MaxAsync(x => (int?)x.SortOrder, ct) ?? 0;

        var entity = new AccountGroupEntity
        {
            Name = name,
            Description = description,
            Color = color,
            SortOrder = maxOrder + 1
        };

        _db.AccountGroups.Add(entity);
        await _db.SaveChangesAsync(ct);

        return new AccountGroup
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            Color = entity.Color,
            SortOrder = entity.SortOrder,
            AccountCount = 0
        };
    }

    public async Task UpdateAsync(long id, string name, string? description, string color, int sortOrder, CancellationToken ct)
    {
        var entity = await _db.AccountGroups.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null)
            return;

        entity.Name = name;
        entity.Description = description;
        entity.Color = color;
        entity.SortOrder = sortOrder;
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(long id, CancellationToken ct)
    {
        // 先将该分组下的账号设为无分组
        var accounts = await _db.Accounts.Where(a => a.GroupId == id).ToListAsync(ct);
        foreach (var account in accounts)
        {
            account.GroupId = null;
        }

        var entity = await _db.AccountGroups.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is not null)
        {
            _db.AccountGroups.Remove(entity);
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task SetAccountGroupAsync(long accountId, long? groupId, CancellationToken ct)
    {
        var account = await _db.Accounts.FirstOrDefaultAsync(a => a.Id == accountId, ct);
        if (account is null)
            return;

        account.GroupId = groupId;
        await _db.SaveChangesAsync(ct);
    }
}
