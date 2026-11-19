using System.Buffers;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// The broker-side consumer on one channel. A lost or closed attachment is never used again.
/// </summary>
internal sealed class RabbitMQConsumerAttachment(
    IChannel channel,
    RabbitMQConnectionManager connections,
    RabbitMQDeliveryDispatcher dispatcher,
    RabbitMQConsumerLogger logger,
    string queueName) : AsyncDefaultBasicConsumer(channel)
{
    private static readonly CreateChannelOptions s_channelOptions = new(
        publisherConfirmationsEnabled: false,
        publisherConfirmationTrackingEnabled: false,
        consumerDispatchConcurrency: 1);

    private readonly CancellationTokenSource _handlers = new();
    private readonly TaskCompletionSource<ShutdownEventArgs?> _lost = new(
        TaskCreationOptions.RunContinuationsAsynchronously);
    private volatile bool _isRetired;

    /// <summary>
    /// Gets the consumer tag the broker assigned, or an empty string until the subscription is confirmed.
    /// </summary>
    public string Tag { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the token that is cancelled for the handlers of this attachment.
    /// </summary>
    public CancellationToken HandlerToken => _handlers.Token;

    /// <summary>
    /// Gets a task that completes when the channel shut down, with the reason, or when the broker cancelled
    /// the consumer, with <c>null</c>.
    /// </summary>
    public Task<ShutdownEventArgs?> Lost => _lost.Task;

    /// <summary>
    /// Gets whether the channel shut down or the broker cancelled the consumer.
    /// </summary>
    public bool IsLost => _lost.Task.IsCompleted;

    /// <summary>
    /// Gets whether no handler starts for deliveries of this attachment any more.
    /// </summary>
    public bool IsRetired => _isRetired;

    /// <summary>
    /// Opens a channel. The open does not end with <paramref name="cancellationToken"/>, but the wait for it
    /// does, and the channel is closed when the open completes later.
    /// </summary>
    /// <param name="connections">The manager that opens and closes the channel.</param>
    /// <param name="connection">The connection to open the channel on.</param>
    /// <param name="logger">The logger for failures of handler cancellation.</param>
    /// <param name="queueName">The name of the queue the attachment consumes.</param>
    /// <param name="dispatcher">Receives the deliveries of the attachment.</param>
    /// <param name="timeout">How long the broker can take to open the channel.</param>
    /// <param name="cancellationToken">Ends the wait for the open.</param>
    public static async Task<RabbitMQConsumerAttachment> OpenAsync(
        RabbitMQConnectionManager connections,
        IConnection connection,
        RabbitMQConsumerLogger logger,
        string queueName,
        RabbitMQDeliveryDispatcher dispatcher,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        // An open that is not started cannot time out and replace the connection of other consumers.
        cancellationToken.ThrowIfCancellationRequested();

        var opening = connections.OpenChannelAsync(connection, s_channelOptions, timeout);
        IChannel channel;
        try
        {
            channel = await opening.WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The channel is closed once it is open.
            _ = opening.CloseWhenOpenedAsync(connections);
            throw;
        }

        var attachment = new RabbitMQConsumerAttachment(channel, connections, dispatcher, logger, queueName);
        channel.ChannelShutdownAsync += (_, reason) =>
        {
            attachment.OnLost(reason);
            return Task.CompletedTask;
        };

        return attachment;
    }

    /// <summary>
    /// Runs <paramref name="prepareAsync"/> on the channel. A failure leaves the attachment open, and the
    /// caller closes it.
    /// </summary>
    /// <param name="connection">The connection the channel was opened on.</param>
    /// <param name="prepareAsync">Prepares the channel.</param>
    /// <param name="cancellationToken">Ends the wait for the preparation.</param>
    public Task PrepareAsync(
        IConnection connection,
        Func<IConnection, IChannel, CancellationToken, Task> prepareAsync,
        CancellationToken cancellationToken)
        => prepareAsync(connection, Channel, cancellationToken).WaitOrAbandonAsync(cancellationToken);

    /// <summary>
    /// Sends the quality of service and the subscription, and sets <see cref="Tag"/>. Deliveries can arrive
    /// before it returns.
    /// </summary>
    /// <param name="maxPrefetch">The largest number of unacknowledged messages the broker delivers.</param>
    /// <param name="cancellationToken">Ends the wait for the broker.</param>
    public async Task SubscribeAsync(ushort maxPrefetch, CancellationToken cancellationToken)
    {
        await Channel
            .BasicQosAsync(prefetchSize: 0, prefetchCount: maxPrefetch, global: false, cancellationToken)
            .WaitOrAbandonAsync(cancellationToken);

        Tag = await Channel
            .BasicConsumeAsync(queueName, autoAck: false, consumer: this, cancellationToken)
            .WaitOrAbandonAsync(cancellationToken);
    }

    /// <summary>
    /// Asks the broker to stop delivering. Handlers that run settle their messages on the open channel.
    /// </summary>
    /// <param name="cancellationToken">Ends the wait for the broker.</param>
    public Task CancelAsync(CancellationToken cancellationToken)
        => Channel.BasicCancelAsync(Tag, noWait: false, cancellationToken).WaitOrAbandonAsync(cancellationToken);

    /// <summary>
    /// Stops starting handlers for deliveries of this attachment. Running handlers are not cancelled.
    /// </summary>
    public void Retire() => _isRetired = true;

    /// <summary>
    /// Stops starting handlers and cancels the handlers that run, without waiting for their callbacks.
    /// </summary>
    public void CancelHandlers()
    {
        Retire();
        _ = CancelCallbacksAsync();

        async Task CancelCallbacksAsync()
        {
            try
            {
                // The callbacks of handlers run on the thread pool, not on the caller.
                await _handlers.CancelAsync();
            }
            catch (Exception ex)
            {
                logger.HandlerCancellationFailed(ex, queueName);
            }
        }
    }

    /// <summary>
    /// Closes the channel, waiting no longer than the close timeout of the connection manager.
    /// </summary>
    public Task CloseAsync() => connections.CloseChannelAsync(Channel);

    /// <inheritdoc />
    public override Task HandleBasicDeliverAsync(
        string consumerTag,
        ulong deliveryTag,
        bool redelivered,
        string exchange,
        string routingKey,
        IReadOnlyBasicProperties properties,
        ReadOnlyMemory<byte> body,
        CancellationToken cancellationToken = default)
    {
        if (_isRetired)
        {
            // The broker delivers the message again once the channel is closed.
            return Task.CompletedTask;
        }

        // The client returns the body's buffer to its pool once this callback returns.
        var buffer = ArrayPool<byte>.Shared.Rent(body.Length);
        body.CopyTo(buffer);

        var eventArgs = new BasicDeliverEventArgs(
            consumerTag,
            deliveryTag,
            redelivered,
            exchange,
            routingKey,
            properties,
            buffer.AsMemory(0, body.Length),
            HandlerToken);

        if (!dispatcher.TryEnqueue(new RabbitMQDeliveryDispatcher.Delivery(this, eventArgs, buffer)))
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override async Task HandleBasicCancelAsync(string consumerTag, CancellationToken cancellationToken = default)
    {
        await base.HandleBasicCancelAsync(consumerTag, cancellationToken);
        OnLost(reason: null);
    }

    private void OnLost(ShutdownEventArgs? reason)
    {
        // No handler starts from here on, and the reason is there before the handler token is cancelled.
        Retire();
        _lost.TrySetResult(reason);
        CancelHandlers();
    }
}
