namespace TgLitePanel.Infrastructure.Persistence.Entities;

/// <summary>
/// 模块审计日志实体
/// </summary>
public sealed class ModuleAuditLogEntity
{
    public long Id { get; set; }
    public required string ModuleId { get; set; }
    public required string EventType { get; set; }
    public string? EventData { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string? UserId { get; set; }
}
