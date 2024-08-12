using System.Buffers;
using System.Runtime.Serialization;
using System.Text.Json;

namespace StrawberryShake.Transport.WebSockets.Protocols;

/// <summary>
/// The <see cref="GraphQLWebSocketMessageParser"/> parses a sequence of bytes into a
/// <see cref="GraphQLWebSocketMessage"/>
/// </summary>
internal ref struct GraphQLWebSocketMessageParser
{
    private readonly ReadOnlySequence<byte> _messageData;
    private const byte _a = (byte)'a';
    private const byte _c = (byte)'c';
    private const byte _d = (byte)'d';
    private const byte _e = (byte)'e';
    private const byte _i = (byte)'i';
    private const byte _k = (byte)'k';
    private const byte _m = (byte)'m';
    private const byte _p = (byte)'p';
    private const byte _s = (byte)'s';
    private const byte _t = (byte)'t';

    private static ReadOnlySpan<byte> Type =>
    [
        (byte)'t',
        (byte)'y',
        (byte)'p',
        (byte)'e',
    ];

    private static ReadOnlySpan<byte> Id =>
    [
        (byte)'i',
        (byte)'d',
    ];

    private static ReadOnlySpan<byte> Payload =>
    [
        (byte)'p',
        (byte)'a',
        (byte)'y',
        (byte)'l',
        (byte)'o',
        (byte)'a',
        (byte)'d',
    ];

    private Utf8JsonReader _reader;

    /// <summary>
    /// Initializes a new instance of <see cref="GraphQLWebSocketMessageParser"/>
    /// </summary>
    /// <param name="messageData">
    /// The sequence of bytes containing the data of the message
    /// </param>
    private GraphQLWebSocketMessageParser(ReadOnlySequence<byte> messageData)
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
    private GraphQLWebSocketMessage ParseMessage()
    {
        _reader.Read();
        Expect(JsonTokenType.StartObject);

        var message = new GraphQLWebSocketMessage();

        _reader.Read();
        while (_reader.TokenType != JsonTokenType.EndObject)
        {
            ParseMessageProperty(ref message);
            _reader.Read();
        }

        if (message.Type == GraphQLWebSocketMessageType.None)
        {
            throw ThrowHelper.Serialization_MessageHadNoTypeSpecified();
        }

        return message;
    }

    private void ParseMessageProperty(ref GraphQLWebSocketMessage message)
    {
        Expect(JsonTokenType.PropertyName);
        var fieldName = _reader.ValueSpan;

        _reader.Read();
        switch (fieldName[0])
        {
            case _t:
                if (fieldName.SequenceEqual(Type))
                {
                    Expect(JsonTokenType.String);
                    message.Type = ParseMessageType();
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
                    message.Payload = JsonDocument.ParseValue(ref _reader);
                }

                break;

            default:
                throw ThrowHelper.Serialization_UnknownField(fieldName);
        }
    }

    private GraphQLWebSocketMessageType ParseMessageType()
    {
        var typeName = _reader.ValueSpan;
        if (typeName.IsEmpty)
        {
            throw ThrowHelper.Serialization_MessageHadNoTypeSpecified();
        }

        switch (typeName[0])
        {
            case _k:
                if (typeName.SequenceEqual(GraphQLWebSocketMessageTypeSpans.KeepAlive))
                {
                    return GraphQLWebSocketMessageType.KeepAlive;
                }

                break;
            case _d:
                if (typeName.SequenceEqual(GraphQLWebSocketMessageTypeSpans.Data))
                {
                    return GraphQLWebSocketMessageType.Data;
                }

                break;
            case _e:
                if (typeName.SequenceEqual(GraphQLWebSocketMessageTypeSpans.Error))
                {
                    return GraphQLWebSocketMessageType.Error;
                }

                break;
            case _s when typeName[2] is _a:
                if (typeName.SequenceEqual(GraphQLWebSocketMessageTypeSpans.Start))
                {
                    return GraphQLWebSocketMessageType.Start;
                }

                break;
            case _s:
                if (typeName.SequenceEqual(GraphQLWebSocketMessageTypeSpans.Stop))
                {
                    return GraphQLWebSocketMessageType.Stop;
                }

                break;
            case _c when typeName[2] is _m:
                if (typeName.SequenceEqual(GraphQLWebSocketMessageTypeSpans.Complete))
                {
                    return GraphQLWebSocketMessageType.Complete;
                }

                break;
            case _c when typeName[11] is _i:
                if (typeName.SequenceEqual(
                        GraphQLWebSocketMessageTypeSpans.ConnectionInitialize))
                {
                    return GraphQLWebSocketMessageType.ConnectionInit;
                }

                break;
            case _c when typeName[11] is _a:
                if (typeName.SequenceEqual(GraphQLWebSocketMessageTypeSpans.ConnectionAccept))
                {
                    return GraphQLWebSocketMessageType.ConnectionAccept;
                }

                break;
            case _c when typeName[11] is _e:
                if (typeName.SequenceEqual(GraphQLWebSocketMessageTypeSpans.ConnectionError))
                {
                    return GraphQLWebSocketMessageType.ConnectionError;
                }

                break;
            case _c when typeName[11] is _t:
                if (typeName.SequenceEqual(GraphQLWebSocketMessageTypeSpans.ConnectionTerminate)
                   )
                {
                    return GraphQLWebSocketMessageType.ConnectionTerminate;
                }

                break;
        }

        throw ThrowHelper.Serialization_InvalidMessageType(typeName);
    }

    private void Expect(JsonTokenType type)
    {
        if (_reader.TokenType != type)
        {
            throw ThrowHelper.Serialization_InvalidToken(_reader.ValueSpan);
        }
    }

    /// <summary>
    /// Parses a <see cref="GraphQLWebSocketMessage"/> from a sequence of bytes
    /// </summary>
    /// <param name="messageData">
    /// The sequence of bytes containing the data of the message
    /// </param>
    /// <exception cref="SerializationException">
    /// Thrown when a invalid token, a unknown field or the type is not specified
    /// </exception>
    /// <returns>The parsed message</returns>
    public static GraphQLWebSocketMessage Parse(ReadOnlySequence<byte> messageData)
    {
        return new GraphQLWebSocketMessageParser(messageData).ParseMessage();
    }
}
