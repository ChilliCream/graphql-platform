using System.Text.Json;
using Mocha.Features;
using Mocha.Middlewares;
using static System.StringSplitOptions;

namespace Mocha.Transport.Postgres;

/// <summary>
/// A dispatch endpoint that sends messages to a PostgreSQL queue or publishes them through
/// a PostgreSQL topic using the message store.
/// </summary>
/// <remarks>
/// During completion the endpoint resolves its target resource from the topology. For reply
/// endpoints the destination is determined dynamically from the envelope's destination address
/// at dispatch time.
/// </remarks>
public sealed class PostgresDispatchEndpoint(PostgresMessagingTransport transport)
    : DispatchEndpoint<PostgresDispatchEndpointConfiguration>(transport)
{
    /// <summary>
    /// Gets the target queue, or <c>null</c> if this endpoint dispatches to a topic.
    /// </summary>
    public PostgresQueue? Queue { get; private set; }

    /// <summary>
    /// Gets the target topic, or <c>null</c> if this endpoint dispatches to a queue.
    /// </summary>
    public PostgresTopic? Topic { get; private set; }

    private PostgresMessagingTopology _topology = null!;

    protected override void OnInitialize(
        IMessagingConfigurationContext context,
        PostgresDispatchEndpointConfiguration configuration)
    {
        if (configuration.TopicName is null && configuration.QueueName is null)
        {
            throw new InvalidOperationException("Topic name or queue name is required");
        }
    }

    protected override async ValueTask DispatchAsync(IDispatchContext context)
    {
        if (context.Envelope is not { } envelope)
        {
            throw new InvalidOperationException("Envelope is not set");
        }

        var cancellationToken = context.CancellationToken;
        var messageStore = transport.MessageStore;

        var feature = context.Features.GetOrSet<JsonHeadersFeature>();
        var headers = WriteHeadersJson(feature, envelope);
        var body = envelope.Body;

        if (Kind == DispatchEndpointKind.Reply)
        {
            if (!Uri.TryCreate(envelope.DestinationAddress, UriKind.Absolute, out var destinationAddress))
            {
                throw new InvalidOperationException("Destination address is not a valid URI");
            }

            var path = destinationAddress.AbsolutePath.AsSpan();
            Span<Range> ranges = stackalloc Range[2];
            var segmentCount = path.Split(ranges, '/', RemoveEmptyEntries | TrimEntries);

            if (segmentCount == 2)
            {
                var kind = path[ranges[0]];
                var name = path[ranges[1]];

                if (kind is "t")
                {
                    await messageStore.PublishAsync(body, headers, new string(name), cancellationToken);
                    return;
                }

                if (kind is "q")
                {
                    await messageStore.SendAsync(body, headers, new string(name), cancellationToken);
                    return;
                }
            }

            throw new InvalidOperationException(
                $"Cannot determine topic or queue name from destination address {destinationAddress}");
        }

        if (Topic is not null)
        {
            await messageStore.PublishAsync(body, headers, Topic.Name, cancellationToken);
        }
        else if (Queue is not null)
        {
            await messageStore.SendAsync(body, headers, Queue.Name, cancellationToken);
        }
        else
        {
            throw new InvalidOperationException("Resource not found");
        }
    }

    protected override void OnComplete(
        IMessagingConfigurationContext context,
        PostgresDispatchEndpointConfiguration configuration)
    {
        _topology = (PostgresMessagingTopology)Transport.Topology;

        if (configuration.TopicName is not null)
        {
            Topic =
                _topology.Topics.FirstOrDefault(e => e.Name == configuration.TopicName)
                ?? throw new InvalidOperationException("Topic not found");
        }
        else if (configuration.QueueName is not null)
        {
            Queue =
                _topology.Queues.FirstOrDefault(q => q.Name == configuration.QueueName)
                ?? throw new InvalidOperationException("Queue not found");
        }

        Destination =
            Topic as TopologyResource
            ?? Queue as TopologyResource
            ?? throw new InvalidOperationException("Destination is not set");
    }

    private static ReadOnlyMemory<byte> WriteHeadersJson(JsonHeadersFeature feature, MessageEnvelope envelope)
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
