namespace TgLitePanel.Core.Abstractions.Stores;

public interface IAuditLogStore
{
    Task WriteAsync(long? userId, string action, string summary, string? ip, CancellationToken cancellationToken);
}

