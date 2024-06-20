namespace HomeChat.Backend.Performances.Exceptions;

public class RamUsageException : Exception
{
    public RamUsageException() { }

    public RamUsageException(string? message) : base(message) { }

    public RamUsageException(string? message, Exception? innerException) : base(message, innerException) { }
}