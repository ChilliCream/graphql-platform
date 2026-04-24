using System.Globalization;
using Azure.Messaging.ServiceBus;
using Mocha.Features;
using Mocha.Middlewares;
using static System.StringSplitOptions;

namespace Mocha.Transport.AzureServiceBus;

/// <summary>
/// A dispatch endpoint that sends messages to an Azure Service Bus queue or publishes them
/// through an Azure Service Bus topic using the client manager.
/// </summary>
/// <remarks>
/// During completion the endpoint resolves its target resource from the topology. For reply
/// endpoints the destination is determined dynamically from the envelope's destination address
/// at dispatch time.
/// </remarks>
public sealed class AzureServiceBusDispatchEndpoint(AzureServiceBusMessagingTransport transport)
    : DispatchEndpoint<AzureServiceBusDispatchEndpointConfiguration>(transport)
{
    /// <summary>
    /// Gets the target queue, or <c>null</c> if this endpoint dispatches to a topic.
    /// </summary>
    public AzureServiceBusQueue? Queue { get; private set; }

    /// <summary>
    /// Gets the target topic, or <c>null</c> if this endpoint dispatches to a queue.
    /// </summary>
    public AzureServiceBusTopic? Topic { get; private set; }

    protected override void OnInitialize(
        IMessagingConfigurationContext context,
        AzureServiceBusDispatchEndpointConfiguration configuration)
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

        var clientManager = transport.ClientManager;
        var cancellationToken = context.CancellationToken;

        await EnsureProvisionedAsync(cancellationToken);

        string entityPath;

        if (Kind == DispatchEndpointKind.Reply)
        {
            if (!Uri.TryCreate(envelope.DestinationAddress, UriKind.Absolute, out var destinationAddress))
            {
                throw new InvalidOperationException("Destination address is not a valid URI");
            }

            var path = destinationAddress.AbsolutePath.AsSpan();
            Span<Range> ranges = stackalloc Range[2];
            var segmentCount = path.Split(ranges, '/', RemoveEmptyEntries | TrimEntries);

            if (segmentCount != 2)
            {
                throw new InvalidOperationException(
                    $"Cannot determine topic or queue name from destination address {destinationAddress}");
            }

            var kind = path[ranges[0]];
            var name = path[ranges[1]];

            entityPath = new string(name);

            if (kind is not ("t" or "q"))
            {
                throw new InvalidOperationException(
                    $"Cannot determine topic or queue name from destination address {destinationAddress}");
            }
        }
        else if (Topic is not null)
        {
            entityPath = Topic.Name;
        }
        else if (Queue is not null)
        {
            entityPath = Queue.Name;
        }
        else
        {
            throw new InvalidOperationException("Destination not configured");
        }

        var sender = clientManager.GetSender(entityPath);
        var message = CreateMessage(envelope);

        if (envelope.ScheduledTime is { } scheduledTime)
        {
            var sequenceNumber = await sender.ScheduleMessageAsync(message, scheduledTime, cancellationToken);
            context.Features.Configure<ScheduledMessageFeature>(f =>
                f.Token = $"asb:{entityPath}:{sequenceNumber.ToString(CultureInfo.InvariantCulture)}");
        }
        else
        {
            await sender.SendMessageAsync(message, cancellationToken);
        }
    }

    private static ServiceBusMessage CreateMessage(MessageEnvelope envelope)
    {
        // Zero-copy: ReadOnlyMemory<byte> passed directly to ServiceBusMessage constructor
        var message = new ServiceBusMessage(envelope.Body)
        {
            MessageId = envelope.MessageId,
            CorrelationId = envelope.CorrelationId,
            ContentType = envelope.ContentType,
            Subject = envelope.MessageType,
            ReplyTo = envelope.ResponseAddress
        };

        if (envelope.DeliverBy is { } deliverBy)
        {
            var ttl = deliverBy - DateTimeOffset.UtcNow;
            if (ttl > TimeSpan.Zero)
            {
                message.TimeToLive = ttl;
            }
        }

        string? sessionId = null;
        if (envelope.Headers is { } envelopeHeaders)
        {
            if (envelopeHeaders.TryGetValue(AzureServiceBusMessageHeaders.SessionId, out var sessionIdValue)
                && sessionIdValue is string sessionIdString)
            {
                sessionId = sessionIdString;
                message.SessionId = sessionIdString;
            }

            if (envelopeHeaders.TryGetValue(AzureServiceBusMessageHeaders.PartitionKey, out var partitionKeyValue)
                && partitionKeyValue is string partitionKeyString)
            {
                if (sessionId is not null && partitionKeyString != sessionId)
                {
                    throw new InvalidOperationException(
                        "PartitionKey must equal SessionId when both are set on an Azure Service Bus message.");
                }

                message.PartitionKey = partitionKeyString;
            }
            else if (sessionId is not null)
            {
                // Default PartitionKey to SessionId for partitioned + session-aware entities.
                message.PartitionKey = sessionId;
            }

            if (envelopeHeaders.TryGetValue(AzureServiceBusMessageHeaders.ReplyToSessionId, out var replyToSessionIdValue)
                && replyToSessionIdValue is string replyToSessionIdString)
            {
                message.ReplyToSessionId = replyToSessionIdString;
            }

            if (envelopeHeaders.TryGetValue(AzureServiceBusMessageHeaders.To, out var toValue)
                && toValue is string toString)
            {
                message.To = toString;
            }
        }

        var props = message.ApplicationProperties;

        if (envelope.ConversationId is not null)
        {
            props[AzureServiceBusMessageHeaders.ConversationId] = envelope.ConversationId;
        }

        if (envelope.CausationId is not null)
        {
            props[AzureServiceBusMessageHeaders.CausationId] = envelope.CausationId;
        }

        if (envelope.SourceAddress is not null)
        {
            props[AzureServiceBusMessageHeaders.SourceAddress] = envelope.SourceAddress;
        }

        if (envelope.DestinationAddress is not null)
        {
            props[AzureServiceBusMessageHeaders.DestinationAddress] = envelope.DestinationAddress;
        }

        if (envelope.FaultAddress is not null)
        {
            props[AzureServiceBusMessageHeaders.FaultAddress] = envelope.FaultAddress;
        }

        if (envelope.MessageType is not null)
        {
            props[AzureServiceBusMessageHeaders.MessageType] = envelope.MessageType;
        }

        if (envelope.EnclosedMessageTypes is { Length: > 0 } types)
        {
            props[AzureServiceBusMessageHeaders.EnclosedMessageTypes] =
                types.Length == 1 ? types[0] : string.Join(";", types);
        }

        if (envelope.SentAt is { } sentAt)
        {
            props[AzureServiceBusMessageHeaders.SentAt] = sentAt.ToUnixTimeMilliseconds();
        }

        // User-defined headers. Framework-internal keys in the x-mocha-* namespace are already
        // mapped to native SDK properties above (and stripped on receive), so they must not leak
        // into ApplicationProperties.
        if (envelope.Headers is not null)
        {
            foreach (var header in envelope.Headers)
            {
                if (header.Key.StartsWith("x-mocha-", StringComparison.Ordinal))
                {
                    continue;
                }

                if (header.Value is DateTimeOffset dto)
                {
                    props[header.Key] = dto.ToUnixTimeMilliseconds();
                }
                else if (header.Value is DateTime dt)
                {
                    props[header.Key] = new DateTimeOffset(dt).ToUnixTimeMilliseconds();
                }
                else if (header.Value is not null)
                {
                    props[header.Key] = header.Value;
                }
            }
        }

        return message;
    }

    private int _isProvisioned; // 0 = false, 1 = true

    private async ValueTask EnsureProvisionedAsync(CancellationToken cancellationToken)
    {
        if (Volatile.Read(ref _isProvisioned) == 1)
        {
            return;
        }

        // Only one thread provisions
        if (Interlocked.CompareExchange(ref _isProvisioned, 1, 0) != 0)
        {
            return;
        }

        try
        {
            var autoProvision = ((AzureServiceBusMessagingTopology)transport.Topology).AutoProvision;

            if (Queue is not null && (Queue.AutoProvision ?? autoProvision))
            {
                await Queue.ProvisionAsync(transport.ClientManager, cancellationToken);
            }

            if (Topic is not null && (Topic.AutoProvision ?? autoProvision))
            {
                await Topic.ProvisionAsync(transport.ClientManager, cancellationToken);
            }
        }
        catch
        {
            Volatile.Write(ref _isProvisioned, 0); // Reset on failure
            throw;
        }
    }

    protected override void OnComplete(
        IMessagingConfigurationContext context,
        AzureServiceBusDispatchEndpointConfiguration configuration)
    {
        var topology = (AzureServiceBusMessagingTopology)Transport.Topology;

        if (configuration.TopicName is not null)
        {
            Topic =
                topology.Topics.FirstOrDefault(t => t.Name == configuration.TopicName)
                ?? throw new InvalidOperationException("Topic not found");
        }
        else if (configuration.QueueName is not null)
        {
            Queue =
                topology.Queues.FirstOrDefault(q => q.Name == configuration.QueueName)
                ?? throw new InvalidOperationException("Queue not found");
        }

        Destination =
            Topic as TopologyResource
            ?? Queue as TopologyResource
            ?? throw new InvalidOperationException("Destination is not set");
    }
}
