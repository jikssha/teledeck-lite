using Microsoft.EntityFrameworkCore;
using TgLitePanel.Core.Abstractions.Models;
using TgLitePanel.Core.Abstractions.Stores;
using TgLitePanel.Infrastructure.Persistence.Entities;

namespace TgLitePanel.Infrastructure.Persistence.Stores;

public sealed class EfAccountStatusLogStore : IAccountStatusLogStore
{
    private readonly AppDbContext _db;

    public EfAccountStatusLogStore(AppDbContext db) => _db = db;

    public async Task WriteAsync(long accountId, bool isOnline, string? error, string source, CancellationToken ct)
    {
        var entity = new AccountStatusLogEntity
        {
            AccountId = accountId,
            IsOnline = isOnline,
            Error = error,
            Source = source,
            CheckedAtUtc = DateTime.UtcNow
        };

        _db.AccountStatusLogs.Add(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<AccountStatusLog>> GetByAccountAsync(long accountId, int limit, CancellationToken ct)
    {
        var logs = await _db.AccountStatusLogs.AsNoTracking()
            .Where(x => x.AccountId == accountId)
            .OrderByDescending(x => x.CheckedAtUtc)
            .Take(limit)
            .ToListAsync(ct);

        return logs.Select(x => new AccountStatusLog
        {
            Id = x.Id,
            AccountId = x.AccountId,
            IsOnline = x.IsOnline,
            Error = x.Error,
            CheckedAtUtc = x.CheckedAtUtc,
            Source = x.Source
        }).ToList();
    }

    public async Task<int> GetConsecutiveFailureCountAsync(long accountId, CancellationToken ct)
    {
        // 获取最近的日志，直到遇到成功记录
        var logs = await _db.AccountStatusLogs.AsNoTracking()
            .Where(x => x.AccountId == accountId)
            .OrderByDescending(x => x.CheckedAtUtc)
            .Take(100) // 最多检查100条
            .ToListAsync(ct);

        var count = 0;
        foreach (var log in logs)
        {
            if (log.IsOnline)
                break;
            count++;
        }

        return count;
    }

    public async Task<IReadOnlyDictionary<long, AccountStatusLog>> GetLatestForAllAsync(CancellationToken ct)
    {
        var latestLogs = await _db.AccountStatusLogs.AsNoTracking()
            .GroupBy(x => x.AccountId)
            .Select(g => g.OrderByDescending(x => x.CheckedAtUtc).First())
            .ToDictionaryAsync(x => x.AccountId, x => new AccountStatusLog
            {
                Id = x.Id,
                AccountId = x.AccountId,
                IsOnline = x.IsOnline,
                Error = x.Error,
                CheckedAtUtc = x.CheckedAtUtc,
                Source = x.Source
            }, ct);

        return latestLogs;
    }

    public async Task CleanupAsync(TimeSpan retention, CancellationToken ct)
    {
        var cutoff = DateTime.UtcNow - retention;
        await _db.AccountStatusLogs
            .Where(x => x.CheckedAtUtc < cutoff)
            .ExecuteDeleteAsync(ct);
    }
}
