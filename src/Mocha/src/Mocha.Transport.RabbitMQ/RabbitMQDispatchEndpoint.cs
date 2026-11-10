using Mocha.Middlewares;
using Mocha.Threading;
using Mocha.Transport.RabbitMQ.Features;
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
    private static readonly TimeSpan s_initialRetryDelay = TimeSpan.FromMilliseconds(250);
    private static readonly TimeSpan s_maxRetryDelay = TimeSpan.FromSeconds(5);

    private bool _isProvisioned;

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
        var publish = context.Features.GetOrSet<RabbitMQPublishFeature>();
        publish.Start();

        try
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    await PublishOnceAsync(context, publish, envelope);
                    break;
                }
                catch (Exception ex)
                {
                    var isConnectionLoss = RabbitMQFailure.IsConnectionLoss(ex, dispatcher, cancellationToken);
                    await ReleaseFailedChannelAsync(publish, isConnectionLoss);

                    if (!isConnectionLoss)
                    {
                        throw;
                    }

                    // The message may have reached the broker before the connection was lost, so a retry can deliver
                    // it twice.
                    await BackOffAsync(context, publish, ex);
                }
            }

            await dispatcher.ReturnChannelAsync(publish.Channel!);
            dispatcher.LogPublishOutcome(context, publish, PublishOutcome.Confirmed, exception: null);
        }
        catch (Exception ex)
        {
            var outcome = cancellationToken.IsCancellationRequested ? PublishOutcome.Cancelled : PublishOutcome.Failed;
            dispatcher.LogPublishOutcome(context, publish, outcome, ex);
            throw;
        }
    }

    /// <summary>
    /// Publishes the message on a rented channel and waits for the broker's confirmation. The channel stays
    /// rented, and <paramref name="publish"/> tells the caller how to release it.
    /// </summary>
    private async ValueTask PublishOnceAsync(
        IDispatchContext context,
        RabbitMQPublishFeature publish,
        MessageEnvelope envelope)
    {
        var dispatcher = transport.Dispatcher;
        var cancellationToken = context.CancellationToken;
        var timeProvider = context.Services.GetTimeProvider();
        publish.Channel = null;
        publish.IsChannelReusable = false;

        if (transport.ReplyReceiveEndpoint is RabbitMQReceiveEndpoint replyEndpoint
            && envelope.ResponseAddress == replyEndpoint.Source.Address.ToString())
        {
            await dispatcher.RunPublishPhaseAsync(
                context,
                publish,
                PublishPhase.ReplyConsumer,
                replyEndpoint.WaitUntilHealthyAsync(cancellationToken));
        }

        var channel = await dispatcher.RunPublishPhaseAsync(
            context,
            publish,
            PublishPhase.ChannelCapacity,
            dispatcher.RentChannelAsync(this, cancellationToken));
        publish.Channel = channel;

        await dispatcher.RunPublishPhaseAsync(
            context,
            publish,
            PublishPhase.Topology,
            EnsureProvisionedAsync(channel, cancellationToken));
        publish.IsChannelReusable = true;

        var isQueueRestored = false;
        while (true)
        {
            try
            {
                await dispatcher.RunPublishPhaseAsync(
                    context,
                    publish,
                    PublishPhase.PublisherConfirm,
                    PublishAsync(channel, envelope, timeProvider, cancellationToken));
                return;
            }
            catch (PublishException ex) when (ex.IsReturn && Queue is { } queue)
            {
                if (isQueueRestored || queue.Exclusive || !transport.Topology.ShouldProvision(queue))
                {
                    throw ThrowHelper.MessageReturned(queue.Name, ex);
                }

                await RestoreQueueAsync(context, publish, channel, queue);
                isQueueRestored = true;
            }
        }
    }

    /// <summary>
    /// Declares the queue of a returned message and its bindings again.
    /// </summary>
    private async ValueTask RestoreQueueAsync(
        IDispatchContext context,
        RabbitMQPublishFeature publish,
        IChannel channel,
        RabbitMQQueue queue)
    {
        var dispatcher = transport.Dispatcher;
        publish.IsChannelReusable = false;
        dispatcher.LogReturnedQueueRepairStarted(context, channel, queue.Name);

        var provisioner = new RabbitMQTopologyProvisioner(transport.Topology, dispatcher.GetChannelConnection(channel));
        await dispatcher.RunPublishPhaseAsync(
            context,
            publish,
            PublishPhase.Topology,
            new ValueTask(provisioner.ProvisionQueueAsync(queue, channel, context.CancellationToken)));
        publish.IsChannelReusable = true;
        dispatcher.LogReturnedQueueRedeclared(context, channel, queue.Name);
    }

    /// <summary>
    /// Releases the channel of a failed attempt: back to the pool when it is still usable, retired otherwise.
    /// </summary>
    private ValueTask ReleaseFailedChannelAsync(RabbitMQPublishFeature publish, bool isConnectionLoss)
    {
        if (publish.Channel is not { } channel)
        {
            return ValueTask.CompletedTask;
        }

        if (publish.IsChannelReusable && channel.IsOpen && !isConnectionLoss)
        {
            return transport.Dispatcher.ReturnChannelAsync(channel);
        }

        transport.Dispatcher.DiscardChannel(channel);
        return ValueTask.CompletedTask;
    }

    private async ValueTask BackOffAsync(IDispatchContext context, RabbitMQPublishFeature publish, Exception exception)
    {
        publish.Retries++;
        var delay = ExponentialBackoff.CalculateDelay(publish.Retries, s_initialRetryDelay, s_maxRetryDelay);
        transport.Dispatcher.LogPublishRetry(context, publish, exception, delay);
        publish.Phase = PublishPhase.RecoveryBackoff;
        await Task.Delay(delay, context.CancellationToken);
    }

    private ValueTask PublishAsync(
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

        return channel.BasicPublishAsync(
            exchangeName,
            routingKey,
            mandatory,
            properties,
            envelope.Body,
            cancellationToken);
    }

    private async ValueTask EnsureProvisionedAsync(IChannel channel, CancellationToken cancellationToken)
    {
        if (_isProvisioned)
        {
            return;
        }

        var topology = transport.Topology;
        var connection = transport.Dispatcher.GetChannelConnection(channel);

        // An exclusive queue belongs to the connection that declares it, which must be the consumer's.
        if (Queue is { Exclusive: false } && topology.ShouldProvision(Queue))
        {
            var provisioner = new RabbitMQTopologyProvisioner(topology, connection);
            await provisioner.ProvisionQueueAsync(Queue, channel, cancellationToken);
        }

        if (Exchange is not null && topology.ShouldProvision(Exchange))
        {
            await Exchange.ProvisionAsync(channel, connection, cancellationToken);
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
