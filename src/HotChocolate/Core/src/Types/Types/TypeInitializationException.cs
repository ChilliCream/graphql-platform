using System;

namespace HotChocolate.Types
{
    [Serializable]
    public class TypeInitializationException : Exception
    {
        public TypeInitializationException() { }

        public TypeInitializationException(string message)
            : base(message) { }

        public TypeInitializationException(string message, Exception inner)
            : base(message, inner) { }

        protected TypeInitializationException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}
