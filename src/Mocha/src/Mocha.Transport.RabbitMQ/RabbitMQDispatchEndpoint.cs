using System.Net.Sockets;
using Mocha.Middlewares;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
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
    private static readonly TimeSpan s_initialRetryDelay = TimeSpan.FromMilliseconds(250);
    private static readonly TimeSpan s_maxRetryDelay = TimeSpan.FromSeconds(5);

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
        var timeProvider = context.Services.GetTimeProvider();
        var attempt = 0;

        while (true)
        {
            IChannel? channel = null;
            try
            {
                channel = await dispatcher.RentChannelAsync(cancellationToken);
                await EnsureProvisionedAsync(channel, cancellationToken);
                await DispatchAsync(channel, envelope, timeProvider, cancellationToken);
            }
            catch (Exception ex)
            {
                if (channel is not null)
                {
                    dispatcher.DiscardChannel(channel);
                }

                if (!IsConnectionLoss(ex, dispatcher, cancellationToken))
                {
                    throw;
                }

                // The message may have reached the broker before the connection was lost, so a retry can deliver
                // it twice.
                attempt++;
                var delay = RabbitMQBackoff.GetDelay(attempt, s_initialRetryDelay, s_maxRetryDelay);
                dispatcher.LogPublishRetry(ex, attempt, delay);
                await Task.Delay(delay, cancellationToken);
                continue;
            }

            await dispatcher.ReturnChannelAsync(channel);
            return;
        }
    }

    private static bool IsConnectionLoss(
        Exception exception,
        RabbitMQDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested || dispatcher.IsShutDown)
        {
            return false;
        }

        return exception switch
        {
            // The broker rejected the message, and publishing it again fails the same way.
            PublishException => false,

            // A cancellation the caller did not request comes from a timed out operation on a broken connection.
            OperationCanceledException or ObjectDisposedException => true,
            BrokerUnreachableException { InnerException: AuthenticationFailureException } => false,
            BrokerUnreachableException or ConnectFailureException => true,
            OperationInterruptedException { ShutdownReason: var reason } => IsConnectionLoss(reason),
            IOException or SocketException => true,
            _ => false
        };
    }

    private static bool IsConnectionLoss(ShutdownEventArgs? reason)
        => reason is null
            || reason.Initiator != ShutdownInitiator.Peer
            || reason.ReplyCode
                is Constants.ConnectionForced
                or Constants.FrameError
                or Constants.SyntaxError
                or Constants.CommandInvalid
                or Constants.ChannelError
                or Constants.UnexpectedFrame
                or Constants.ResourceError
                or Constants.NotImplemented
                or Constants.InternalError;

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

        var properties = RabbitMQMessageEnvelopeFormatter.Format(envelope, timeProvider);

        // A message sent to a missing queue is confirmed and dropped unless the broker is asked to return it.
        // A reply whose requester is gone has no queue, and is dropped.
        var mandatory = Queue is not null && Kind != DispatchEndpointKind.Reply;

        try
        {
            await channel.BasicPublishAsync(exchangeName, routingKey, mandatory, properties, envelope.Body, cancellationToken);
        }
        catch (PublishException ex) when (ex.IsReturn && Queue is not null)
        {
            var autoProvision = ((RabbitMQMessagingTopology)transport.Topology).AutoProvision;

            // An exclusive queue belongs to the consumer connection, which declares it again when it re-attaches.
            if (Queue.Exclusive || !(Queue.AutoProvision ?? autoProvision))
            {
                throw ThrowHelper.MessageReturned(Queue.Name, ex);
            }

            // The queue was deleted after it was declared, for example by an operator or because it expired.
            await Queue.ProvisionAsync(channel, cancellationToken);
            transport.Dispatcher.LogReturnedQueueRedeclared(Queue.Name);

            try
            {
                await channel.BasicPublishAsync(exchangeName, routingKey, mandatory, properties, envelope.Body, cancellationToken);
            }
            catch (PublishException again) when (again.IsReturn)
            {
                throw ThrowHelper.MessageReturned(Queue.Name, again);
            }
        }
    }

    private bool _isProvisioned;

    private async ValueTask EnsureProvisionedAsync(IChannel channel, CancellationToken cancellationToken)
    {
        if (_isProvisioned)
        {
            return;
        }

        var autoProvision = ((RabbitMQMessagingTopology)transport.Topology).AutoProvision;

        // An exclusive queue belongs to the connection that declares it, which must be the consumer's.
        if (Queue is { Exclusive: false } && (Queue.AutoProvision ?? autoProvision))
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
