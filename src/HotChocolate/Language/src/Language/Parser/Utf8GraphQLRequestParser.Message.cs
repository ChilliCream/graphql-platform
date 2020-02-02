using System;

namespace HotChocolate.Language
{
    public ref partial struct Utf8GraphQLRequestParser
    {
        private ref struct Message
        {
            public string? Id { get; set; }

            public string? Type { get; set; }

            public ReadOnlySpan<byte> Payload { get; set; }

            public bool HasPayload { get; set; }
        }
    }
}
