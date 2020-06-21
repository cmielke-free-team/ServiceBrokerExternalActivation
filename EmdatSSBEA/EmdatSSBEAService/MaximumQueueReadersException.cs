using System;
using System.Runtime.Serialization;

namespace EmdatSSBEAService
{
    [Serializable]
    internal class MaximumQueueReadersException : Exception
    {
        public MaximumQueueReadersException() : this($"The maximum number of processes are already running")
        {
        }

        public MaximumQueueReadersException(int maxQueueReaders) : this($"The maximum number of processes are already running: {maxQueueReaders}")
        {
        }

        public MaximumQueueReadersException(string message) : base(message)
        {
        }

        public MaximumQueueReadersException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected MaximumQueueReadersException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}