namespace TgLitePanel.Core.Abstractions.Exceptions;

public sealed class NotFoundException : AppException
{
    public NotFoundException(string message) : base(message) { }
}

