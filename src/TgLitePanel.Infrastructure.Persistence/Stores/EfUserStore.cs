using Microsoft.EntityFrameworkCore;
using TgLitePanel.Core.Abstractions.Security;
using TgLitePanel.Core.Abstractions.Stores;
using TgLitePanel.Infrastructure.Persistence.Entities;

namespace TgLitePanel.Infrastructure.Persistence.Stores;

public sealed class EfUserStore : IUserStore
{
    private readonly AppDbContext _db;

    public EfUserStore(AppDbContext db) => _db = db;

    public async Task<UserRecord?> FindByUsernameAsync(string username, CancellationToken cancellationToken)
    {
        var row = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Username == username, cancellationToken);
        return row is null ? null : new UserRecord(row.Id, row.Username, row.PasswordHash, row.Role);
    }

    public async Task<UserRecord?> FindByIdAsync(long userId, CancellationToken cancellationToken)
    {
        var row = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
        return row is null ? null : new UserRecord(row.Id, row.Username, row.PasswordHash, row.Role);
    }

    public async Task<long> EnsureAdminAsync(string username, string passwordHash, CancellationToken cancellationToken)
    {
        var existing = await _db.Users.FirstOrDefaultAsync(x => x.Username == username, cancellationToken);
        if (existing is not null)
            return existing.Id;

        var entity = new UserEntity
        {
            Username = username,
            PasswordHash = passwordHash,
            Role = Roles.Admin,
            CreatedAtUtc = DateTime.UtcNow
        };
        _db.Users.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}

