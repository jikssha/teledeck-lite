namespace TgLitePanel.Core.Abstractions.Stores;

public sealed record UserRecord(long Id, string Username, string PasswordHash, string Role);

