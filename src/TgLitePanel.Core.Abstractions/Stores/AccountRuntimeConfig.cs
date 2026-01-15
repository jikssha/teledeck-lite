using TgLitePanel.Core.Abstractions.Models;

namespace TgLitePanel.Core.Abstractions.Stores;

public sealed record AccountRuntimeConfig(
    long AccountId,
    string Phone,
    AccountStatus Status,
    string DataDir,
    int? ApiIdOverride,
    long? SystemChatId,
    long? GroupId);
