using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Text.Json;

namespace Mocha.Middlewares;

/// <summary>
/// A ref struct writer that serializes a <see cref="MessageEnvelope"/> to JSON using a <see cref="Utf8JsonWriter"/>.
/// </summary>
/// <param name="writer">The UTF-8 JSON writer to write to.</param>
public ref struct MessageEnvelopeWriter(Utf8JsonWriter writer)
{
    /// <summary>
    /// Writes the specified message envelope as a JSON object.
    /// </summary>
    /// <param name="envelope">The message envelope to serialize.</param>
    public void WriteMessage(MessageEnvelope envelope)
    {
        writer.WriteStartObject();
        if (envelope.MessageId is not null)
        {
            writer.WriteString(MessageEnvelope.Properties.MessageId, envelope.MessageId);
        }

        if (envelope.CorrelationId is not null)
        {
            writer.WriteString(MessageEnvelope.Properties.CorrelationId, envelope.CorrelationId);
        }

        if (envelope.ConversationId is not null)
        {
            writer.WriteString(MessageEnvelope.Properties.ConversationId, envelope.ConversationId);
        }

        if (envelope.CausationId is not null)
        {
            writer.WriteString(MessageEnvelope.Properties.CausationId, envelope.CausationId);
        }

        if (envelope.SourceAddress is not null)
        {
            writer.WriteString(MessageEnvelope.Properties.SourceAddress, envelope.SourceAddress);
        }

        if (envelope.DestinationAddress is not null)
        {
            writer.WriteString(MessageEnvelope.Properties.DestinationAddress, envelope.DestinationAddress);
        }

        if (envelope.ResponseAddress is not null)
        {
            writer.WriteString(MessageEnvelope.Properties.ResponseAddress, envelope.ResponseAddress);
        }

        if (envelope.FaultAddress is not null)
        {
            writer.WriteString(MessageEnvelope.Properties.FaultAddress, envelope.FaultAddress);
        }

        if (envelope.ContentType is not null)
        {
            writer.WriteString(MessageEnvelope.Properties.ContentType, envelope.ContentType);
        }

        if (envelope.MessageType is not null)
        {
            writer.WriteString(MessageEnvelope.Properties.MessageType, envelope.MessageType);
        }

        if (envelope.EnclosedMessageTypes is not null)
        {
            writer.WriteStartArray(MessageEnvelope.Properties.EnclosedMessageTypes);
            foreach (var enclosedMessageType in envelope.EnclosedMessageTypes)
            {
                writer.WriteStringValue(enclosedMessageType);
            }

            writer.WriteEndArray();
        }

        if (envelope.SentAt is not null)
        {
            writer.WriteString(MessageEnvelope.Properties.SentAt, envelope.SentAt.Value);
        }

        if (envelope.DeliverBy is not null)
        {
            writer.WriteString(MessageEnvelope.Properties.DeliverBy, envelope.DeliverBy.Value);
        }

        if (envelope.DeliveryCount is not null)
        {
            writer.WriteNumber(MessageEnvelope.Properties.DeliveryCount, envelope.DeliveryCount.Value);
        }

        if (envelope.Headers is not null)
        {
            writer.WritePropertyName(MessageEnvelope.Properties.Headers);
            HeadersJsonConverter.Instance.Write(writer, envelope.Headers, HeadersJsonConverter.Options);
        }

        if (envelope.Body.Length > 0)
        {
            writer.WritePropertyName(MessageEnvelope.Properties.Body);
            writer.WriteRawValue(envelope.Body.Span);
        }

        writer.WriteEndObject();
    }
}

/// <summary>
/// A ref struct reader that deserializes a <see cref="MessageEnvelope"/> from JSON bytes.
/// </summary>
/// <param name="body">The raw JSON bytes to parse.</param>
public ref struct MessageEnvelopeReader(ReadOnlyMemory<byte> body)
{
    /// <summary>
    /// Parses the specified JSON bytes into a <see cref="MessageEnvelope"/>.
    /// </summary>
    /// <param name="body">The raw JSON bytes to parse.</param>
    /// <returns>The deserialized message envelope.</returns>
    public static MessageEnvelope Parse(ReadOnlyMemory<byte> body)
    {
        var parser = new MessageEnvelopeReader(body);
        return parser.ReadMessage();
    }

    private string? _messageId;
    private string? _correlationId;
    private string? _conversationId;
    private string? _causationId;
    private string? _sourceAddress;
    private string? _destinationAddress;
    private string? _responseAddress;
    private string? _faultAddress;
    private string? _contentType;
    private string? _messageType;
    private ImmutableArray<string>? _enclosedMessageTypes;
    private DateTimeOffset? _sentAt;
    private DateTimeOffset? _deliverBy;
    private int _attempt;
    private IHeaders? _headers;
    private ReadOnlyMemory<byte> _body;

    /// <summary>
    /// Reads and parses the JSON body into a <see cref="MessageEnvelope"/>.
    /// </summary>
    /// <returns>The deserialized message envelope.</returns>
    public MessageEnvelope ReadMessage()
    {
        var reader = new Utf8JsonReader(body.Span);
        ExpectStartObject(ref reader);

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                if (reader.ValueTextEquals(MessageEnvelope.Properties.MessageId))
                {
                    reader.Read();
                    _messageId = reader.GetString();
                }
                else if (reader.ValueTextEquals(MessageEnvelope.Properties.CorrelationId))
                {
                    reader.Read();
                    _correlationId = reader.GetString();
                }
                else if (reader.ValueTextEquals(MessageEnvelope.Properties.ConversationId))
                {
                    reader.Read();
                    _conversationId = reader.GetString();
                }
                else if (reader.ValueTextEquals(MessageEnvelope.Properties.CausationId))
                {
                    reader.Read();
                    _causationId = reader.GetString();
                }
                else if (reader.ValueTextEquals(MessageEnvelope.Properties.SourceAddress))
                {
                    reader.Read();
                    _sourceAddress = reader.GetString();
                }
                else if (reader.ValueTextEquals(MessageEnvelope.Properties.DestinationAddress))
                {
                    reader.Read();
                    _destinationAddress = reader.GetString();
                }
                else if (reader.ValueTextEquals(MessageEnvelope.Properties.ResponseAddress))
                {
                    reader.Read();
                    _responseAddress = reader.GetString();
                }
                else if (reader.ValueTextEquals(MessageEnvelope.Properties.FaultAddress))
                {
                    reader.Read();
                    _faultAddress = reader.GetString();
                }
                else if (reader.ValueTextEquals(MessageEnvelope.Properties.ContentType))
                {
                    reader.Read();
                    _contentType = reader.GetString();
                }
                else if (reader.ValueTextEquals(MessageEnvelope.Properties.MessageType))
                {
                    reader.Read();
                    _messageType = reader.GetString();
                }
                else if (reader.ValueTextEquals(MessageEnvelope.Properties.EnclosedMessageTypes))
                {
                    reader.Read();
                    _enclosedMessageTypes = ReadStringArray(ref reader);
                }
                else if (reader.ValueTextEquals(MessageEnvelope.Properties.SentAt))
                {
                    reader.Read();
                    _sentAt = reader.GetDateTimeOffset();
                }
                else if (reader.ValueTextEquals(MessageEnvelope.Properties.DeliverBy))
                {
                    reader.Read();
                    _deliverBy = reader.GetDateTimeOffset();
                }
                else if (reader.ValueTextEquals(MessageEnvelope.Properties.DeliveryCount))
                {
                    reader.Read();
                    _attempt = reader.GetInt32();
                }
                else if (reader.ValueTextEquals(MessageEnvelope.Properties.Headers))
                {
                    reader.Read();
                    _headers = HeadersJsonConverter.Instance.Read(
                        ref reader,
                        typeof(IHeaders),
                        HeadersJsonConverter.Options);
                }
                else if (reader.ValueTextEquals(MessageEnvelope.Properties.Body))
                {
                    reader.Read();
                    var before = reader.BytesConsumed - 1;
                    reader.Skip();
                    var after = reader.BytesConsumed;
                    _body = body.Slice((int)before, (int)(after - before));
                }
                else
                {
                    throw new JsonException($"Unknown property: {reader.GetString()}");
                }
            }
            else if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }
            else
            {
                throw new JsonException("Expected property name");
            }
        }

        if (reader.TokenType != JsonTokenType.EndObject)
        {
            throw new JsonException("Expected end object");
        }

        return new MessageEnvelope
        {
            MessageId = _messageId,
            CorrelationId = _correlationId,
            ConversationId = _conversationId,
            CausationId = _causationId,
            SourceAddress = _sourceAddress,
            DestinationAddress = _destinationAddress,
            ResponseAddress = _responseAddress,
            FaultAddress = _faultAddress,
            ContentType = _contentType,
            MessageType = _messageType,
            EnclosedMessageTypes = _enclosedMessageTypes,
            SentAt = _sentAt,
            DeliverBy = _deliverBy,
            DeliveryCount = _attempt,
            Headers = _headers,
            Body = _body
        };
    }

    private static void ExpectStartObject(ref Utf8JsonReader reader)
    {
        reader.Read();
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected start object");
        }
    }

    private ImmutableArray<string> ReadStringArray(ref Utf8JsonReader reader)
    {
        var builder = ImmutableArray.CreateBuilder<string>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                return builder.ToImmutableArray();
            }

            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException("Expected string");
            }

            if (reader.GetString() is { } value)
            {
                builder.Add(value);
            }
        }

        throw new JsonException("Expected end array");
    }
}
