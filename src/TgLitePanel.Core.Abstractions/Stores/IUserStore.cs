namespace TgLitePanel.Core.Abstractions.Stores;

public interface IUserStore
{
    Task<UserRecord?> FindByUsernameAsync(string username, CancellationToken cancellationToken);
    Task<UserRecord?> FindByIdAsync(long userId, CancellationToken cancellationToken);
    Task<long> EnsureAdminAsync(string username, string passwordHash, CancellationToken cancellationToken);
}

