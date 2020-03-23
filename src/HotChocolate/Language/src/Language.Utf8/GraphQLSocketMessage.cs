using System;

namespace HotChocolate.Language
{
    public readonly ref struct GraphQLSocketMessage
    {
        public GraphQLSocketMessage(
            string type,
            string? id,
            ReadOnlySpan<byte> payload,
            bool hasPayload)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            Type = type;
            Id = id;
            Payload = payload;
            HasPayload = hasPayload;
        }

        public string? Id { get; }

        public string Type { get; }

        public ReadOnlySpan<byte> Payload { get; }

        public bool HasPayload { get; }
    }
}
