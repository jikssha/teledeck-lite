using TgLitePanel.Core.Abstractions.Models;

namespace TgLitePanel.Infrastructure.Persistence.Entities;

public sealed class AccountEntity
{
    public long Id { get; set; }
    public required string Phone { get; set; }
    public AccountStatus Status { get; set; }
    public required string DataDir { get; set; }
    public int? ApiIdOverride { get; set; }
    public long? SystemChatId { get; set; }

    // 新增：分组管理
    public long? GroupId { get; set; }

    // 新增：状态监控
    public DateTime? LastOnlineUtc { get; set; }
    public DateTime? LastCheckedUtc { get; set; }
    public string? LastError { get; set; }
    public bool IsOnline { get; set; }

    // 新增：显示名称
    public string? DisplayName { get; set; }
    public string? Username { get; set; }

    // 新增：二级密码（用于导入时保存）
    public string? TwoFactorPassword { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
