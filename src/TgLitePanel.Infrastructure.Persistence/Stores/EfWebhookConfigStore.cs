using Microsoft.EntityFrameworkCore;
using TgLitePanel.Core.Abstractions.Models;
using TgLitePanel.Core.Abstractions.Stores;
using TgLitePanel.Infrastructure.Persistence.Entities;

namespace TgLitePanel.Infrastructure.Persistence.Stores;

public sealed class EfWebhookConfigStore : IWebhookConfigStore
{
    private readonly AppDbContext _db;

    public EfWebhookConfigStore(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<WebhookConfig>> ListAsync(CancellationToken ct)
    {
        var entities = await _db.WebhookConfigs.AsNoTracking()
            .OrderBy(x => x.Id)
            .ToListAsync(ct);

        return entities.Select(MapToModel).ToList();
    }

    public async Task<IReadOnlyList<WebhookConfig>> ListEnabledAsync(CancellationToken ct)
    {
        var entities = await _db.WebhookConfigs.AsNoTracking()
            .Where(x => x.IsEnabled)
            .OrderBy(x => x.Id)
            .ToListAsync(ct);

        return entities.Select(MapToModel).ToList();
    }

    public async Task<WebhookConfig?> GetAsync(long id, CancellationToken ct)
    {
        var entity = await _db.WebhookConfigs.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        return entity is null ? null : MapToModel(entity);
    }

    public async Task<WebhookConfig> CreateAsync(WebhookConfig config, CancellationToken ct)
    {
        var entity = new WebhookConfigEntity
        {
            Name = config.Name,
            Url = config.Url,
            Secret = config.Secret,
            IsEnabled = config.IsEnabled,
            Events = string.Join(",", config.Events),
            AccountIds = config.AccountIds.Count > 0 ? string.Join(",", config.AccountIds) : null,
            RetryCount = config.RetryCount,
            TimeoutSeconds = config.TimeoutSeconds
        };

        _db.WebhookConfigs.Add(entity);
        await _db.SaveChangesAsync(ct);

        return MapToModel(entity);
    }

    public async Task UpdateAsync(WebhookConfig config, CancellationToken ct)
    {
        var entity = await _db.WebhookConfigs.FirstOrDefaultAsync(x => x.Id == config.Id, ct);
        if (entity is null)
            return;

        entity.Name = config.Name;
        entity.Url = config.Url;
        entity.Secret = config.Secret;
        entity.IsEnabled = config.IsEnabled;
        entity.Events = string.Join(",", config.Events);
        entity.AccountIds = config.AccountIds.Count > 0 ? string.Join(",", config.AccountIds) : null;
        entity.RetryCount = config.RetryCount;
        entity.TimeoutSeconds = config.TimeoutSeconds;

        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(long id, CancellationToken ct)
    {
        var entity = await _db.WebhookConfigs.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is not null)
        {
            _db.WebhookConfigs.Remove(entity);
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task UpdateLastTriggeredAsync(long id, DateTime triggeredAtUtc, string? error, CancellationToken ct)
    {
        var entity = await _db.WebhookConfigs.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null)
            return;

        entity.LastTriggeredAtUtc = triggeredAtUtc;
        entity.LastError = error;
        await _db.SaveChangesAsync(ct);
    }

    private static WebhookConfig MapToModel(WebhookConfigEntity entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Url = entity.Url,
        Secret = entity.Secret,
        IsEnabled = entity.IsEnabled,
        Events = string.IsNullOrEmpty(entity.Events)
            ? new List<string>()
            : entity.Events.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
        AccountIds = string.IsNullOrEmpty(entity.AccountIds)
            ? new List<long>()
            : entity.AccountIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => long.TryParse(s, out var id) ? id : 0)
                .Where(id => id > 0)
                .ToList(),
        RetryCount = entity.RetryCount,
        TimeoutSeconds = entity.TimeoutSeconds,
        LastTriggeredAtUtc = entity.LastTriggeredAtUtc,
        LastError = entity.LastError
    };
}
