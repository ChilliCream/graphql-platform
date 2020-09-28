using System;
using System.Runtime.Serialization;

namespace HotChocolate.Utilities.Introspection
{
    [Serializable]
    public class IntrospectionException
        : Exception
    {
        public IntrospectionException() { }
        
        public IntrospectionException(string message)
            : base(message) { }

        public IntrospectionException(string message, Exception inner)
            : base(message, inner) { }

        protected IntrospectionException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context) { }
    }
}
