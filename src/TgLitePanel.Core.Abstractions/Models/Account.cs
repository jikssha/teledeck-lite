namespace TgLitePanel.Core.Abstractions.Models;

/// <summary>
/// 账号信息模型
/// </summary>
public sealed class Account
{
    public long Id { get; set; }
    public required string Phone { get; set; }
    public AccountStatus Status { get; set; }
    public string? DisplayName { get; set; }
    public string? Username { get; set; }
    public long? GroupId { get; set; }
    public string? GroupName { get; set; }
    public bool IsOnline { get; set; }
    public DateTime? LastOnlineUtc { get; set; }
    public DateTime? LastCheckedUtc { get; set; }
    public string? LastError { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
