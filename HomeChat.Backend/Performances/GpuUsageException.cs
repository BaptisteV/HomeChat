using System.Runtime.Serialization;

namespace HomeChat.Backend.Performances
{
    [Serializable]
    internal class GpuUsageException : Exception
    {
        public GpuUsageException()
        {
        }

        public GpuUsageException(string? message) : base(message)
        {
        }

        public GpuUsageException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected GpuUsageException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}