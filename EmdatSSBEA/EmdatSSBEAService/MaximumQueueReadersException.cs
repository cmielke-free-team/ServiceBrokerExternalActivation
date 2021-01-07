using System;
using System.Runtime.Serialization;

namespace EmdatSSBEAService
{
    [Serializable]
    internal class MaximumQueueReadersException : Exception
    {
        public MaximumQueueReadersException()
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