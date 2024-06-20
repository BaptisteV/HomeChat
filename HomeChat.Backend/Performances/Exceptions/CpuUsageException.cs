namespace HomeChat.Backend.Performances.Exceptions;

public class CpuUsageException : Exception
{
    public CpuUsageException()
    {
    }

    public CpuUsageException(string? message) : base(message)
    {
    }

    public CpuUsageException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}