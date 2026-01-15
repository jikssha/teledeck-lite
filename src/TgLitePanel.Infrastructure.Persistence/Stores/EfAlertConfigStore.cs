using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TgLitePanel.Core.Abstractions.Models;
using TgLitePanel.Core.Abstractions.Stores;
using TgLitePanel.Infrastructure.Persistence.Entities;

namespace TgLitePanel.Infrastructure.Persistence.Stores;

public sealed class EfAlertConfigStore : IAlertConfigStore
{
    private readonly AppDbContext _db;

    public EfAlertConfigStore(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<AlertConfig>> ListAsync(CancellationToken ct)
    {
        var entities = await _db.AlertConfigs.AsNoTracking()
            .OrderBy(x => x.AlertType)
            .ToListAsync(ct);

        return entities.Select(MapToModel).ToList();
    }

    public async Task<AlertConfig?> GetByTypeAsync(string alertType, CancellationToken ct)
    {
        var entity = await _db.AlertConfigs.AsNoTracking()
            .FirstOrDefaultAsync(x => x.AlertType == alertType, ct);

        return entity is null ? null : MapToModel(entity);
    }

    public async Task<long> SaveAsync(AlertConfig config, CancellationToken ct)
    {
        AlertConfigEntity entity;

        if (config.Id > 0)
        {
            entity = await _db.AlertConfigs.FirstOrDefaultAsync(x => x.Id == config.Id, ct)
                ?? throw new InvalidOperationException($"告警配置不存在：{config.Id}");

            entity.AlertType = config.AlertType;
            entity.IsEnabled = config.IsEnabled;
            entity.ConsecutiveFailureThreshold = config.ConsecutiveFailureThreshold;
            entity.CooldownMinutes = config.CooldownMinutes;
            entity.NotifyMethods = config.NotifyMethods;
            entity.AccountIdsJson = config.AccountIds is { Length: > 0 }
                ? JsonSerializer.Serialize(config.AccountIds)
                : null;
            entity.GroupIdsJson = config.GroupIds is { Length: > 0 }
                ? JsonSerializer.Serialize(config.GroupIds)
                : null;
            entity.UpdatedAtUtc = DateTime.UtcNow;
        }
        else
        {
            entity = new AlertConfigEntity
            {
                AlertType = config.AlertType,
                IsEnabled = config.IsEnabled,
                ConsecutiveFailureThreshold = config.ConsecutiveFailureThreshold,
                CooldownMinutes = config.CooldownMinutes,
                NotifyMethods = config.NotifyMethods,
                AccountIdsJson = config.AccountIds is { Length: > 0 }
                    ? JsonSerializer.Serialize(config.AccountIds)
                    : null,
                GroupIdsJson = config.GroupIds is { Length: > 0 }
                    ? JsonSerializer.Serialize(config.GroupIds)
                    : null,
                CreatedAtUtc = DateTime.UtcNow
            };

            _db.AlertConfigs.Add(entity);
        }

        await _db.SaveChangesAsync(ct);
        return entity.Id;
    }

    public async Task DeleteAsync(long id, CancellationToken ct)
    {
        await _db.AlertConfigs
            .Where(x => x.Id == id)
            .ExecuteDeleteAsync(ct);
    }

    public async Task EnsureDefaultsAsync(CancellationToken ct)
    {
        var existingTypes = await _db.AlertConfigs
            .Select(x => x.AlertType)
            .ToListAsync(ct);

        if (!existingTypes.Contains(AlertTypes.AccountOffline))
        {
            _db.AlertConfigs.Add(new AlertConfigEntity
            {
                AlertType = AlertTypes.AccountOffline,
                IsEnabled = true,
                ConsecutiveFailureThreshold = 1,
                CooldownMinutes = 30,
                NotifyMethods = "webhook",
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        if (!existingTypes.Contains(AlertTypes.ConsecutiveFailures))
        {
            _db.AlertConfigs.Add(new AlertConfigEntity
            {
                AlertType = AlertTypes.ConsecutiveFailures,
                IsEnabled = true,
                ConsecutiveFailureThreshold = 3,
                CooldownMinutes = 60,
                NotifyMethods = "webhook",
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync(ct);
    }

    private static AlertConfig MapToModel(AlertConfigEntity entity)
    {
        return new AlertConfig
        {
            Id = entity.Id,
            AlertType = entity.AlertType,
            IsEnabled = entity.IsEnabled,
            ConsecutiveFailureThreshold = entity.ConsecutiveFailureThreshold,
            CooldownMinutes = entity.CooldownMinutes,
            NotifyMethods = entity.NotifyMethods,
            AccountIds = string.IsNullOrEmpty(entity.AccountIdsJson)
                ? null
                : JsonSerializer.Deserialize<long[]>(entity.AccountIdsJson),
            GroupIds = string.IsNullOrEmpty(entity.GroupIdsJson)
                ? null
                : JsonSerializer.Deserialize<long[]>(entity.GroupIdsJson),
            CreatedAtUtc = entity.CreatedAtUtc,
            UpdatedAtUtc = entity.UpdatedAtUtc
        };
    }
}
