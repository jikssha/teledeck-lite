using Microsoft.EntityFrameworkCore;
using TgLitePanel.Core.Abstractions.Models;
using TgLitePanel.Core.Abstractions.Stores;
using TgLitePanel.Infrastructure.Persistence.Entities;

namespace TgLitePanel.Infrastructure.Persistence.Stores;

public sealed class EfSharedCodeStore : ISharedCodeStore
{
    private readonly AppDbContext _db;

    public EfSharedCodeStore(AppDbContext db) => _db = db;

    public async Task<SharedCodeDto> CreateAsync(long accountId, string code, DateTimeOffset expiresAtUtc, CancellationToken cancellationToken)
    {
        var token = Convert.ToHexString(Guid.NewGuid().ToByteArray()).ToLowerInvariant();
        var entity = new SharedCodeTokenEntity
        {
            Token = token,
            AccountId = accountId,
            Code = code,
            ExpiresAtUtc = expiresAtUtc.UtcDateTime,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.SharedCodeTokens.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return new SharedCodeDto(entity.Token, entity.Code, new DateTimeOffset(entity.ExpiresAtUtc, TimeSpan.Zero));
    }

    public async Task<SharedCodeDto?> GetAsync(string token, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var entity = await _db.SharedCodeTokens.AsNoTracking()
            .Where(x => x.Token == token)
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
            return null;

        if (entity.ExpiresAtUtc <= now)
            return null;

        return new SharedCodeDto(entity.Token, entity.Code, new DateTimeOffset(entity.ExpiresAtUtc, TimeSpan.Zero));
    }
}

