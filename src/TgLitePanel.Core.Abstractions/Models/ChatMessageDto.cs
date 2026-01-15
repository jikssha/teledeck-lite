namespace TgLitePanel.Core.Abstractions.Models;

public sealed record ChatMessageDto(long MessageId, bool IsOutgoing, DateTimeOffset Date, string Text);

