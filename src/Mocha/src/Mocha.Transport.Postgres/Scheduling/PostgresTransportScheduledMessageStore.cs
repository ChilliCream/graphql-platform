using System.Globalization;
using System.Text.Json;
using Mocha.Middlewares;
using Mocha.Scheduling;

namespace Mocha.Transport.Postgres.Scheduling;

/// <summary>
/// Implements <see cref="IScheduledMessageStore"/> for the Postgres transport by inserting rows
/// into the same transport message table the dispatch endpoint uses, with <c>scheduled_time</c>
/// populated. Cancellation deletes the inserted rows when they have not yet been claimed by a
/// consumer.
/// </summary>
internal sealed class PostgresTransportScheduledMessageStore(PostgresMessagingTransport transport)
    : IScheduledMessageStore
{
    private const string TokenPrefix = "postgres-transport:";

    public async ValueTask<string> PersistAsync(IDispatchContext context, CancellationToken cancellationToken)
    {
        if (context.Endpoint is not PostgresDispatchEndpoint endpoint)
        {
            throw new InvalidOperationException(
                "PostgresTransportScheduledMessageStore requires a PostgresDispatchEndpoint, "
                + $"but the dispatch context carries a '{context.Endpoint.GetType().Name}'.");
        }

        if (context.Envelope is not { } envelope)
        {
            throw new InvalidOperationException(
                "PostgresTransportScheduledMessageStore requires a serialized envelope on the dispatch context.");
        }

        if (envelope.ScheduledTime is not { } scheduledTime)
        {
            throw new InvalidOperationException(
                "PostgresTransportScheduledMessageStore requires the envelope to carry a scheduled time.");
        }

        var headers = WriteHeadersJson(envelope);

        if (endpoint.Topic is not null)
        {
            var ids = await transport.MessageStore.PublishScheduledAsync(
                envelope.Body,
                headers,
                endpoint.Topic.Name,
                scheduledTime,
                cancellationToken);

            return TokenPrefix + string.Join(',', ids.Select(id => id.ToString("D", CultureInfo.InvariantCulture)));
        }

        if (endpoint.Queue is not null)
        {
            var id = await transport.MessageStore.SendScheduledAsync(
                envelope.Body,
                headers,
                endpoint.Queue.Name,
                scheduledTime,
                cancellationToken);

            return id is { } sendId
                ? TokenPrefix + sendId.ToString("D", CultureInfo.InvariantCulture)
                : TokenPrefix;
        }

        throw new InvalidOperationException(
            "PostgresTransportScheduledMessageStore cannot resolve a topic or queue from the endpoint.");
    }

    public async ValueTask<bool> CancelAsync(string token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(token) || !token.StartsWith(TokenPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        var payload = token[TokenPrefix.Length..];
        if (payload.Length == 0)
        {
            return false;
        }

        var ids = new List<Guid>();
        foreach (var part in payload.Split(','))
        {
            if (Guid.TryParseExact(part, "D", out var id))
            {
                ids.Add(id);
            }
        }

        if (ids.Count == 0)
        {
            return false;
        }

        var deleted = await transport.MessageStore.CancelScheduledAsync(ids, cancellationToken);
        return deleted > 0;
    }

    private static ReadOnlyMemory<byte> WriteHeadersJson(MessageEnvelope envelope)
    {
        using var stream = new System.IO.MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
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
        }

        var bytes = stream.ToArray();
        return bytes.Length <= 2 ? ReadOnlyMemory<byte>.Empty : bytes;
    }
}
