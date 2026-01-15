using System.Text.Json;

namespace TgLitePanel.Host.Services;

internal static class SystemMessageTextExtractor
{
    public static bool TryExtractPlainText(JsonElement message, out string text)
    {
        text = string.Empty;
        if (!message.TryGetProperty("content", out var content))
            return false;

        if (!content.TryGetProperty("@type", out var typeEl))
            return false;

        if (typeEl.GetString() != "messageText")
            return false;

        if (!content.TryGetProperty("text", out var t) || !t.TryGetProperty("text", out var tt))
            return false;

        text = tt.GetString() ?? string.Empty;
        return !string.IsNullOrWhiteSpace(text);
    }
}

