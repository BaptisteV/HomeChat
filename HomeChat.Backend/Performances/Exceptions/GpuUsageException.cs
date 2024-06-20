namespace HomeChat.Backend.Performances.Exceptions;

public class GpuUsageException : Exception
{
    public GpuUsageException() { }

    public GpuUsageException(string? message) : base(message) { }

    public GpuUsageException(string? message, Exception? innerException) : base(message, innerException) { }
}