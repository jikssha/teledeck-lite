using Microsoft.EntityFrameworkCore;
using TgLitePanel.Core.Abstractions.Exceptions;
using TgLitePanel.Core.Abstractions.Models;
using TgLitePanel.Core.Abstractions.Stores;
using TgLitePanel.Infrastructure.Persistence.Entities;

namespace TgLitePanel.Infrastructure.Persistence.Stores;

public sealed class EfAccountStore : IAccountStore
{
    private readonly AppDbContext _db;

    public EfAccountStore(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<AccountRuntimeConfig>> ListAsync(CancellationToken cancellationToken)
    {
        var list = await _db.Accounts.AsNoTracking()
            .OrderBy(x => x.Id)
            .Select(x => new AccountRuntimeConfig(x.Id, x.Phone, x.Status, x.DataDir, x.ApiIdOverride, x.SystemChatId, x.GroupId))
            .ToListAsync(cancellationToken);

        return list;
    }

    public async Task<(IReadOnlyList<AccountRuntimeConfig> items, int totalCount)> ListPagedAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = _db.Accounts.AsNoTracking();

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(x => x.Id)
            .Skip(page * pageSize)
            .Take(pageSize)
            .Select(x => new AccountRuntimeConfig(x.Id, x.Phone, x.Status, x.DataDir, x.ApiIdOverride, x.SystemChatId, x.GroupId))
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<AccountRuntimeConfig?> GetAsync(long accountId, CancellationToken cancellationToken)
    {
        return await _db.Accounts.AsNoTracking()
            .Where(x => x.Id == accountId)
            .Select(x => new AccountRuntimeConfig(x.Id, x.Phone, x.Status, x.DataDir, x.ApiIdOverride, x.SystemChatId, x.GroupId))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<long> CreateAsync(string phone, string dataDir, AccountStatus status, CancellationToken cancellationToken)
    {
        var entity = new AccountEntity
        {
            Phone = phone,
            Status = status,
            DataDir = dataDir
        };

        _db.Accounts.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }

    public async Task UpdateDataDirAsync(long accountId, string dataDir, CancellationToken cancellationToken)
    {
        try
        {
            var entity = await _db.Accounts.FirstOrDefaultAsync(x => x.Id == accountId, cancellationToken);
            if (entity is null)
                throw new NotFoundException($"账号 {accountId} 不存在");

            entity.DataDir = dataDir;
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new InvalidOperationException("数据已被其他用户修改，请刷新后重试");
        }
    }

    public async Task UpdateStatusAsync(long accountId, AccountStatus status, long? systemChatId, CancellationToken cancellationToken)
    {
        try
        {
            var entity = await _db.Accounts.FirstOrDefaultAsync(x => x.Id == accountId, cancellationToken);
            if (entity is null)
                throw new NotFoundException($"账号 {accountId} 不存在");

            entity.Status = status;
            entity.SystemChatId = systemChatId;
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new InvalidOperationException("数据已被其他用户修改，请刷新后重试");
        }
    }

    public async Task DeleteAsync(long accountId, CancellationToken cancellationToken)
    {
        try
        {
            var entity = await _db.Accounts.FirstOrDefaultAsync(x => x.Id == accountId, cancellationToken);
            if (entity is null)
                throw new NotFoundException($"账号 {accountId} 不存在");

            _db.Accounts.Remove(entity);
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new InvalidOperationException("数据已被其他用户修改，请刷新后重试");
        }
    }
}
