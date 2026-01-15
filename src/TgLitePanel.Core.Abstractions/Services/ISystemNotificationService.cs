using TgLitePanel.Core.Abstractions.Models;

namespace TgLitePanel.Core.Abstractions.Services;

public interface ISystemNotificationService
{
    Task<string?> GetLatestOtpCodeAsync(long accountId, CancellationToken cancellationToken);
    Task<SharedCodeDto> GenerateShareTokenAsync(long accountId, CancellationToken cancellationToken);
    Task<SharedCodeDto?> GetSharedCodeAsync(string token, CancellationToken cancellationToken);
}

