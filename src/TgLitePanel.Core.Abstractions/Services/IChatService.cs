using TgLitePanel.Core.Abstractions.Models;

namespace TgLitePanel.Core.Abstractions.Services;

public interface IChatService
{
    Task<IReadOnlyList<ChatDialogDto>> GetDialogsAsync(long accountId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ChatMessageDto>> GetHistoryAsync(long accountId, long chatId, int limit, CancellationToken cancellationToken);
    Task SendTextAsync(long accountId, long chatId, string text, CancellationToken cancellationToken);
}

