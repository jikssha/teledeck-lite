using Microsoft.EntityFrameworkCore;
using TgLitePanel.Core.Abstractions;
using TgLitePanel.Core.Abstractions.Models;
using TgLitePanel.Core.Abstractions.Stores;
using TgLitePanel.Infrastructure.Persistence.Entities;

namespace TgLitePanel.Infrastructure.Persistence.Stores;

public sealed class EfAppConfigStore : IAppConfigStore
{
    private readonly AppDbContext _db;

    public EfAppConfigStore(AppDbContext db) => _db = db;

    public async Task<string?> GetStringAsync(string key, CancellationToken cancellationToken)
    {
        var row = await _db.AppConfigs.AsNoTracking().FirstOrDefaultAsync(x => x.Key == key, cancellationToken);
        return row?.Value;
    }

    public async Task SetStringAsync(string key, string value, CancellationToken cancellationToken)
    {
        await UpsertAsync(key, value, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<TelegramApiConfigDto?> GetTelegramApiConfigAsync(CancellationToken cancellationToken)
    {
        var apiIdRow = await _db.AppConfigs.AsNoTracking().FirstOrDefaultAsync(x => x.Key == AppConfigKeys.TgApiId, cancellationToken);
        var apiHashRow = await _db.AppConfigs.AsNoTracking().FirstOrDefaultAsync(x => x.Key == AppConfigKeys.TgApiHash, cancellationToken);

        if (apiIdRow is null || apiHashRow is null)
            return null;

        if (!int.TryParse(apiIdRow.Value, out var apiId))
            return null;

        if (string.IsNullOrWhiteSpace(apiHashRow.Value))
            return null;

        return new TelegramApiConfigDto(apiId, apiHashRow.Value.Trim());
    }

    public async Task SetTelegramApiConfigAsync(TelegramApiConfigDto config, CancellationToken cancellationToken)
    {
        await UpsertAsync(AppConfigKeys.TgApiId, config.ApiId.ToString(), cancellationToken);
        await UpsertAsync(AppConfigKeys.TgApiHash, config.ApiHash.Trim(), cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task UpsertAsync(string key, string value, CancellationToken cancellationToken)
    {
        var row = await _db.AppConfigs.FirstOrDefaultAsync(x => x.Key == key, cancellationToken);
        if (row is null)
        {
            _db.AppConfigs.Add(new AppConfigEntity
            {
                Key = key,
                Value = value,
                UpdatedAtUtc = DateTime.UtcNow
            });
            return;
        }

        row.Value = value;
        row.UpdatedAtUtc = DateTime.UtcNow;
    }
}
