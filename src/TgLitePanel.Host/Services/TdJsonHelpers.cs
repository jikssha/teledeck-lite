using System.Text.Json;

namespace TgLitePanel.Host.Services;

internal static class TdJsonHelpers
{
    internal static JsonDocument Parse(string json)
        => JsonDocument.Parse(json, new JsonDocumentOptions { AllowTrailingCommas = true });

    internal static void ThrowIfError(JsonElement root)
    {
        if (root.TryGetProperty("@type", out var typeEl) && typeEl.ValueKind == JsonValueKind.String &&
            string.Equals(typeEl.GetString(), "error", StringComparison.OrdinalIgnoreCase))
        {
            var message = root.TryGetProperty("message", out var msgEl) && msgEl.ValueKind == JsonValueKind.String
                ? msgEl.GetString()
                : "TDLib 返回错误";
            throw new InvalidOperationException(message);
        }
    }
}

