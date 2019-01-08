using System;
using System.Runtime.Serialization;

namespace EmdatSSBEAService
{
    [Serializable]
    public class QueueActivationException : Exception
    {
        public QueueActivationException()
        {
        }

        public QueueActivationException(string message) : base(message)
        {
        }

        public QueueActivationException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected QueueActivationException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}