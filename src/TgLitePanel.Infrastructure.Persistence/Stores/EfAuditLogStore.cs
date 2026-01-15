using TgLitePanel.Core.Abstractions.Stores;
using TgLitePanel.Infrastructure.Persistence.Entities;

namespace TgLitePanel.Infrastructure.Persistence.Stores;

public sealed class EfAuditLogStore : IAuditLogStore
{
    private readonly AppDbContext _db;

    public EfAuditLogStore(AppDbContext db) => _db = db;

    public async Task WriteAsync(long? userId, string action, string summary, string? ip, CancellationToken cancellationToken)
    {
        _db.AuditLogs.Add(new AuditLogEntity
        {
            UserId = userId,
            Action = action,
            Summary = summary,
            Ip = ip,
            CreatedAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);
    }
}

