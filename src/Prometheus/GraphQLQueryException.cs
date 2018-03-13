using System;
using System.Runtime.Serialization;

namespace Prometheus
{
    [Serializable]
    public class GraphQLQueryException
        : Exception
    {
        public GraphQLQueryException() { }

        public GraphQLQueryException(string[] messages)
        {
            Messages = messages ?? throw new ArgumentNullException(nameof(messages));
        }

        public GraphQLQueryException(string message)
            : base(message)
        {
            Messages = new string[] { message };
        }

        public GraphQLQueryException(string message, Exception inner)
            : base(message, inner)
        {
            Messages = new string[] { message };
        }

        protected GraphQLQueryException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
        }

        public string[] Messages { get; }
    }
}