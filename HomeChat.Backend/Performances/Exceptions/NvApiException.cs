namespace HomeChat.Backend.Performances.Exceptions;

public class NvApiException : Exception
{
    public NvApiException() { }

    public NvApiException(string? message) : base(message) { }

    public NvApiException(string? message, Exception? innerException) : base(message, innerException) { }
}