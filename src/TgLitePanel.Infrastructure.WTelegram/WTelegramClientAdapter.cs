using System.Text.Json;
using TgLitePanel.Core.Abstractions.TdLib;
using TL;
using JsonObject = System.Text.Json.Nodes.JsonObject;
using JsonArray = System.Text.Json.Nodes.JsonArray;
using JsonNode = System.Text.Json.Nodes.JsonNode;
using JsonValue = System.Text.Json.Nodes.JsonValue;
using Client = WTelegram.Client;

namespace TgLitePanel.Infrastructure.WTelegram;

/// <summary>
/// WTelegram 客户端适配器，将 TDLib JSON 格式的请求转换为 WTelegram API 调用
/// </summary>
internal sealed class WTelegramClientAdapter : ITdClient
{
    private readonly Client _client;
    private readonly long _accountId;

    public WTelegramClientAdapter(Client client, long accountId)
    {
        _client = client;
        _accountId = accountId;
    }

    public async ValueTask<string> ExecuteAsync(string json, TimeSpan timeout, CancellationToken ct)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(timeout);

        try
        {
            var request = JsonNode.Parse(json)?.AsObject()
                ?? throw new ArgumentException("Invalid JSON", nameof(json));

            var type = request["@type"]?.GetValue<string>()
                ?? throw new ArgumentException("Missing @type", nameof(json));

            return type switch
            {
                "getChats" => await GetChatsAsync(request, cts.Token),
                "getChat" => await GetChatAsync(request, cts.Token),
                "getChatHistory" => await GetChatHistoryAsync(request, cts.Token),
                "sendMessage" => await SendMessageAsync(request, cts.Token),
                "searchChats" => await SearchChatsAsync(request, cts.Token),
                "setAuthenticationPhoneNumber" => await SetAuthPhoneAsync(request, cts.Token),
                "checkAuthenticationCode" => await CheckAuthCodeAsync(request, cts.Token),
                "checkAuthenticationPassword" => await CheckAuthPasswordAsync(request, cts.Token),
                "getMe" => await GetMeAsync(cts.Token),
                _ => throw new NotSupportedException($"Method not implemented: {type}")
            };
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            throw new TimeoutException($"Operation timeout after {timeout}");
        }
    }

    private async Task<string> GetChatsAsync(JsonObject request, CancellationToken ct)
    {
        var limit = request["limit"]?.GetValue<int>() ?? 50;
        var dialogs = await _client.Messages_GetAllDialogs();

        var chatIds = new List<long>();
        foreach (var dialog in dialogs.dialogs.Take(limit))
        {
            var peerId = dialog.Peer.ID;
            chatIds.Add(peerId);
        }

        var response = new JsonObject
        {
            ["@type"] = "chats",
            ["chat_ids"] = new JsonArray(chatIds.Select(id => JsonValue.Create(id)).ToArray())
        };

        return response.ToJsonString();
    }

    private async Task<string> GetChatAsync(JsonObject request, CancellationToken ct)
    {
        var chatId = request["chat_id"]?.GetValue<long>()
            ?? throw new ArgumentException("Missing chat_id");

        var dialogs = await _client.Messages_GetAllDialogs();

        string? title = null;
        string? chatType = "chatTypePrivate";

        // 查找用户
        if (dialogs.users.TryGetValue(chatId, out var user))
        {
            title = string.IsNullOrEmpty(user.last_name)
                ? user.first_name
                : $"{user.first_name} {user.last_name}";
            chatType = "chatTypePrivate";
        }
        // 查找聊天（群组/频道）
        else if (dialogs.chats.TryGetValue(chatId, out var chat))
        {
            title = chat.Title;
            chatType = chat is Channel ? "chatTypeSupergroup" : "chatTypeBasicGroup";
        }

        if (title == null)
        {
            return new JsonObject { ["@type"] = "error", ["message"] = "Chat not found" }.ToJsonString();
        }

        var response = new JsonObject
        {
            ["@type"] = "chat",
            ["id"] = chatId,
            ["title"] = title,
            ["type"] = new JsonObject { ["@type"] = chatType }
        };

        return response.ToJsonString();
    }

    private async Task<string> GetChatHistoryAsync(JsonObject request, CancellationToken ct)
    {
        var chatId = request["chat_id"]?.GetValue<long>()
            ?? throw new ArgumentException("Missing chat_id");
        var limit = request["limit"]?.GetValue<int>() ?? 50;

        // 获取 InputPeer
        var dialogs = await _client.Messages_GetAllDialogs();
        InputPeer? peer = null;

        if (dialogs.users.TryGetValue(chatId, out var user))
        {
            peer = user;
        }
        else if (dialogs.chats.TryGetValue(chatId, out var chat))
        {
            peer = chat;
        }

        if (peer == null)
        {
            return new JsonObject
            {
                ["@type"] = "messages",
                ["messages"] = new JsonArray(),
                ["total_count"] = 0
            }.ToJsonString();
        }

        var history = await _client.Messages_GetHistory(peer, limit: limit);

        var messages = new JsonArray();
        foreach (var msg in history.Messages)
        {
            if (msg is not Message message) continue;

            var messageObj = new JsonObject
            {
                ["@type"] = "message",
                ["id"] = message.id,
                ["is_outgoing"] = message.flags.HasFlag(Message.Flags.out_),
                ["date"] = new DateTimeOffset(message.Date).ToUnixTimeSeconds(),
                ["content"] = new JsonObject
                {
                    ["@type"] = "messageText",
                    ["text"] = new JsonObject
                    {
                        ["@type"] = "formattedText",
                        ["text"] = message.message ?? ""
                    }
                }
            };
            messages.Add(messageObj);
        }

        var response = new JsonObject
        {
            ["@type"] = "messages",
            ["messages"] = messages,
            ["total_count"] = messages.Count
        };

        return response.ToJsonString();
    }

    private async Task<string> SendMessageAsync(JsonObject request, CancellationToken ct)
    {
        var chatId = request["chat_id"]?.GetValue<long>()
            ?? throw new ArgumentException("Missing chat_id");

        var content = request["input_message_content"]?.AsObject();
        var text = content?["text"]?["text"]?.GetValue<string>()
            ?? throw new ArgumentException("Missing message text");

        // 获取 InputPeer
        var dialogs = await _client.Messages_GetAllDialogs();
        InputPeer? peer = null;

        if (dialogs.users.TryGetValue(chatId, out var user))
        {
            peer = user;
        }
        else if (dialogs.chats.TryGetValue(chatId, out var chat))
        {
            peer = chat;
        }

        if (peer == null)
        {
            return new JsonObject { ["@type"] = "error", ["message"] = "Peer not found" }.ToJsonString();
        }

        var result = await _client.SendMessageAsync(peer, text);

        var response = new JsonObject
        {
            ["@type"] = "message",
            ["id"] = result.id,
            ["is_outgoing"] = true,
            ["date"] = new DateTimeOffset(result.Date).ToUnixTimeSeconds()
        };

        return response.ToJsonString();
    }

    private async Task<string> SearchChatsAsync(JsonObject request, CancellationToken ct)
    {
        var query = request["query"]?.GetValue<string>()?.Trim();
        var limit = request["limit"]?.GetValue<int>() ?? 50;
        if (limit <= 0) limit = 50;

        var dialogs = await _client.Messages_GetAllDialogs();
        var ids = new List<long>();

        foreach (var dialog in dialogs.dialogs)
        {
            var id = dialog.Peer.ID;

            if (string.IsNullOrWhiteSpace(query))
            {
                ids.Add(id);
            }
            else if (dialogs.users.TryGetValue(id, out var user))
            {
                var name = string.IsNullOrWhiteSpace(user.last_name) ? user.first_name : $"{user.first_name} {user.last_name}";
                if (!string.IsNullOrWhiteSpace(name) &&
                    name.Contains(query, StringComparison.OrdinalIgnoreCase))
                {
                    ids.Add(id);
                }
            }
            else if (dialogs.chats.TryGetValue(id, out var chat))
            {
                if (!string.IsNullOrWhiteSpace(chat.Title) &&
                    chat.Title.Contains(query, StringComparison.OrdinalIgnoreCase))
                {
                    ids.Add(id);
                }
            }

            if (ids.Count >= limit)
                break;
        }

        var response = new JsonObject
        {
            ["@type"] = "chats",
            ["chat_ids"] = new JsonArray(ids.Select(x => JsonValue.Create(x)).ToArray())
        };

        return response.ToJsonString();
    }

    private Task<string> SetAuthPhoneAsync(JsonObject request, CancellationToken ct)
    {
        var phone = request["phone_number"]?.GetValue<string>()
            ?? throw new ArgumentException("Missing phone_number");

        // WTelegram 通过配置回调处理登录流程。此处尝试触发登录以发送验证码。
        _ = Task.Run(async () =>
        {
            try
            {
                _ = await _client.LoginUserIfNeeded();
            }
            catch
            {
                // 预期：需要验证码/2FA 时会失败，但验证码发送可能已触发
            }
        });

        var response = new JsonObject
        {
            ["@type"] = "authorizationStateWaitCode",
            ["code_info"] = new JsonObject
            {
                ["@type"] = "authenticationCodeInfo",
                ["phone_number"] = phone,
                ["type"] = new JsonObject { ["@type"] = "authenticationCodeTypeSms" }
            }
        };

        return Task.FromResult(response.ToJsonString());
    }

    private async Task<string> CheckAuthCodeAsync(JsonObject request, CancellationToken ct)
    {
        var code = request["code"]?.GetValue<string>()
            ?? throw new ArgumentException("Missing code");

        // WTelegram.Client 通过配置回调处理验证码
        // 登录状态由 WTelegramClientManager 管理
        try
        {
            var user = await _client.LoginUserIfNeeded();
            var response = new JsonObject
            {
                ["@type"] = "authorizationStateReady"
            };
            return response.ToJsonString();
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("SESSION_PASSWORD_NEEDED", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("PASSWORD", StringComparison.OrdinalIgnoreCase))
            {
                return new JsonObject { ["@type"] = "authorizationStateWaitPassword" }.ToJsonString();
            }

            return new JsonObject { ["@type"] = "error", ["message"] = ex.Message }.ToJsonString();
        }
    }

    private async Task<string> CheckAuthPasswordAsync(JsonObject request, CancellationToken ct)
    {
        var password = request["password"]?.GetValue<string>()
            ?? throw new ArgumentException("Missing password");

        try
        {
            var user = await _client.LoginUserIfNeeded();
            var response = new JsonObject
            {
                ["@type"] = "authorizationStateReady"
            };
            return response.ToJsonString();
        }
        catch (Exception ex)
        {
            return new JsonObject { ["@type"] = "error", ["message"] = ex.Message }.ToJsonString();
        }
    }

    private async Task<string> GetMeAsync(CancellationToken ct)
    {
        try
        {
            var user = await _client.LoginUserIfNeeded();

            var response = new JsonObject
            {
                ["@type"] = "user",
                ["id"] = user.id,
                ["first_name"] = user.first_name ?? "",
                ["last_name"] = user.last_name ?? "",
                ["username"] = user.MainUsername ?? "",
                ["phone_number"] = user.phone ?? ""
            };

            return response.ToJsonString();
        }
        catch (Exception ex)
        {
            return new JsonObject
            {
                ["@type"] = "error",
                ["message"] = ex.Message
            }.ToJsonString();
        }
    }

    public ValueTask DisposeAsync()
    {
        // Client 由 Manager 管理，此处不释放
        return ValueTask.CompletedTask;
    }
}
