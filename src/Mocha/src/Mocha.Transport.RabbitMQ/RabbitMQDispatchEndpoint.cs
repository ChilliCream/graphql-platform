using Mocha.Middlewares;
using RabbitMQ.Client;
using static System.StringSplitOptions;

namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// RabbitMQ dispatch endpoint that publishes outbound messages to a target queue or exchange
/// using pooled channels from the transport's dispatcher.
/// </summary>
/// <param name="transport">The owning RabbitMQ transport instance.</param>
public sealed class RabbitMQDispatchEndpoint(RabbitMQMessagingTransport transport)
    : DispatchEndpoint<RabbitMQDispatchEndpointConfiguration>(transport)
{
    /// <summary>
    /// Gets the target queue for this endpoint, or <c>null</c> if the endpoint targets an exchange.
    /// </summary>
    public RabbitMQQueue? Queue { get; private set; }

    /// <summary>
    /// Gets the target exchange for this endpoint, or <c>null</c> if the endpoint targets a queue.
    /// </summary>
    public RabbitMQExchange? Exchange { get; private set; }

    protected override async ValueTask DispatchAsync(IDispatchContext context)
    {
        if (context.Envelope is not { } envelope)
        {
            throw new InvalidOperationException("Envelope is not set");
        }

        var dispatcher = transport.Dispatcher;
        var cancellationToken = context.CancellationToken;
        var channel = await dispatcher.RentChannelAsync(cancellationToken);
        try
        {
            await EnsureProvisionedAsync(channel, cancellationToken);
            await DispatchAsync(channel, envelope, cancellationToken);
        }
        finally
        {
            await dispatcher.ReturnChannelAsync(channel);
        }
    }

    private async ValueTask DispatchAsync(
        IChannel channel,
        MessageEnvelope envelope,
        CancellationToken cancellationToken)
    {
        var exchangeName = CachedString.Empty;
        var routingKey = CachedString.Empty;
        if (Kind == DispatchEndpointKind.Reply)
        {
            if (!Uri.TryCreate(envelope.DestinationAddress, UriKind.Absolute, out var destinationAddress))
            {
                throw new InvalidOperationException("Destination address is not a valid URI");
            }

            var path = destinationAddress.AbsolutePath.AsSpan();
            Span<Range> ranges = stackalloc Range[3];
            var segmentCount = path.Split(ranges, '/', RemoveEmptyEntries | TrimEntries);

            int kindIndex,
                nameIndex;
            if (segmentCount == 3)
            {
                // vhost/kind/name — vhost adds an extra leading segment
                kindIndex = 1;
                nameIndex = 2;
            }
            else if (segmentCount == 2)
            {
                // kind/name — default vhost "/" disappears with RemoveEmptyEntries
                kindIndex = 0;
                nameIndex = 1;
            }
            else
            {
                throw new InvalidOperationException(
                    $"Cannot determine exchange or queue name from destination address {destinationAddress}");
            }

            var kind = path[ranges[kindIndex]];
            var name = path[ranges[nameIndex]];

            if (kind is "e" && name is var exchangeSegment)
            {
                exchangeName = new CachedString(new string(exchangeSegment));
                if (destinationAddress.TryGetRoutingKey(out var routingKeyValue))
                {
                    routingKey = new CachedString(routingKeyValue);
                }
            }
            else if (kind is "q" && name is var queueSegment)
            {
                routingKey = new CachedString(new string(queueSegment));
            }
            else
            {
                throw new InvalidOperationException(
                    $"Cannot determine exchange or queue name from destination address {destinationAddress}");
            }
        }
        else
        {
            if (Exchange is not null)
            {
                exchangeName = Exchange.CachedName;
            }
            else if (Queue is not null)
            {
                routingKey = Queue.CachedName;
            }
        }

        var headers = envelope.BuildHeaders();

        var messageType = envelope.MessageType ?? headers.Get(RabbitMQMessageHeaders.MessageType);

        var properties = new BasicProperties
        {
            MessageId = envelope.MessageId,
            CorrelationId = envelope.CorrelationId,
            Type = messageType,
            Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
            ReplyTo = envelope.ResponseAddress,
            Headers = headers,
            ContentType = envelope.ContentType,
            DeliveryMode = DeliveryModes.Persistent
            // TODO wire up durable
            // TODO expiration
            // TODO priority
        };

        await channel.BasicPublishAsync(exchangeName, routingKey, true, properties, envelope.Body, cancellationToken);
    }

    private bool _isProvisioned;

    private async ValueTask EnsureProvisionedAsync(IChannel channel, CancellationToken cancellationToken)
    {
        if (_isProvisioned)
        {
            return;
        }

        if (Queue is not null)
        {
            await Queue.ProvisionAsync(channel, cancellationToken);
        }

        if (Exchange is not null)
        {
            await Exchange.ProvisionAsync(channel, cancellationToken);
        }

        _isProvisioned = true;
    }

    protected override void OnInitialize(
        IMessagingConfigurationContext context,
        RabbitMQDispatchEndpointConfiguration configuration)
    {
        if (configuration.ExchangeName is null && configuration.QueueName is null)
        {
            throw new InvalidOperationException("Exchange name or queue name is required");
        }
    }

    protected override void OnComplete(
        IMessagingConfigurationContext context,
        RabbitMQDispatchEndpointConfiguration configuration)
    {
        var topology = (RabbitMQMessagingTopology)Transport.Topology;
        if (configuration.ExchangeName is not null)
        {
            Exchange =
                topology.Exchanges.FirstOrDefault(e => e.Name == configuration.ExchangeName)
                ?? throw new InvalidOperationException("Exchange not found");
        }
        else if (configuration.QueueName is not null)
        {
            Queue =
                topology.Queues.FirstOrDefault(q => q.Name == configuration.QueueName)
                ?? throw new InvalidOperationException("Queue not found");
        }

        Destination =
            Exchange as TopologyResource
            ?? Queue as TopologyResource
            ?? throw new InvalidOperationException("Destination is not set");
    }
}

/// <summary>
/// Extension methods for building RabbitMQ-specific message headers from a <see cref="MessageEnvelope"/>.
/// </summary>
public static class RabbitMQDispatchContextExtensions
{
    internal static IDictionary<string, object?> BuildHeaders(this MessageEnvelope envelope)
    {
        var headerCount =
            (envelope.ConversationId is not null ? 1 : 0)
            + (envelope.CausationId is not null ? 1 : 0)
            + (envelope.SourceAddress is not null ? 1 : 0)
            + (envelope.DestinationAddress is not null ? 1 : 0)
            + (envelope.FaultAddress is not null ? 1 : 0)
            + (envelope.Headers?.Count ?? 0);

        var headers = new Dictionary<string, object?>(headerCount);

        if (envelope.Headers is not null)
        {
            foreach (var header in envelope.Headers)
            {
                if (header.Value is DateTimeOffset dateTimeOffset)
                {
                    headers[header.Key] = new AmqpTimestamp(dateTimeOffset.ToUnixTimeSeconds());
                }
                else if (header.Value is DateTime dateTime)
                {
                    headers[header.Key] = new AmqpTimestamp(new DateTimeOffset(dateTime).ToUnixTimeSeconds());
                }
                else if (header.Value is not null)
                {
                    headers[header.Key] = header.Value;
                }
            }
        }

        if (envelope.ConversationId is not null)
        {
            headers.Set(RabbitMQMessageHeaders.ConversationId, envelope.ConversationId);
        }

        if (envelope.CausationId is not null)
        {
            headers.Set(RabbitMQMessageHeaders.CausationId, envelope.CausationId);
        }

        if (envelope.SourceAddress is not null)
        {
            headers.Set(RabbitMQMessageHeaders.SourceAddress, envelope.SourceAddress);
        }

        if (envelope.DestinationAddress is not null)
        {
            headers.Set(RabbitMQMessageHeaders.DestinationAddress, envelope.DestinationAddress);
        }

        if (envelope.FaultAddress is not null)
        {
            headers.Set(RabbitMQMessageHeaders.FaultAddress, envelope.FaultAddress);
        }

        if (envelope.EnclosedMessageTypes is { Length: > 0 })
        {
            headers.Set(RabbitMQMessageHeaders.EnclosedMessageTypes, envelope.EnclosedMessageTypes.Value);
        }

        if (envelope.MessageType is not null)
        {
            headers.Set(RabbitMQMessageHeaders.MessageType, envelope.MessageType);
        }

        return headers;
    }
}
