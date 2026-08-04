using System.Collections.Immutable;
using System.Text.Json;
using Mocha.Middlewares;

namespace Mocha.Transport.Postgres;

/// <summary>
/// Parses a <see cref="PostgresMessageItem"/> read from the database into a normalized
/// <see cref="MessageEnvelope"/>, extracting standard message properties and custom headers
/// from the JSON headers column.
/// </summary>
internal sealed class PostgresMessageEnvelopeParser
{
    /// <summary>
    /// Converts a PostgreSQL message item into a <see cref="MessageEnvelope"/> by parsing the
    /// JSON headers and mapping them to envelope fields.
    /// </summary>
    /// <param name="messageItem">The message item read from the PostgreSQL message store.</param>
    /// <returns>A fully populated message envelope ready for the receive middleware pipeline.</returns>
    public MessageEnvelope Parse(PostgresMessageItem messageItem)
    {
        string? messageId = null;
        string? correlationId = null;
        string? conversationId = null;
        string? causationId = null;
        string? sourceAddress = null;
        string? destinationAddress = null;
        string? responseAddress = null;
        string? faultAddress = null;
        string? contentType = null;
        string? messageType = null;
        DateTimeOffset? deliverBy = null;
        DateTimeOffset? scheduledTime = null;
        ImmutableArray<string>? enclosedMessageTypes = null;
        Headers? customHeaders = null;

        if (!messageItem.Headers.IsEmpty)
        {
            try
            {
                var reader = new Utf8JsonReader(messageItem.Headers.Span);

                if (reader.Read() && reader.TokenType == JsonTokenType.StartObject)
                {
                    while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
                    {
                        if (reader.ValueTextEquals(PostgresMessageHeaders.MessageId))
                        {
                            reader.Read();
                            messageId = reader.TokenType == JsonTokenType.String ? reader.GetString() : null;
                        }
                        else if (reader.ValueTextEquals(PostgresMessageHeaders.CorrelationId))
                        {
                            reader.Read();
                            correlationId = reader.TokenType == JsonTokenType.String ? reader.GetString() : null;
                        }
                        else if (reader.ValueTextEquals(PostgresMessageHeaders.ConversationId))
                        {
                            reader.Read();
                            conversationId = reader.TokenType == JsonTokenType.String ? reader.GetString() : null;
                        }
                        else if (reader.ValueTextEquals(PostgresMessageHeaders.CausationId))
                        {
                            reader.Read();
                            causationId = reader.TokenType == JsonTokenType.String ? reader.GetString() : null;
                        }
                        else if (reader.ValueTextEquals(PostgresMessageHeaders.SourceAddress))
                        {
                            reader.Read();
                            sourceAddress = reader.TokenType == JsonTokenType.String ? reader.GetString() : null;
                        }
                        else if (reader.ValueTextEquals(PostgresMessageHeaders.DestinationAddress))
                        {
                            reader.Read();
                            destinationAddress = reader.TokenType == JsonTokenType.String ? reader.GetString() : null;
                        }
                        else if (reader.ValueTextEquals(PostgresMessageHeaders.ResponseAddress))
                        {
                            reader.Read();
                            responseAddress = reader.TokenType == JsonTokenType.String ? reader.GetString() : null;
                        }
                        else if (reader.ValueTextEquals(PostgresMessageHeaders.FaultAddress))
                        {
                            reader.Read();
                            faultAddress = reader.TokenType == JsonTokenType.String ? reader.GetString() : null;
                        }
                        else if (reader.ValueTextEquals(PostgresMessageHeaders.ContentType))
                        {
                            reader.Read();
                            contentType = reader.TokenType == JsonTokenType.String ? reader.GetString() : null;
                        }
                        else if (reader.ValueTextEquals(PostgresMessageHeaders.MessageType))
                        {
                            reader.Read();
                            messageType = reader.TokenType == JsonTokenType.String ? reader.GetString() : null;
                        }
                        else if (reader.ValueTextEquals(PostgresMessageHeaders.EnclosedMessageTypes))
                        {
                            reader.Read();

                            if (reader.TokenType == JsonTokenType.StartArray)
                            {
                                var builder = ImmutableArray.CreateBuilder<string>();

                                while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                                {
                                    if (reader.TokenType == JsonTokenType.String)
                                    {
                                        builder.Add(reader.GetString()!);
                                    }
                                }

                                enclosedMessageTypes = builder.ToImmutableArray();
                            }
                            else
                            {
                                reader.Skip();
                            }
                        }
                        else if (reader.ValueTextEquals(PostgresMessageHeaders.DeliverBy))
                        {
                            reader.Read();

                            if (reader.TokenType == JsonTokenType.String
                                && reader.TryGetDateTimeOffset(out var parsed))
                            {
                                deliverBy = parsed;
                            }
                        }
                        else if (reader.ValueTextEquals(PostgresMessageHeaders.ScheduledTime))
                        {
                            reader.Read();

                            if (reader.TokenType == JsonTokenType.String
                                && reader.TryGetDateTimeOffset(out var parsed))
                            {
                                scheduledTime = parsed;
                            }
                        }
                        else
                        {
                            var key = reader.GetString()!;
                            reader.Read();

                            object? value = reader.TokenType switch
                            {
                                JsonTokenType.String => reader.GetString(),
                                JsonTokenType.Number => reader.GetDouble(),
                                JsonTokenType.True => true,
                                JsonTokenType.False => false,
                                _ => null
                            };

                            if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
                            {
                                reader.Skip();
                            }

                            if (value is not null)
                            {
                                customHeaders ??= new Headers();
                                customHeaders.Set(key, value);
                            }
                        }
                    }
                }
            }
            catch (JsonException)
            {
                throw new InvalidOperationException(
                    "Failed to parse message headers from the database. The headers may be corrupted or in an invalid format.");
            }
        }

        return new MessageEnvelope
        {
            MessageId = messageId,
            CorrelationId = correlationId,
            ConversationId = conversationId,
            CausationId = causationId,
            SourceAddress = sourceAddress,
            DestinationAddress = destinationAddress,
            ResponseAddress = responseAddress,
            FaultAddress = faultAddress,
            ContentType = contentType,
            MessageType = messageType,
            EnclosedMessageTypes = enclosedMessageTypes,
            Headers = customHeaders ?? Headers.Empty(),
            SentAt = new DateTimeOffset(messageItem.SentTime, TimeSpan.Zero),
            DeliverBy = deliverBy,
            ScheduledTime = scheduledTime,
            DeliveryCount = messageItem.DeliveryCount,
            Body = messageItem.Body
        };
    }

    /// <summary>
    /// Shared singleton instance of the parser.
    /// </summary>
    public static readonly PostgresMessageEnvelopeParser Instance = new();
}
