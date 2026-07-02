using System.Text.Json;
using Mocha.Middlewares;

namespace Mocha.Transport.Postgres;

internal static class PostgresMessageHeadersWriter
{
    public static ReadOnlyMemory<byte> Write(JsonHeadersFeature feature, MessageEnvelope envelope)
    {
        using var writer = new Utf8JsonWriter(feature.Writer);

        writer.WriteStartObject();

        if (envelope.MessageId is not null)
        {
            writer.WriteString(PostgresMessageHeaders.MessageId, envelope.MessageId);
        }

        if (envelope.CorrelationId is not null)
        {
            writer.WriteString(PostgresMessageHeaders.CorrelationId, envelope.CorrelationId);
        }

        if (envelope.ConversationId is not null)
        {
            writer.WriteString(PostgresMessageHeaders.ConversationId, envelope.ConversationId);
        }

        if (envelope.CausationId is not null)
        {
            writer.WriteString(PostgresMessageHeaders.CausationId, envelope.CausationId);
        }

        if (envelope.SourceAddress is not null)
        {
            writer.WriteString(PostgresMessageHeaders.SourceAddress, envelope.SourceAddress);
        }

        if (envelope.DestinationAddress is not null)
        {
            writer.WriteString(PostgresMessageHeaders.DestinationAddress, envelope.DestinationAddress);
        }

        if (envelope.ResponseAddress is not null)
        {
            writer.WriteString(PostgresMessageHeaders.ResponseAddress, envelope.ResponseAddress);
        }

        if (envelope.FaultAddress is not null)
        {
            writer.WriteString(PostgresMessageHeaders.FaultAddress, envelope.FaultAddress);
        }

        if (envelope.ContentType is not null)
        {
            writer.WriteString(PostgresMessageHeaders.ContentType, envelope.ContentType);
        }

        if (envelope.MessageType is not null)
        {
            writer.WriteString(PostgresMessageHeaders.MessageType, envelope.MessageType);
        }

        if (envelope.EnclosedMessageTypes is { Length: > 0 } enclosedTypes)
        {
            writer.WriteStartArray(PostgresMessageHeaders.EnclosedMessageTypes);

            foreach (var type in enclosedTypes)
            {
                writer.WriteStringValue(type);
            }

            writer.WriteEndArray();
        }

        if (envelope.DeliverBy is { } deliverBy)
        {
            writer.WriteString(PostgresMessageHeaders.DeliverBy, deliverBy.ToString("O"));
        }

        if (envelope.ScheduledTime is { } scheduledTime)
        {
            writer.WriteString(PostgresMessageHeaders.ScheduledTime, scheduledTime.ToString("O"));
        }

        if (envelope.Headers is not null)
        {
            foreach (var header in envelope.Headers)
            {
                if (header.Value is not null)
                {
                    writer.WritePropertyName(header.Key);
                    JsonSerializer.Serialize(writer, header.Value, header.Value.GetType());
                }
            }
        }

        writer.WriteEndObject();
        writer.Flush();

        var bytes = feature.GetWrittenBytes();
        return bytes.Length <= 2 ? ReadOnlyMemory<byte>.Empty : bytes;
    }
}
