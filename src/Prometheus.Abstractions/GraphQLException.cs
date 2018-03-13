using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Prometheus
{
    [Serializable]
    public class GraphQLException
        : Exception
    {
        private const string _messages = "messages";

        public GraphQLException() { }

        public GraphQLException(string[] messages)
            : base(CreateMessage(messages))
        {
            if (messages == null)
            {
                throw new ArgumentNullException(nameof(messages));
            }
            Messages = messages;
        }

        public GraphQLException(string message)
            : base(message)
        {
            Messages = new string[] { message };
        }

        public GraphQLException(string message, Exception innerException)
            : base(message, innerException)
        {
            Messages = new string[] { message };
        }

        protected GraphQLException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
            Messages = (string[])info.GetValue(_messages, typeof(string[]));
        }

        public IReadOnlyCollection<string> Messages { get; }

        public override void GetObjectData(
            SerializationInfo info,
            StreamingContext context)
        {
            info.AddValue(_messages, Messages.ToArray());
        }

        private static string CreateMessage(IEnumerable<string> messages)
        {
            return string.Join("\r\n", messages);
        }
    }
}