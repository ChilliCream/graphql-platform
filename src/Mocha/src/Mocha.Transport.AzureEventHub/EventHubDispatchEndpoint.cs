using Azure.Core.Amqp;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mocha.Middlewares;

namespace Mocha.Transport.AzureEventHub;

/// <summary>
/// Event Hub dispatch endpoint that publishes outbound messages to a target Event Hub
/// using singleton producer clients from the transport's connection manager.
/// </summary>
/// <param name="transport">The owning Event Hub transport instance.</param>
public sealed class EventHubDispatchEndpoint(EventHubMessagingTransport transport)
    : DispatchEndpoint<EventHubDispatchEndpointConfiguration>(transport)
{
    private EventHubBatchDispatcher? _batchDispatcher;
    private string? _partitionId;

    /// <summary>
    /// Gets the target topic for this endpoint.
    /// </summary>
    public EventHubTopic? Topic { get; private set; }

    /// <inheritdoc />
    protected override async ValueTask DispatchAsync(IDispatchContext context)
    {
        if (context.Envelope is not { } envelope)
        {
            throw new InvalidOperationException("Envelope is not set");
        }

        var cancellationToken = context.CancellationToken;

        // Resolve target hub name
        string hubName;
        if (Kind == DispatchEndpointKind.Reply)
        {
            if (!Uri.TryCreate(envelope.DestinationAddress, UriKind.Absolute, out var destinationAddress))
            {
                throw new InvalidOperationException("Destination address is not a valid URI");
            }

            hubName = ResolveReplyHubName(destinationAddress);
        }
        else
        {
            hubName = Topic?.Name
                ?? throw new InvalidOperationException("Topic is not set on dispatch endpoint");
        }

        var producer = transport.ConnectionManager.GetOrCreateProducer(hubName);

        // Size validation: Event Hubs has a 1MB message size limit.
        // This checks the body only — AMQP framing and properties add overhead, so actual
        // on-wire size will be slightly larger. The broker will reject events that exceed
        // the true limit; this is a fast-fail approximation to surface obvious violations early.
        if (envelope.Body.Length > 1_048_576)
        {
            throw new InvalidOperationException(
                $"Message body size ({envelope.Body.Length} bytes) exceeds the Event Hubs "
                + "maximum message size of 1MB. Consider splitting the message or using a "
                + "claim-check pattern.");
        }

        // Build EventData from envelope — zero-copy via ReadOnlyMemory<byte> constructor
        var eventData = new EventData(envelope.Body);

        // Map envelope headers -> AMQP structured properties (zero dictionary allocation path)
        var amqp = eventData.GetRawAmqpMessage();
        var props = amqp.Properties;

        if (envelope.MessageId is not null)
        {
            props.MessageId = new AmqpMessageId(envelope.MessageId);
        }

        if (envelope.CorrelationId is not null)
        {
            props.CorrelationId = new AmqpMessageId(envelope.CorrelationId);
        }

        if (envelope.ContentType is not null)
        {
            props.ContentType = envelope.ContentType;
        }

        // Use Subject for MessageType (structured AMQP property, no dict allocation)
        if (envelope.MessageType is not null)
        {
            props.Subject = envelope.MessageType;
        }

        if (envelope.ResponseAddress is not null)
        {
            props.ReplyTo = new AmqpAddress(envelope.ResponseAddress);
        }

        // Overflow headers go to ApplicationProperties
        var appProps = amqp.ApplicationProperties;

        if (envelope.ConversationId is not null)
        {
            appProps[EventHubMessageHeaders.ConversationId] = envelope.ConversationId;
        }

        if (envelope.CausationId is not null)
        {
            appProps[EventHubMessageHeaders.CausationId] = envelope.CausationId;
        }

        if (envelope.SourceAddress is not null)
        {
            appProps[EventHubMessageHeaders.SourceAddress] = envelope.SourceAddress;
        }

        if (envelope.DestinationAddress is not null)
        {
            appProps[EventHubMessageHeaders.DestinationAddress] = envelope.DestinationAddress;
        }

        if (envelope.FaultAddress is not null)
        {
            appProps[EventHubMessageHeaders.FaultAddress] = envelope.FaultAddress;
        }

        if (envelope.EnclosedMessageTypes is { Length: > 0 } types)
        {
            appProps[EventHubMessageHeaders.EnclosedMessageTypes] =
                types.Length == 1 ? types[0] : string.Join(";", types);
        }

        if (envelope.SentAt is not null)
        {
            appProps[EventHubMessageHeaders.SentAt] = envelope.SentAt.Value;
        }

        // Custom headers
        if (envelope.Headers is not null)
        {
            foreach (var header in envelope.Headers)
            {
                if (header.Value is not null)
                {
                    appProps[header.Key] = header.Value;
                }
            }
        }

        // Partition routing precedence:
        // 1. x-partition-id header (explicit partition targeting per-message)
        // 2. Configuration-level PartitionId (static per-endpoint partition)
        // 3. x-partition-key header (partition key routing for ordering)
        // 4. No partition targeting (round-robin, default)
        SendEventOptions? sendOptions = null;
        if (envelope.Headers?.TryGetValue("x-partition-id", out var partitionIdValue) == true
            && partitionIdValue is string headerPartitionId)
        {
            sendOptions = new SendEventOptions { PartitionId = headerPartitionId };
        }
        else if (_partitionId is { } configPartitionId)
        {
            sendOptions = new SendEventOptions { PartitionId = configPartitionId };
        }
        else if (envelope.Headers?.TryGetValue("x-partition-key", out var partitionKeyValue) == true
            && partitionKeyValue is string partitionKey)
        {
            sendOptions = new SendEventOptions { PartitionKey = partitionKey };
        }

        // Batch mode: enqueue into the batch dispatcher for deferred batched send.
        // Single mode: send immediately (allocates a single-element array per dispatch).
        if (_batchDispatcher is not null)
        {
            await _batchDispatcher.EnqueueAsync(eventData, sendOptions, cancellationToken);
        }
        else if (sendOptions is not null)
        {
            await producer.SendAsync([eventData], sendOptions, cancellationToken);
        }
        else
        {
            await producer.SendAsync([eventData], cancellationToken);
        }
    }

    /// <inheritdoc />
    protected override void OnInitialize(
        IMessagingConfigurationContext context,
        EventHubDispatchEndpointConfiguration configuration)
    {
        if (configuration.HubName is null)
        {
            throw new InvalidOperationException("Hub name is required");
        }
    }

    /// <inheritdoc />
    protected override void OnComplete(
        IMessagingConfigurationContext context,
        EventHubDispatchEndpointConfiguration configuration)
    {
        _partitionId = configuration.PartitionId;

        var topology = (EventHubMessagingTopology)Transport.Topology;

        if (configuration.HubName is not null)
        {
            Topic = topology.Topics.FirstOrDefault(t => t.Name == configuration.HubName)
                ?? throw new InvalidOperationException($"Topic '{configuration.HubName}' not found");
        }

        Destination = Topic
            ?? throw new InvalidOperationException("Destination is not set");

        var effectiveBatchMode = configuration.BatchMode
            ?? transport.TransportConfiguration.Defaults.DefaultBatchMode;

        if (effectiveBatchMode == EventHubBatchMode.Batch)
        {
            var producer = transport.ConnectionManager.GetOrCreateProducer(Topic.Name);
            var logger = context.Services.GetRequiredService<ILogger<EventHubBatchDispatcher>>();
            _batchDispatcher = new EventHubBatchDispatcher(producer, logger);
        }
    }

    /// <summary>
    /// Resolves the target hub name from a reply destination address by extracting the
    /// last path segment.
    /// </summary>
    /// <param name="destinationAddress">The destination URI carried on the envelope.</param>
    /// <returns>The hub name corresponding to the last segment of the URI path.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the path is empty or contains only separators.
    /// </exception>
    private static string ResolveReplyHubName(Uri destinationAddress)
    {
        var segments = destinationAddress.Segments;
        var lastSegment = segments.Length > 0 ? segments[^1].Trim('/') : string.Empty;

        if (lastSegment.Length == 0)
        {
            throw new InvalidOperationException(
                $"Cannot determine hub name from destination address '{destinationAddress}': path is empty.");
        }

        return lastSegment;
    }

    /// <summary>
    /// Disposes the batch dispatcher if one was created.
    /// </summary>
    internal ValueTask DisposeBatchDispatcherAsync()
    {
        if (_batchDispatcher is not null)
        {
            return _batchDispatcher.DisposeAsync();
        }

        return default;
    }
}
