using System;
using System.Text.Json;
using StrawberryShake.Http.Subscriptions.Messages;

namespace StrawberryShake.Http.Subscriptions
{
    public ref struct GraphQlMessageParser
    {
        private readonly ReadOnlySpan<byte> _messageData;
        private const byte _t = (byte)'t';
        private const byte _i = (byte)'i';
        private const byte _p = (byte)'p';

        private static ReadOnlySpan<byte> Type => new[]
        {
            (byte)'t',
            (byte)'y',
            (byte)'p',
            (byte)'e'
        };

        private static ReadOnlySpan<byte> Id => new[]
        {
            (byte)'i',
            (byte)'d'
        };

        private static ReadOnlySpan<byte> Payload => new[]
        {
            (byte)'p',
            (byte)'a',
            (byte)'y',
            (byte)'l',
            (byte)'o',
            (byte)'a',
            (byte)'d'
        };

        private Utf8JsonReader _reader;

        public GraphQlMessageParser(ReadOnlySpan<byte> messageData)
        {
            _messageData = messageData;
            _reader = new Utf8JsonReader(messageData);
        }

        public GraphQLSocketMessage ParseMessage()
        {
            _reader.Read();
            Expect(JsonTokenType.StartObject);

            var message = new Message();

            _reader.Read();
            while (_reader.TokenType != JsonTokenType.EndObject)
            {
                ParseMessageProperty(ref message);
                _reader.Read();
            }

            if (message.Type is null)
            {
                throw new InvalidOperationException(
                    "The GraphQL socket message had no type property specified.");
            }

            return new GraphQLSocketMessage(
                message.Type,
                message.Id,
                message.Payload
            );
        }

        private void ParseMessageProperty(ref Message message)
        {
            Expect(JsonTokenType.PropertyName);
            ReadOnlySpan<byte> fieldName = _reader.ValueSpan;

            _reader.Read();
            switch (fieldName[0])
            {
                case _t:
                    if (fieldName.SequenceEqual(Type))
                    {
                        Expect(JsonTokenType.String);
                        message.Type = _reader.GetString();
                    }

                    break;

                case _i:
                    if (fieldName.SequenceEqual(Id))
                    {
                        Expect(JsonTokenType.String);
                        message.Id = _reader.GetString();
                    }

                    break;

                case _p:
                    if (fieldName.SequenceEqual(Payload))
                    {
                        var start = _reader.TokenStartIndex;
                        _reader.Skip();
                        var end = _reader.BytesConsumed;

                        message.Payload =
                            _messageData.Slice((int)start, (int)(end - start));
                    }

                    break;

                default:
                    // TODO serialization exception
                    throw new InvalidOperationException();
            }
        }

        private ref struct Message
        {
            public string? Id { get; set; }

            public string? Type { get; set; }

            public ReadOnlySpan<byte> Payload { get; set; }
        }

        private void Expect(JsonTokenType type)
        {
            if (_reader.TokenType != type)
            {
                throw new InvalidOperationException("Invalid token");
            }
        }
    }
}
