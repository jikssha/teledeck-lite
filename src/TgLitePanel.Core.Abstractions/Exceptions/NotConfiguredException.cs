namespace TgLitePanel.Core.Abstractions.Exceptions;

public sealed class NotConfiguredException : AppException
{
    public NotConfiguredException(string message) : base(message) { }
}

