namespace TgLitePanel.Infrastructure.Persistence.Entities;

/// <summary>
/// 账号分组实体
/// </summary>
public sealed class AccountGroupEntity
{
    public long Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string Color { get; set; } = "#1976D2";
    public int SortOrder { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
