namespace TgLitePanel.Infrastructure.Persistence.Entities;

public sealed class AuditLogEntity
{
    public long Id { get; set; }
    public long? UserId { get; set; }
    public required string Action { get; set; }
    public required string Summary { get; set; }
    public string? Ip { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

