namespace TgLitePanel.Infrastructure.Persistence.Entities;

public sealed class SharedCodeTokenEntity
{
    public required string Token { get; set; }
    public long AccountId { get; set; }
    public required string Code { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

