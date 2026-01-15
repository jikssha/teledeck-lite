namespace TgLitePanel.Core.Abstractions.Models;

/// <summary>
/// 账号状态检查历史记录
/// </summary>
public sealed record AccountStatusLog
{
    public long Id { get; init; }
    public long AccountId { get; init; }
    public bool IsOnline { get; init; }
    public string? Error { get; init; }
    public DateTime CheckedAtUtc { get; init; }

    /// <summary>
    /// 检查来源：auto（自动）、manual（手动）
    /// </summary>
    public string Source { get; init; } = "auto";
}
