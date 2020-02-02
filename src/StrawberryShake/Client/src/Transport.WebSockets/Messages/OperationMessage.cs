using System;

namespace StrawberryShake.Transport.WebSockets.Messages
{
    public class OperationMessage
    {
        public OperationMessage(string type)
        {
            if (string.IsNullOrEmpty(type))
            {
                // TODO : resources
                throw new ArgumentException(
                    "The message type mustn`t be null or empty.",
                    nameof(type));
            }

            Type = type;
        }

        public OperationMessage(string type, string id)
            : this(type)
        {
            Id = id;
        }

        public string? Id { get; }

        public string Type { get; }
    }
}
