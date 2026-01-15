namespace TgLitePanel.Infrastructure.Persistence.Entities;

public sealed class AppConfigEntity
{
    public required string Key { get; set; }
    public required string Value { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

