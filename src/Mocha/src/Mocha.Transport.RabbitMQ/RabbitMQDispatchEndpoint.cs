using Mocha.Middlewares;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using static System.StringSplitOptions;

namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// RabbitMQ dispatch endpoint that publishes outbound messages to a target queue or exchange
/// and waits for the broker to confirm each message.
/// </summary>
/// <param name="transport">The owning RabbitMQ transport instance.</param>
public sealed class RabbitMQDispatchEndpoint(RabbitMQMessagingTransport transport)
    : DispatchEndpoint<RabbitMQDispatchEndpointConfiguration>(transport)
{
    private volatile bool _isProvisioned;

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
        var envelope = context.Envelope ?? throw ThrowHelper.EnvelopeRequired();
        var dispatcher = transport.Dispatcher;
        var cancellationToken = context.CancellationToken;

        // The services of the caller's scope are read before the first wait, because the caller can dispose the
        // scope while the publish is in flight.
        var timeProvider = context.Services.GetTimeProvider();

        using var lease = await dispatcher.RentChannelAsync(cancellationToken);

        try
        {
            await EnsureProvisionedAsync(lease, cancellationToken);

            var properties = RabbitMQMessageEnvelopeFormatter.Format(envelope, timeProvider);
            properties.EnsureHeadersFit(lease.Connection);
            await PublishAsync(lease.Channel, envelope, properties, cancellationToken);

            // The channel is used again only when the broker answered the publish. A late answer must not
            // arrive during the next publish on the channel.
            lease.IsReusable = true;
        }
        catch (PublishException)
        {
            // The broker answered with a rejection or a return.
            lease.IsReusable = true;
            throw;
        }
    }

    private async ValueTask EnsureProvisionedAsync(ChannelLease lease, CancellationToken cancellationToken)
    {
        if (_isProvisioned)
        {
            return;
        }

        var topology = transport.Topology;

        // An exclusive queue belongs to the connection that declares it, which must be the consumer's.
        if (Queue is { Exclusive: false } queue && topology.ShouldProvision(queue))
        {
            await ProvisionQueueAsync(lease, queue, cancellationToken);
        }

        if (Exchange is { } exchange && topology.ShouldProvision(exchange))
        {
            await exchange
                .ProvisionAsync(lease.Channel, lease.Connection, cancellationToken)
                .WaitOrAbandonAsync(cancellationToken);
        }

        _isProvisioned = true;
    }

    /// <summary>
    /// Declares a queue, its bindings and the exchanges they start at.
    /// </summary>
    private Task ProvisionQueueAsync(ChannelLease lease, RabbitMQQueue queue, CancellationToken cancellationToken)
    {
        var provisioner = new RabbitMQTopologyProvisioner(transport.Topology, lease.Connection);
        return provisioner
            .ProvisionQueueAsync(queue, lease.Channel, cancellationToken)
            .WaitOrAbandonAsync(cancellationToken);
    }

    private ValueTask PublishAsync(
        IChannel channel,
        MessageEnvelope envelope,
        BasicProperties properties,
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

            int kindIndex;
            int nameIndex;

            if (segmentCount == 3)
            {
                // vhost/kind/name - vhost adds an extra leading segment
                kindIndex = 1;
                nameIndex = 2;
            }
            else if (segmentCount == 2)
            {
                // kind/name - default vhost "/" disappears with RemoveEmptyEntries
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

                if (envelope.Headers is not null
                    && envelope.Headers.TryGet(RabbitMQMessageHeaders.RoutingKey, out var rk))
                {
                    routingKey = new CachedString(rk);
                }
            }
            else if (Queue is not null)
            {
                routingKey = Queue.CachedName;
            }
        }

        return channel.BasicPublishAsync(
            exchangeName,
            routingKey,
            mandatory: false,
            properties,
            envelope.Body,
            cancellationToken);
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

file static class Extensions
{
    /// <summary>
    /// The bytes that a header frame holds in addition to the properties of the message: 20 bytes of framing,
    /// 29 bytes for the sequence number header and up to 600 bytes for the trace context that the client adds.
    /// </summary>
    private const int HeaderFrameReserve = 1024;

    /// <summary>
    /// Throws when the header frame of the message can exceed the frame size of the connection. The broker
    /// closes a connection that sends such a frame, which fails every publish in flight on it.
    /// </summary>
    public static void EnsureHeadersFit(this BasicProperties properties, IConnection connection)
    {
        // A frame size of zero means that the connection has no limit.
        var frameMax = connection.FrameMax;
        if (frameMax == 0)
        {
            return;
        }

        var size = ((IAmqpWriteable)properties).GetRequiredBufferSize();
        if (size + HeaderFrameReserve > frameMax)
        {
            throw ThrowHelper.MessageHeadersTooLarge(size, HeaderFrameReserve, frameMax);
        }
    }
}
