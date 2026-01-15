namespace TgLitePanel.Core.Abstractions.Exceptions;

public sealed class ValidationException : AppException
{
    public ValidationException(string message) : base(message) { }
}

