using System;
using System.Runtime.Serialization;

namespace Zeus
{
    [Serializable]
    public class GraphQLQueryException 
        : Exception
    {
        public GraphQLQueryException() { }
        public GraphQLQueryException(string message) 
            : base(message) { }
        public GraphQLQueryException(string message, Exception inner) 
            : base(message, inner) { }
        protected GraphQLQueryException(
            SerializationInfo info,
            StreamingContext context) 
            : base(info, context) { }
    }
}