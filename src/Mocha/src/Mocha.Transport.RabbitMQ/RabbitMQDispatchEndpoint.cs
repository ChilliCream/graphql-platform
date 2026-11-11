using System.Diagnostics;
using Mocha.Middlewares;
using Mocha.Threading;
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
    // What a header frame holds beyond the properties of the message: 20 bytes of framing, the 29 bytes of the
    // sequence number header and, when a listener records the publish, the trace context the client adds
    // (a traceparent and a tracestate of at most 512 characters need 600 bytes).
    private const int HeaderFrameReserve = 1024;

    private static readonly TimeSpan s_initialRetryDelay = TimeSpan.FromMilliseconds(250);
    private static readonly TimeSpan s_maxRetryDelay = TimeSpan.FromSeconds(5);

    private volatile bool _isProvisioned;

    // Set while the exchange of the endpoint is known to be deleted. The broker deleted its bindings with it,
    // so they are declared again together with the exchange.
    private volatile bool _isExchangeMissing;

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
        var callerToken = context.CancellationToken;

        // The services of the caller's scope are read before the first wait, because the caller can dispose the
        // scope while the publish is in flight.
        var timeProvider = context.Services.GetTimeProvider();
        var started = Stopwatch.GetTimestamp();
        var attempt = 1;
        var isWritten = false;
        Exception? lastFailure = null;

        var deadline = dispatcher.StartPublish(callerToken);
        var cancellationToken = deadline?.Token ?? callerToken;

        try
        {
            while (true)
            {
                try
                {
                    var lease = await PrepareChannelAsync(envelope, cancellationToken);

                    // The attempt writes the message from here on. A failure before this point means that
                    // the broker did not receive it.
                    isWritten = true;
                    await PublishOnceAsync(context, envelope, lease, timeProvider, cancellationToken);
                    break;
                }
                catch (Exception ex) when (dispatcher.CanRetry(ex, cancellationToken))
                {
                    // The message may have reached the broker, so the next attempt can deliver it twice.
                    lastFailure = ex;
                    var delay = ExponentialBackoff.CalculateDelay(attempt, s_initialRetryDelay, s_maxRetryDelay);
                    dispatcher.LogPublishRetry(context, ex, attempt, delay);

                    // The next attempt waits for a connection by itself. The delay keeps a publish that fails
                    // on a ready connection from spinning.
                    await Task.Delay(delay, cancellationToken);
                    attempt++;
                }
            }

            dispatcher.LogPublishConfirmed(context, attempt, started);
        }
        catch (Exception ex)
        {
            if (callerToken.IsCancellationRequested)
            {
                dispatcher.LogPublishCancelled(context, attempt, started);
                if (ex is OperationCanceledException)
                {
                    throw ThrowHelper.PublishCancelled(ex, callerToken);
                }

                throw;
            }

            if (deadline is { IsCancellationRequested: true } && !dispatcher.IsDisposed)
            {
                var timedOut = lastFailure is PublishException { IsReturn: false }
                    ? ThrowHelper.PublishRejected(dispatcher.PublishTimeout, lastFailure)
                    : isWritten
                        ? ThrowHelper.PublishTimedOut(dispatcher.PublishTimeout, lastFailure ?? ex)
                        : ThrowHelper.PublishNotSent(dispatcher.PublishTimeout, lastFailure ?? ex);
                dispatcher.LogPublishFailed(context, timedOut, attempt, started);
                throw timedOut;
            }

            dispatcher.LogPublishFailed(context, ex, attempt, started);
            throw;
        }
        finally
        {
            dispatcher.FinishPublish(deadline);
        }
    }

    /// <summary>
    /// Leases a channel for one attempt of a publish, and declares the destination of the endpoint when
    /// that is still to do. Nothing of the message is written to the broker.
    /// </summary>
    private async ValueTask<PublisherLease> PrepareChannelAsync(
        MessageEnvelope envelope,
        CancellationToken cancellationToken)
    {
        var dispatcher = transport.Dispatcher;

        // A request is published once its reply queue exists and is consumed, so the reply cannot be dropped.
        if (transport.ReplyReceiveEndpoint is RabbitMQReceiveEndpoint replyEndpoint
            && envelope.ResponseAddress == replyEndpoint.Source.Address.ToString())
        {
            await replyEndpoint.WaitUntilAttachedAsync(cancellationToken);
        }

        var lease = await dispatcher.RentChannelAsync(cancellationToken);
        try
        {
            await EnsureProvisionedAsync(lease, cancellationToken);
            return lease;
        }
        catch
        {
            dispatcher.Return(lease, reusable: false);
            throw;
        }
    }

    /// <summary>
    /// Publishes the message on a leased channel and waits for the broker's answer. The channel goes back
    /// to the pool only when the broker answered: with a confirmation, a rejection or a return.
    /// </summary>
    private async ValueTask PublishOnceAsync(
        IDispatchContext context,
        MessageEnvelope envelope,
        PublisherLease lease,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var dispatcher = transport.Dispatcher;

        // The channel is reusable as long as nothing is written on it, and again when the broker answered.
        var isChannelReusable = true;
        try
        {
            var properties = RabbitMQMessageEnvelopeFormatter.Format(envelope, timeProvider);
            EnsureHeadersFit(properties, lease.Connection);
            isChannelReusable = false;

            PublishException returned;
            try
            {
                await PublishAsync(lease.Channel, envelope, properties, cancellationToken);
                isChannelReusable = true;
                return;
            }
            catch (PublishException ex)
            {
                isChannelReusable = true;
                if (!ex.IsReturn
                    && Exchange is { } exchange
                    && envelope.Headers?.GetMessageKind() == MessageKind.Publish)
                {
                    // An event goes to every queue that is bound to the exchange. The queues that accepted it
                    // have it, and another attempt would give each of them a copy.
                    throw ThrowHelper.PublishRejectedByExchange(exchange.Name, ex);
                }

                if (!ex.IsReturn || Queue is null)
                {
                    throw;
                }

                returned = ex;
            }
            catch (OperationInterruptedException ex)
                when (ex.ShutdownReason is { Initiator: ShutdownInitiator.Peer, ReplyCode: Constants.NotFound }
                    && Exchange is { } exchange
                    && transport.Topology.ShouldProvision(exchange))
            {
                // The exchange of the endpoint does not exist. The next publish declares it again, with the
                // bindings that the broker deleted together with it.
                _isExchangeMissing = true;
                _isProvisioned = false;
                throw;
            }

            // The queue of the endpoint does not exist.
            if (Queue.Exclusive || !transport.Topology.ShouldProvision(Queue))
            {
                throw ThrowHelper.MessageReturned(Queue.Name, returned);
            }

            isChannelReusable = false;
            dispatcher.LogMessageReturned(context, Queue.Name);
            await ProvisionQueueAsync(lease, Queue, cancellationToken);
            dispatcher.LogQueueRedeclared(context, Queue.Name);

            try
            {
                await PublishAsync(lease.Channel, envelope, properties, cancellationToken);
                isChannelReusable = true;
            }
            catch (PublishException ex)
            {
                isChannelReusable = true;
                if (ex.IsReturn)
                {
                    throw ThrowHelper.MessageReturned(Queue.Name, ex);
                }

                throw;
            }
        }
        finally
        {
            dispatcher.Return(lease, isChannelReusable);
        }
    }

    private async ValueTask EnsureProvisionedAsync(PublisherLease lease, CancellationToken cancellationToken)
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

            if (_isExchangeMissing)
            {
                await ProvisionBindingsAsync(lease, exchange, cancellationToken);
                _isExchangeMissing = false;
            }
        }

        _isProvisioned = true;
    }

    /// <summary>
    /// Declares the bindings that start or end at an exchange. A binding to an exclusive queue belongs to
    /// the connection of its consumer and is left to it.
    /// </summary>
    private async Task ProvisionBindingsAsync(
        PublisherLease lease,
        RabbitMQExchange exchange,
        CancellationToken cancellationToken)
    {
        foreach (var binding in exchange.Bindings)
        {
            if (binding is not RabbitMQQueueBinding { Destination.Exclusive: true }
                && transport.Topology.ShouldProvision(binding))
            {
                await binding
                    .ProvisionAsync(lease.Channel, lease.Connection, cancellationToken)
                    .WaitOrAbandonAsync(cancellationToken);
            }
        }
    }

    /// <summary>
    /// Declares a queue, its bindings and the exchanges they start at.
    /// </summary>
    private Task ProvisionQueueAsync(PublisherLease lease, RabbitMQQueue queue, CancellationToken cancellationToken)
    {
        var provisioner = new RabbitMQTopologyProvisioner(transport.Topology, lease.Connection);
        return provisioner
            .ProvisionQueueAsync(queue, lease.Channel, cancellationToken)
            .WaitOrAbandonAsync(cancellationToken);
    }

    /// <summary>
    /// Throws when the header frame of the message can exceed the frame size of the connection. The broker
    /// closes a connection that sends such a frame, which fails every publish in flight on it.
    /// </summary>
    private static void EnsureHeadersFit(BasicProperties properties, IConnection connection)
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
