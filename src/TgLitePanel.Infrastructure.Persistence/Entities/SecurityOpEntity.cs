namespace TgLitePanel.Infrastructure.Persistence.Entities;

public sealed class SecurityOpEntity
{
    public long Id { get; set; }
    public required string Kind { get; set; }
    public required string Status { get; set; }
    public int Total { get; set; }
    public int Processed { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

