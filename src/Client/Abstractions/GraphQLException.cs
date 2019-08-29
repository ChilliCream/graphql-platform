using System;
using System.Runtime.Serialization;

namespace StrawberryShake
{
    [Serializable]
    public class GraphQLException
        : Exception
    {
        public GraphQLException() { }

        public GraphQLException(string message)
            : base(message) { }

        public GraphQLException(string message, Exception inner)
            : base(message, inner) { }

        protected GraphQLException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context) { }
    }
}
