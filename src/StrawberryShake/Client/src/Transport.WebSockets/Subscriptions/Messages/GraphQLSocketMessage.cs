using System;

namespace StrawberryShake.Http.Subscriptions.Messages
{
    public readonly ref struct GraphQLSocketMessage
    {
        public GraphQLSocketMessage(string type, string? id, ReadOnlySpan<byte> payload)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            Type = type;
            Id = id;
            Payload = payload;
        }

        public string? Id { get; }

        public string Type { get; }

        public ReadOnlySpan<byte> Payload { get; }
    }
}
