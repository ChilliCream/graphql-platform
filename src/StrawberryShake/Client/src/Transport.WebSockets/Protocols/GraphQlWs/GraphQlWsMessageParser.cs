using System;
using System.Buffers;
using System.Runtime.Serialization;
using System.Text.Json;
using StrawberryShake.Transport.WebSockets;

namespace StrawberryShake.Http.Subscriptions
{
    /// <summary>
    /// The <see cref="GraphQlWsMessageParser"/> parses a sequence of bytes into a
    /// <see cref="GraphQlWsMessage"/>
    /// </summary>
    internal ref struct GraphQlWsMessageParser
    {
        private readonly ReadOnlySequence<byte> _messageData;
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

        /// <summary>
        /// Initializes a new instance of <see cref="GraphQlWsMessageParser"/>
        /// </summary>
        /// <param name="messageData">
        /// The sequence of bytes containing the data of the message
        /// </param>
        private GraphQlWsMessageParser(ReadOnlySequence<byte> messageData)
        {
            _messageData = messageData;
            _reader = new Utf8JsonReader(messageData);
        }

        /// <summary>
        /// Parses the message out of the sequence
        /// </summary>
        /// <returns></returns>
        /// <exception cref="SerializationException">
        /// Thrown when a invalid token, a unknown field or the type is not specified
        /// </exception>
        private GraphQlWsMessage ParseMessage()
        {
            _reader.Read();
            Expect(JsonTokenType.StartObject);

            var message = new GraphQlWsMessage();

            _reader.Read();
            while (_reader.TokenType != JsonTokenType.EndObject)
            {
                ParseMessageProperty(ref message);
                _reader.Read();
            }

            if (message.Type is null)
            {
                throw ThrowHelper.Serialization_MessageHadNoTypeSpecified();
            }

            return message;
        }

        private void ParseMessageProperty(ref GraphQlWsMessage message)
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
                    throw ThrowHelper.Serialization_UnknownField(fieldName);
            }
        }

        private void Expect(JsonTokenType type)
        {
            if (_reader.TokenType != type)
            {
                throw ThrowHelper.Serialization_InvalidToken(_reader.ValueSpan);
            }
        }

        /// <summary>
        /// Parses a <see cref="GraphQlWsMessage"/> from a sequence of bytes
        /// </summary>
        /// <param name="messageData">
        /// The sequence of bytes containing the data of the message
        /// </param>
        /// <exception cref="SerializationException">
        /// Thrown when a invalid token, a unknown field or the type is not specified
        /// </exception>
        /// <returns>The parsed message</returns>
        public static GraphQlWsMessage Parse(ReadOnlySequence<byte> messageData)
        {
            return new GraphQlWsMessageParser(messageData).ParseMessage();
        }
    }
}
