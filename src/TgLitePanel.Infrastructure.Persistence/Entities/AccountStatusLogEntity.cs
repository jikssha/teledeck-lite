namespace TgLitePanel.Infrastructure.Persistence.Entities;

/// <summary>
/// 账号状态历史记录实体
/// </summary>
public sealed class AccountStatusLogEntity
{
    public long Id { get; set; }
    public long AccountId { get; set; }
    public bool IsOnline { get; set; }
    public string? Error { get; set; }
    public DateTime CheckedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 检查来源：auto（自动）、manual（手动）
    /// </summary>
    public string Source { get; set; } = "auto";
}
