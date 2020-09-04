using System;

namespace HotChocolate.AspNetCore.Subscriptions.Messages
{
    public class OperationMessage
    {
        public OperationMessage(string type)
        {
            if (string.IsNullOrEmpty(type))
            {
                // TODO : resources
                throw new ArgumentException(
                    "The message type cannot be null or empty.",
                    nameof(type));
            }

            Type = type;
        }

        public OperationMessage(string type, string id)
            : this(type)
        {
            Id = id;
        }

        public virtual string? Id { get; }

        public string Type { get; }
    }
}
