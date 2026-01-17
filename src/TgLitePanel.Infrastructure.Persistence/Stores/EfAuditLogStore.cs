using Microsoft.EntityFrameworkCore;
using TgLitePanel.Core.Abstractions.Models;
using TgLitePanel.Core.Abstractions.Stores;
using TgLitePanel.Infrastructure.Persistence.Entities;

namespace TgLitePanel.Infrastructure.Persistence.Stores;

public sealed class EfAuditLogStore : IAuditLogStore
{
    private readonly AppDbContext _db;

    public EfAuditLogStore(AppDbContext db)
    {
        _db = db;
    }

    public async Task WriteAsync(
        string userName,
        string action,
        string description,
        string? targetId = null,
        string? ipAddress = null,
        string? userAgent = null,
        string result = "success",
        string? additionalData = null,
        CancellationToken cancellationToken = default)
    {
        var entity = new AuditLogEntity
        {
            UserName = userName,
            Action = action,
            Description = description,
            TargetId = targetId,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Result = result,
            AdditionalData = additionalData,
            CreatedAt = DateTime.UtcNow
        };

        _db.AuditLogs.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<AuditLog> Items, int TotalCount)> ListPagedAsync(
        int pageIndex,
        int pageSize,
        string? userNameFilter = null,
        string? actionFilter = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _db.AuditLogs.AsNoTracking();

        // 应用过滤条件
        if (!string.IsNullOrWhiteSpace(userNameFilter))
            query = query.Where(x => x.UserName.Contains(userNameFilter));

        if (!string.IsNullOrWhiteSpace(actionFilter))
            query = query.Where(x => x.Action.Contains(actionFilter));

        if (startDate.HasValue)
            query = query.Where(x => x.CreatedAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(x => x.CreatedAt <= endDate.Value);

        // 获取总数
        var totalCount = await query.CountAsync(cancellationToken);

        // 分页查询
        var entities = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = entities.Select(x => new AuditLog
        {
            Id = x.Id,
            UserName = x.UserName,
            Action = x.Action,
            Description = x.Description,
            TargetId = x.TargetId,
            IpAddress = x.IpAddress,
            UserAgent = x.UserAgent,
            Result = x.Result,
            AdditionalData = x.AdditionalData,
            CreatedAt = x.CreatedAt
        }).ToList();

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<AuditLog>> GetRecentByUserAsync(
        string userName,
        int count = 20,
        CancellationToken cancellationToken = default)
    {
        var entities = await _db.AuditLogs
            .AsNoTracking()
            .Where(x => x.UserName == userName)
            .OrderByDescending(x => x.CreatedAt)
            .Take(count)
            .ToListAsync(cancellationToken);

        return entities.Select(x => new AuditLog
        {
            Id = x.Id,
            UserName = x.UserName,
            Action = x.Action,
            Description = x.Description,
            TargetId = x.TargetId,
            IpAddress = x.IpAddress,
            UserAgent = x.UserAgent,
            Result = x.Result,
            AdditionalData = x.AdditionalData,
            CreatedAt = x.CreatedAt
        }).ToList();
    }

    public async Task CleanupOldLogsAsync(int retentionDays = 90, CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);

        await _db.AuditLogs
            .Where(x => x.CreatedAt < cutoffDate)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
