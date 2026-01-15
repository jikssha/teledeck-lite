using TgLitePanel.Core.Abstractions.Models;

namespace TgLitePanel.Core.Abstractions.Stores;

public interface ISharedCodeStore
{
    Task<SharedCodeDto> CreateAsync(long accountId, string code, DateTimeOffset expiresAtUtc, CancellationToken cancellationToken);
    Task<SharedCodeDto?> GetAsync(string token, CancellationToken cancellationToken);
}

