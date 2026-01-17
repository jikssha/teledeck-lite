namespace TgLitePanel.Infrastructure.Persistence.Entities;

/// <summary>
/// 审计日志实体 - 记录系统关键操作
/// </summary>
public sealed class AuditLogEntity
{
    /// <summary>
    /// 主键ID
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 操作用户名
    /// </summary>
    public required string UserName { get; set; }

    /// <summary>
    /// 操作动作 (如: account.delete, account.export, account.import)
    /// </summary>
    public required string Action { get; set; }

    /// <summary>
    /// 操作描述 (用户友好的描述信息)
    /// </summary>
    public required string Description { get; set; }

    /// <summary>
    /// 目标资源ID (被操作的资源，如账号ID)
    /// </summary>
    public string? TargetId { get; set; }

    /// <summary>
    /// 客户端IP地址
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// User-Agent (浏览器信息)
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// 操作结果 (success, failed)
    /// </summary>
    public required string Result { get; set; }

    /// <summary>
    /// 额外数据 (JSON格式，可选)
    /// </summary>
    public string? AdditionalData { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

