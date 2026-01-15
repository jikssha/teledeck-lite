namespace TgLitePanel.Core.Abstractions.Models;

/// <summary>
/// 账号分组
/// </summary>
public sealed class AccountGroup
{
    public long Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string Color { get; set; } = "#1976D2";
    public int SortOrder { get; set; }
    public int AccountCount { get; set; }
}
