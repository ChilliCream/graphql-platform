using System;
using System.Runtime.Serialization;

namespace HotChocolate.Subscriptions.InMemory
{
    [Serializable]
    public class InvalidMessageTypeException : Exception
    {
        public InvalidMessageTypeException() { }

        public InvalidMessageTypeException(string message)
            : base(message) { }

        public InvalidMessageTypeException(string message, Exception inner)
            : base(message, inner) { }

        protected InvalidMessageTypeException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context) { }
    }
}
