namespace TgLitePanel.Core.Abstractions.Models;

/// <summary>
/// 审计日志记录
/// </summary>
public sealed record AuditLog
{
    public long Id { get; init; }
    public required string UserName { get; init; }
    public required string Action { get; init; }
    public required string Description { get; init; }
    public string? TargetId { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
    public required string Result { get; init; }
    public string? AdditionalData { get; init; }
    public DateTime CreatedAt { get; init; }
}
