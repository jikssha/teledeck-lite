using Microsoft.EntityFrameworkCore;
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
        var entity = await _db.Accounts.FirstOrDefaultAsync(x => x.Id == accountId, cancellationToken);
        if (entity is null)
            return;

        entity.DataDir = dataDir;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateStatusAsync(long accountId, AccountStatus status, long? systemChatId, CancellationToken cancellationToken)
    {
        var entity = await _db.Accounts.FirstOrDefaultAsync(x => x.Id == accountId, cancellationToken);
        if (entity is null)
            return;

        entity.Status = status;
        entity.SystemChatId = systemChatId;
        await _db.SaveChangesAsync(cancellationToken);
    }
}
