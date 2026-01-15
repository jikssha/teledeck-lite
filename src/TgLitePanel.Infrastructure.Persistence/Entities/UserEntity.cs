namespace TgLitePanel.Infrastructure.Persistence.Entities;

public sealed class UserEntity
{
    public long Id { get; set; }
    public required string Username { get; set; }
    public required string PasswordHash { get; set; }
    public required string Role { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

