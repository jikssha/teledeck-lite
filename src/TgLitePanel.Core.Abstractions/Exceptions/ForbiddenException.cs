namespace TgLitePanel.Core.Abstractions.Exceptions;

public sealed class ForbiddenException : AppException
{
    public ForbiddenException(string message) : base(message) { }
}

