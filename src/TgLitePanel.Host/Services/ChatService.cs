using System.Text.Json;
using System.Text.Json.Nodes;
using TgLitePanel.Core.Abstractions.Models;
using TgLitePanel.Core.Abstractions.Services;
using TgLitePanel.Core.Abstractions.TdLib;

namespace TgLitePanel.Host.Services;

public sealed class ChatService : IChatService
{
    private readonly ITdClientManager _tdClientManager;

    public ChatService(ITdClientManager tdClientManager) => _tdClientManager = tdClientManager;

    public async Task<IReadOnlyList<ChatDialogDto>> GetDialogsAsync(long accountId, CancellationToken cancellationToken)
    {
        await using var lease = await _tdClientManager.AcquireAsync(accountId, cancellationToken);
        var chatsJson = await lease.Client.ExecuteAsync(new JsonObject
        {
            ["@type"] = "getChats",
            ["chat_list"] = new JsonObject { ["@type"] = "chatListMain" },
            ["limit"] = 50
        }.ToJsonString(), TimeSpan.FromSeconds(15), cancellationToken);

        using var doc = TdJsonHelpers.Parse(chatsJson);
        TdJsonHelpers.ThrowIfError(doc.RootElement);

        if (!doc.RootElement.TryGetProperty("chat_ids", out var ids) || ids.ValueKind != JsonValueKind.Array)
            return Array.Empty<ChatDialogDto>();

        var result = new List<ChatDialogDto>();
        foreach (var idEl in ids.EnumerateArray())
        {
            if (idEl.ValueKind != JsonValueKind.Number || !idEl.TryGetInt64(out var chatId))
                continue;

            var chatJson = await lease.Client.ExecuteAsync(new JsonObject
            {
                ["@type"] = "getChat",
                ["chat_id"] = chatId
            }.ToJsonString(), TimeSpan.FromSeconds(15), cancellationToken);
            using var chatDoc = TdJsonHelpers.Parse(chatJson);
            TdJsonHelpers.ThrowIfError(chatDoc.RootElement);

            var title = chatDoc.RootElement.TryGetProperty("title", out var titleEl) ? titleEl.GetString() : null;
            if (string.IsNullOrWhiteSpace(title))
                title = $"Chat {chatId}";

            string? preview = null;
            if (chatDoc.RootElement.TryGetProperty("last_message", out var lastMsg) &&
                lastMsg.TryGetProperty("content", out var content) &&
                content.TryGetProperty("@type", out var ctype) &&
                ctype.GetString() == "messageText" &&
                content.TryGetProperty("text", out var text) &&
                text.TryGetProperty("text", out var textText))
            {
                preview = textText.GetString();
            }

            result.Add(new ChatDialogDto(chatId, title!, preview));
        }

        return result;
    }

    public async Task<IReadOnlyList<ChatMessageDto>> GetHistoryAsync(long accountId, long chatId, int limit, CancellationToken cancellationToken)
    {
        if (limit <= 0)
            limit = 50;

        await using var lease = await _tdClientManager.AcquireAsync(accountId, cancellationToken);
        var historyJson = await lease.Client.ExecuteAsync(new JsonObject
        {
            ["@type"] = "getChatHistory",
            ["chat_id"] = chatId,
            ["from_message_id"] = 0,
            ["offset"] = 0,
            ["limit"] = limit
        }.ToJsonString(), TimeSpan.FromSeconds(15), cancellationToken);

        using var doc = TdJsonHelpers.Parse(historyJson);
        TdJsonHelpers.ThrowIfError(doc.RootElement);

        if (!doc.RootElement.TryGetProperty("messages", out var messages) || messages.ValueKind != JsonValueKind.Array)
            return Array.Empty<ChatMessageDto>();

        var list = new List<ChatMessageDto>();
        foreach (var msg in messages.EnumerateArray())
        {
            if (!msg.TryGetProperty("id", out var idEl) || !idEl.TryGetInt64(out var msgId))
                continue;

            var isOutgoing = msg.TryGetProperty("is_outgoing", out var outEl) && outEl.ValueKind == JsonValueKind.True;
            var dateUnix = msg.TryGetProperty("date", out var dateEl) && dateEl.TryGetInt64(out var v) ? v : 0;
            var date = DateTimeOffset.FromUnixTimeSeconds(dateUnix);

            var text = "";
            if (msg.TryGetProperty("content", out var content) &&
                content.TryGetProperty("@type", out var ctype) &&
                ctype.GetString() == "messageText" &&
                content.TryGetProperty("text", out var t) &&
                t.TryGetProperty("text", out var tt))
            {
                text = tt.GetString() ?? "";
            }

            list.Add(new ChatMessageDto(msgId, isOutgoing, date, text));
        }

        list.Reverse();
        return list;
    }

    public async Task SendTextAsync(long accountId, long chatId, string text, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        await using var lease = await _tdClientManager.AcquireAsync(accountId, cancellationToken);
        var request = new JsonObject
        {
            ["@type"] = "sendMessage",
            ["chat_id"] = chatId,
            ["input_message_content"] = new JsonObject
            {
                ["@type"] = "inputMessageText",
                ["text"] = new JsonObject { ["@type"] = "formattedText", ["text"] = text }
            }
        }.ToJsonString();

        _ = await lease.Client.ExecuteAsync(request, TimeSpan.FromSeconds(15), cancellationToken);
    }
}
