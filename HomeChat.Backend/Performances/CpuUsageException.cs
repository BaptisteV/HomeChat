using System.Runtime.Serialization;

namespace HomeChat.Backend.Performances
{
    [Serializable]
    internal class CpuUsageException : Exception
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

        protected CpuUsageException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}