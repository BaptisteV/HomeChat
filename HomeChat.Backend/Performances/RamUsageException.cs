using System.Runtime.Serialization;

namespace HomeChat.Backend.Performances
{
    [Serializable]
    internal class RamUsageException : Exception
    {
        public RamUsageException()
        {
        }

        public RamUsageException(string? message) : base(message)
        {
        }

        public RamUsageException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected RamUsageException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}