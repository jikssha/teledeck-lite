using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TgLitePanel.Core.Abstractions.Modules;
using TgLitePanel.Infrastructure.Persistence.Entities;

namespace TgLitePanel.Infrastructure.Persistence.Stores;

/// <summary>
/// 模块存储实现
/// </summary>
public sealed class ModuleStore : IModuleStore
{
    private readonly AppDbContext _db;

    public ModuleStore(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ModuleInfo?> GetModuleAsync(string moduleId, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Modules
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == moduleId, cancellationToken);

        return entity == null ? null : ToModuleInfo(entity);
    }

    public async Task<IReadOnlyList<ModuleInfo>> GetAllModulesAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _db.Modules
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return entities.Select(ToModuleInfo).ToList();
    }

    public async Task<IReadOnlyList<ModuleInfo>> GetEnabledModulesAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _db.Modules
            .AsNoTracking()
            .Where(x => x.Status == (int)ModuleStatus.Enabled)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return entities.Select(ToModuleInfo).ToList();
    }

    public async Task SaveModuleAsync(ModuleInfo module, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Modules.FirstOrDefaultAsync(x => x.Id == module.Id, cancellationToken);

        if (entity == null)
        {
            entity = new ModuleEntity
            {
                Id = module.Id,
                Name = module.Name,
                Version = module.Version,
                Description = module.Description,
                AuthorName = module.AuthorName,
                AuthorEmail = module.AuthorEmail,
                Status = (int)module.Status,
                ErrorMessage = module.ErrorMessage,
                InstallPath = module.InstallPath,
                EntryAssembly = module.EntryAssembly,
                EntryType = module.EntryType,
                InstalledAtUtc = module.InstalledAt,
                UpdatedAtUtc = module.UpdatedAt,
                LastLoadedAtUtc = module.LastLoadedAt,
                InstalledBy = module.InstalledBy,
                Checksum = module.Checksum,
                PermissionsJson = JsonSerializer.Serialize(module.Permissions),
                RoutesJson = JsonSerializer.Serialize(module.Routes)
            };
            _db.Modules.Add(entity);
        }
        else
        {
            entity.Name = module.Name;
            entity.Version = module.Version;
            entity.Description = module.Description;
            entity.AuthorName = module.AuthorName;
            entity.AuthorEmail = module.AuthorEmail;
            entity.Status = (int)module.Status;
            entity.ErrorMessage = module.ErrorMessage;
            entity.InstallPath = module.InstallPath;
            entity.EntryAssembly = module.EntryAssembly;
            entity.EntryType = module.EntryType;
            entity.UpdatedAtUtc = DateTime.UtcNow;
            entity.LastLoadedAtUtc = module.LastLoadedAt;
            entity.Checksum = module.Checksum;
            entity.PermissionsJson = JsonSerializer.Serialize(module.Permissions);
            entity.RoutesJson = JsonSerializer.Serialize(module.Routes);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateModuleStatusAsync(
        string moduleId,
        ModuleStatus status,
        string? errorMessage = null,
        CancellationToken cancellationToken = default)
    {
        var entity = await _db.Modules.FirstOrDefaultAsync(x => x.Id == moduleId, cancellationToken);
        if (entity == null) return;

        entity.Status = (int)status;
        entity.ErrorMessage = errorMessage;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateLastLoadedTimeAsync(
        string moduleId,
        DateTime loadedAt,
        CancellationToken cancellationToken = default)
    {
        var entity = await _db.Modules.FirstOrDefaultAsync(x => x.Id == moduleId, cancellationToken);
        if (entity == null) return;

        entity.LastLoadedAtUtc = loadedAt;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteModuleAsync(string moduleId, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Modules.FirstOrDefaultAsync(x => x.Id == moduleId, cancellationToken);
        if (entity == null) return;

        _db.Modules.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task AddAuditLogAsync(
        string moduleId,
        string eventType,
        object? eventData,
        string? userId,
        CancellationToken cancellationToken = default)
    {
        var entity = new ModuleAuditLogEntity
        {
            ModuleId = moduleId,
            EventType = eventType,
            EventData = eventData == null ? null : JsonSerializer.Serialize(eventData),
            CreatedAtUtc = DateTime.UtcNow,
            UserId = userId
        };

        _db.ModuleAuditLogs.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ModuleAuditLog>> GetAuditLogsAsync(
        string? moduleId = null,
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        var query = _db.ModuleAuditLogs.AsNoTracking();

        if (!string.IsNullOrEmpty(moduleId))
        {
            query = query.Where(x => x.ModuleId == moduleId);
        }

        var entities = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return entities.Select(e => new ModuleAuditLog
        {
            Id = e.Id,
            ModuleId = e.ModuleId,
            EventType = e.EventType,
            EventData = e.EventData,
            CreatedAt = e.CreatedAtUtc,
            UserId = e.UserId
        }).ToList();
    }

    private static ModuleInfo ToModuleInfo(ModuleEntity entity)
    {
        return new ModuleInfo
        {
            Id = entity.Id,
            Name = entity.Name,
            Version = entity.Version,
            Description = entity.Description,
            AuthorName = entity.AuthorName,
            AuthorEmail = entity.AuthorEmail,
            Status = (ModuleStatus)entity.Status,
            ErrorMessage = entity.ErrorMessage,
            InstallPath = entity.InstallPath,
            EntryAssembly = entity.EntryAssembly,
            EntryType = entity.EntryType,
            InstalledAt = entity.InstalledAtUtc,
            UpdatedAt = entity.UpdatedAtUtc,
            LastLoadedAt = entity.LastLoadedAtUtc,
            InstalledBy = entity.InstalledBy,
            Checksum = entity.Checksum,
            Permissions = string.IsNullOrEmpty(entity.PermissionsJson)
                ? new List<string>()
                : JsonSerializer.Deserialize<List<string>>(entity.PermissionsJson) ?? new List<string>(),
            Routes = string.IsNullOrEmpty(entity.RoutesJson)
                ? new List<ModuleRoute>()
                : JsonSerializer.Deserialize<List<ModuleRoute>>(entity.RoutesJson) ?? new List<ModuleRoute>()
        };
    }
}
