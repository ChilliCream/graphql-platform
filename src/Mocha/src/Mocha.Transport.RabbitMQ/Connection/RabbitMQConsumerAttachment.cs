using System.Buffers;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// The broker-side consumer on one channel. A lost or closed attachment is never used again.
/// </summary>
internal sealed class RabbitMQConsumerAttachment : AsyncDefaultBasicConsumer
{
    private readonly RabbitMQConnectionManager _connectionManager;
    private readonly RabbitMQDeliveryDispatcher _dispatcher;
    private readonly RabbitMQConsumerLogger _logger;
    private readonly string _queueName;
    private readonly CancellationTokenSource _handlers = new();
    private readonly TaskCompletionSource<ShutdownEventArgs?> _lost = new(
        TaskCreationOptions.RunContinuationsAsynchronously);
    private volatile bool _isRetired;

    /// <summary>
    /// Creates the attachment for an open channel and starts to watch the channel for its shutdown.
    /// </summary>
    /// <param name="channel">The open channel.</param>
    /// <param name="connectionManager">The manager that closes the channel.</param>
    /// <param name="dispatcher">Receives the deliveries of the attachment.</param>
    /// <param name="logger">The logger for failures of handler cancellation.</param>
    /// <param name="queueName">The name of the queue the attachment consumes.</param>
    public RabbitMQConsumerAttachment(
        IChannel channel,
        RabbitMQConnectionManager connectionManager,
        RabbitMQDeliveryDispatcher dispatcher,
        RabbitMQConsumerLogger logger,
        string queueName) : base(channel)
    {
        _connectionManager = connectionManager;
        _dispatcher = dispatcher;
        _logger = logger;
        _queueName = queueName;

        channel.ChannelShutdownAsync += (_, reason) =>
        {
            OnLost(reason);
            return Task.CompletedTask;
        };
    }

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
            .BasicConsumeAsync(_queueName, autoAck: false, consumer: this, cancellationToken)
            .WaitOrAbandonAsync(cancellationToken);
    }

    /// <summary>
    /// Stops consuming and waits for running handlers until <paramref name="drainToken"/> is cancelled, then
    /// cancels the handlers that still run and closes the channel.
    /// </summary>
    /// <param name="drainToken">Ends the wait for the broker and for running handlers.</param>
    /// <returns>The number of handlers that still ran when the wait ended.</returns>
    public async Task<int> StopAsync(CancellationToken drainToken)
    {
        if (!IsLost && Channel.IsOpen)
        {
            try
            {
                // Asks the broker to stop delivering. Running handlers can still acknowledge on the open
                // channel. Deliveries that arrive during this call are still accepted.
                await Channel.BasicCancelAsync(Tag, noWait: false, drainToken).WaitOrAbandonAsync(drainToken);
            }
            catch
            {
                // The channel is closed below.
            }
        }
        else
        {
            // The client closes a channel before it raises the shutdown, so nobody else cancels the
            // handlers in that gap.
            CancelHandlers();
        }

        // Retire before the wait reads the count. A worker checks the flag and counts its handler under one
        // lock, so either the wait sees the handler or the worker skips the delivery.
        _isRetired = true;

        // The wait also covers handlers of older channels. They hold a worker until they return.
        var running = await _dispatcher.WaitForIdleAsync(drainToken);
        if (running > 0)
        {
            CancelHandlers();
        }

        await CloseAsync();
        return running;
    }

    /// <summary>
    /// Stops starting handlers and cancels the handlers that run, without waiting for their callbacks.
    /// </summary>
    public void CancelHandlers()
    {
        _isRetired = true;
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
                _logger.HandlerCancellationFailed(ex, _queueName);
            }
        }
    }

    /// <summary>
    /// Closes the channel, waiting no longer than the close timeout of the connection manager.
    /// </summary>
    public Task CloseAsync() => _connectionManager.CloseChannelAsync(Channel);

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

        if (!_dispatcher.TryEnqueue(new RabbitMQDeliveryDispatcher.Delivery(this, eventArgs, buffer)))
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
        _isRetired = true;
        _lost.TrySetResult(reason);
        CancelHandlers();
    }
}
