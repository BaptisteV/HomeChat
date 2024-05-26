using System.Runtime.Serialization;

namespace HomeChat.Backend;

[Serializable]
internal class NvAPIWrapperException : Exception
{
    public NvAPIWrapperException()
    {
    }

    public NvAPIWrapperException(string? message) : base(message)
    {
    }

    public NvAPIWrapperException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    protected NvAPIWrapperException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}