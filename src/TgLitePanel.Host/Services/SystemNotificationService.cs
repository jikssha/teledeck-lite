using System.Text.Json;
using System.Text.Json.Nodes;
using TgLitePanel.Core.Abstractions.Models;
using TgLitePanel.Core.Abstractions.Services;
using TgLitePanel.Core.Abstractions.Stores;
using TgLitePanel.Core.Abstractions.TdLib;
using CoreOtpCodeExtractor = TgLitePanel.Core.Abstractions.Utils.OtpCodeExtractor;
using CoreSystemMessageTextExtractor = TgLitePanel.Core.Abstractions.Utils.SystemMessageTextExtractor;

namespace TgLitePanel.Host.Services;

public sealed class SystemNotificationService : ISystemNotificationService
{
    private readonly ISharedCodeStore _sharedCodeStore;
    private readonly ITdClientManager _tdClientManager;
    private readonly IAccountStore _accountStore;

    public SystemNotificationService(ISharedCodeStore sharedCodeStore, ITdClientManager tdClientManager, IAccountStore accountStore)
    {
        _sharedCodeStore = sharedCodeStore;
        _tdClientManager = tdClientManager;
        _accountStore = accountStore;
    }

    public async Task<string?> GetLatestOtpCodeAsync(long accountId, CancellationToken cancellationToken)
    {
        var account = await _accountStore.GetAsync(accountId, cancellationToken);
        if (account is null)
            return null;

        await using var lease = await _tdClientManager.AcquireAsync(accountId, cancellationToken);

        var dialogs = await lease.Client.ExecuteAsync(new JsonObject
        {
            ["@type"] = "searchChats",
            ["query"] = "Telegram",
            ["limit"] = 10
        }.ToJsonString(), TimeSpan.FromSeconds(15), cancellationToken);

        using var doc = TdJsonHelpers.Parse(dialogs);
        TdJsonHelpers.ThrowIfError(doc.RootElement);

        if (!doc.RootElement.TryGetProperty("chat_ids", out var ids) || ids.ValueKind != JsonValueKind.Array)
            return null;

        foreach (var idEl in ids.EnumerateArray())
        {
            if (!idEl.TryGetInt64(out var chatId))
                continue;

            var historyJson = await lease.Client.ExecuteAsync(new JsonObject
            {
                ["@type"] = "getChatHistory",
                ["chat_id"] = chatId,
                ["from_message_id"] = 0,
                ["offset"] = 0,
                ["limit"] = 30
            }.ToJsonString(), TimeSpan.FromSeconds(15), cancellationToken);

            using var history = TdJsonHelpers.Parse(historyJson);
            TdJsonHelpers.ThrowIfError(history.RootElement);

            if (!history.RootElement.TryGetProperty("messages", out var messages) || messages.ValueKind != JsonValueKind.Array)
                continue;

            foreach (var msg in messages.EnumerateArray())
            {
                if (!CoreSystemMessageTextExtractor.TryExtractPlainText(msg, out var text))
                    continue;

                if (CoreOtpCodeExtractor.TryExtract(text, out var code))
                    return code;
            }
        }

        return null;
    }

    public async Task<SharedCodeDto> GenerateShareTokenAsync(long accountId, CancellationToken cancellationToken)
    {
        var code = await GetLatestOtpCodeAsync(accountId, cancellationToken);
        if (string.IsNullOrWhiteSpace(code))
            throw new InvalidOperationException("未找到可用验证码。");

        return await _sharedCodeStore.CreateAsync(accountId, code, DateTimeOffset.UtcNow.AddMinutes(10), cancellationToken);
    }

    public Task<SharedCodeDto?> GetSharedCodeAsync(string token, CancellationToken cancellationToken)
        => _sharedCodeStore.GetAsync(token, cancellationToken);
}
