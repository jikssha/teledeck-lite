namespace TgLitePanel.Core.Abstractions.Models;

public sealed record AccountDto(
    long Id,
    string Phone,
    AccountStatus Status,
    string DataDir,
    int? ApiIdOverride,
    long? SystemChatId);

