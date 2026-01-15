using Microsoft.EntityFrameworkCore;
using TgLitePanel.Core.Abstractions.Models;
using TgLitePanel.Core.Abstractions.Stores;
using TgLitePanel.Infrastructure.Persistence.Entities;

namespace TgLitePanel.Infrastructure.Persistence.Stores;

public sealed class EfAlertHistoryStore : IAlertHistoryStore
{
    private readonly AppDbContext _db;

    public EfAlertHistoryStore(AppDbContext db) => _db = db;

    public async Task<long> WriteAsync(
        long? accountId,
        string alertType,
        string message,
        string? details,
        bool notificationSent,
        string? notificationError,
        CancellationToken ct)
    {
        var entity = new AlertHistoryEntity
        {
            AccountId = accountId,
            AlertType = alertType,
            Message = message,
            Details = details,
            NotificationSent = notificationSent,
            NotificationError = notificationError,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.AlertHistories.Add(entity);
        await _db.SaveChangesAsync(ct);
        return entity.Id;
    }

    public async Task<IReadOnlyList<AlertHistory>> ListAsync(int limit, long? accountId, string? alertType, CancellationToken ct)
    {
        var query = _db.AlertHistories.AsNoTracking();

        if (accountId.HasValue)
            query = query.Where(x => x.AccountId == accountId.Value);

        if (!string.IsNullOrEmpty(alertType))
            query = query.Where(x => x.AlertType == alertType);

        var entities = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(limit)
            .ToListAsync(ct);

        return entities.Select(x => new AlertHistory
        {
            Id = x.Id,
            AccountId = x.AccountId,
            AlertType = x.AlertType,
            Message = x.Message,
            Details = x.Details,
            NotificationSent = x.NotificationSent,
            NotificationError = x.NotificationError,
            CreatedAtUtc = x.CreatedAtUtc
        }).ToList();
    }

    public async Task<DateTime?> GetLastAlertTimeAsync(long accountId, string alertType, CancellationToken ct)
    {
        return await _db.AlertHistories.AsNoTracking()
            .Where(x => x.AccountId == accountId && x.AlertType == alertType)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => (DateTime?)x.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);
    }

    public async Task CleanupAsync(TimeSpan retention, CancellationToken ct)
    {
        var cutoff = DateTime.UtcNow - retention;
        await _db.AlertHistories
            .Where(x => x.CreatedAtUtc < cutoff)
            .ExecuteDeleteAsync(ct);
    }
}
