namespace TgLitePanel.Core.Abstractions.Models;

public sealed record SharedCodeDto(string Token, string Code, DateTimeOffset ExpiresAtUtc);

