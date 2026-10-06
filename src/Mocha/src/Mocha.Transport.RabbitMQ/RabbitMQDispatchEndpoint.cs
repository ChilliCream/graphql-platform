using Mocha.Middlewares;
using RabbitMQ.Client;
using static System.StringSplitOptions;

namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// RabbitMQ dispatch endpoint that publishes outbound messages to a target queue or exchange
/// using pooled channels from the transport's dispatcher.
/// </summary>
/// <remarks>
/// The target is addressed by name. A target that is part of the transport topology is provisioned
/// according to its auto-provision setting; a target outside the topology is published to by name
/// and is never declared, bound, or consumed.
/// </remarks>
/// <param name="transport">The owning RabbitMQ transport instance.</param>
public sealed class RabbitMQDispatchEndpoint(RabbitMQMessagingTransport transport)
    : DispatchEndpoint<RabbitMQDispatchEndpointConfiguration>(transport)
{
    private CachedString _exchangeName = CachedString.Empty;
    private CachedString _routingKey = CachedString.Empty;

    /// <summary>
    /// Gets the name of the target queue, or <c>null</c> if the endpoint targets an exchange.
    /// </summary>
    public string? QueueName { get; private set; }

    /// <summary>
    /// Gets the name of the target exchange, or <c>null</c> if the endpoint targets a queue.
    /// </summary>
    public string? ExchangeName { get; private set; }

    /// <summary>
    /// Gets the topology queue this endpoint targets, or <c>null</c> if the endpoint targets an
    /// exchange or a queue that is not part of the transport topology.
    /// </summary>
    public RabbitMQQueue? Queue { get; private set; }

    /// <summary>
    /// Gets the topology exchange this endpoint targets, or <c>null</c> if the endpoint targets a
    /// queue or an exchange that is not part of the transport topology.
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
        var timeProvider = context.Services.GetTimeProvider();
        var channel = await dispatcher.RentChannelAsync(cancellationToken);
        try
        {
            await EnsureProvisionedAsync(channel, cancellationToken);
            await DispatchAsync(channel, envelope, timeProvider, cancellationToken);
        }
        finally
        {
            await dispatcher.ReturnChannelAsync(channel);
        }
    }

    private async ValueTask DispatchAsync(
        IChannel channel,
        MessageEnvelope envelope,
        TimeProvider timeProvider,
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
            exchangeName = _exchangeName;
            routingKey = _routingKey;

            if (ExchangeName is not null
                && envelope.Headers is not null
                && envelope.Headers.TryGet(RabbitMQMessageHeaders.RoutingKey, out var rk))
            {
                routingKey = new CachedString(rk);
            }
        }

        var properties = RabbitMQMessageEnvelopeFormatter.Format(envelope, timeProvider);

        await channel.BasicPublishAsync(exchangeName, routingKey, false, properties, envelope.Body, cancellationToken);
    }

    private bool _isProvisioned;

    private async ValueTask EnsureProvisionedAsync(IChannel channel, CancellationToken cancellationToken)
    {
        if (_isProvisioned)
        {
            return;
        }

        var autoProvision = ((RabbitMQMessagingTopology)transport.Topology).AutoProvision;

        if (Queue is not null && (Queue.AutoProvision ?? autoProvision))
        {
            await Queue.ProvisionAsync(channel, cancellationToken);
        }

        if (Exchange is not null && (Exchange.AutoProvision ?? autoProvision))
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

        if (configuration.ExchangeName is { } exchangeName)
        {
            ExchangeName = exchangeName;
            Exchange = topology.Exchanges.FirstOrDefault(e => e.Name == exchangeName);
            _exchangeName = Exchange?.CachedName ?? new CachedString(exchangeName);
        }
        else if (configuration.QueueName is { } queueName)
        {
            QueueName = queueName;
            Queue = topology.Queues.FirstOrDefault(q => q.Name == queueName);
            _routingKey = Queue?.CachedName ?? new CachedString(queueName);
        }

        // The destination stays unset for a target outside the topology.
        Destination = ((TopologyResource?)Exchange ?? Queue)!;
    }
}
