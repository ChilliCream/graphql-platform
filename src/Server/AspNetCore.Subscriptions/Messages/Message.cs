using System;
using System.Collections.Generic;
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

        public string Id { get; }

        public string Type { get; }
    }

    public class OperationMessage<T>
        : OperationMessage
    {
        public OperationMessage(string type, T payload)
            : base(type)
        {
            Payload = payload;
        }

        public OperationMessage(string type, string id, T payload)
            : base(type, id)
        {
            Payload = payload;
        }

        public T Payload { get; }
    }

    public class InitializeConnectionMessage
        : OperationMessage<IDictionary<string, object>>
    {

    }
}
