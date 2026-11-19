using Mocha.Threading;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Consumes one queue. A single task attaches the consumer to a channel, waits until the channel or the
/// broker-side consumer is lost, and attaches again, until the consumer is stopped.
/// </summary>
/// <param name="connectionManager">The manager of the connection the consumer attaches on.</param>
/// <param name="logger">The logger for consumer events.</param>
/// <param name="options">The timeouts and the retry delay of the consumer.</param>
/// <param name="timeProvider">The clock used for elapsed times, delays and timeouts.</param>
/// <param name="queueName">The name of the queue the consumer consumes.</param>
/// <param name="prepareAsync">Runs on every new channel before the consumer consumes, or <c>null</c>.</param>
/// <param name="messageHandler">Handles one delivered message.</param>
/// <param name="maxPrefetch">The largest number of unacknowledged messages the broker delivers.</param>
/// <param name="maxConcurrency">The largest number of messages that are handled at the same time.</param>
/// <param name="onStopped">Called on each stop of the consumer, after the stop ended.</param>
internal sealed class RabbitMQConsumer(
    RabbitMQConnectionManager connectionManager,
    RabbitMQConsumerLogger logger,
    RabbitMQConsumerManagerOptions options,
    TimeProvider timeProvider,
    string queueName,
    Func<IConnection, IChannel, CancellationToken, Task>? prepareAsync,
    Func<IChannel, BasicDeliverEventArgs, CancellationToken, ValueTask> messageHandler,
    ushort maxPrefetch,
    ushort maxConcurrency,
    Action<RabbitMQConsumer> onStopped) : IAsyncDisposable
{
    private static readonly CreateChannelOptions s_channelOptions = new(
        publisherConfirmationsEnabled: false,
        publisherConfirmationTrackingEnabled: false,
        consumerDispatchConcurrency: 1);

    private readonly RabbitMQDeliveryDispatcher _dispatcher = new(maxConcurrency, messageHandler, logger, queueName);

    /// <summary>
    /// Ends every wait of the lifecycle task.
    /// </summary>
    private readonly CancellationTokenSource _stopRequested = new();

    /// <summary>
    /// Ends the wait for running handlers during a stop.
    /// </summary>
    private readonly CancellationTokenSource _drainDeadline = new();

    private readonly TaskCompletionSource _startup = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// The lifecycle task, or <c>null</c> until <see cref="Start"/> was called.
    /// </summary>
    private volatile Task? _lifecycle;

    /// <summary>
    /// Set only while the state is <see cref="ConsumerState.Attached"/>.
    /// </summary>
    private volatile RabbitMQConsumerAttachment? _current;

    private volatile ConsumerState _state;

    /// <summary>
    /// Gets the name of the queue the consumer consumes.
    /// </summary>
    public string QueueName => queueName;

    /// <summary>
    /// Gets the largest number of unacknowledged messages the broker delivers to the consumer.
    /// </summary>
    public ushort MaxPrefetch => maxPrefetch;

    /// <summary>
    /// Gets the largest number of messages that are handled at the same time.
    /// </summary>
    public ushort MaxConcurrency => maxConcurrency;

    /// <summary>
    /// Gets whether the consumer is attached to an open channel and the broker delivers to it.
    /// </summary>
    public bool IsAttached => _state is ConsumerState.Attached && _current is { IsLost: false, Channel.IsOpen: true };

    /// <summary>
    /// Gets the number of handlers that have not returned, including cancelled handlers of a lost channel.
    /// </summary>
    public int RunningHandlers => _dispatcher.RunningCount;

    /// <summary>
    /// Gets the state of the consumer.
    /// </summary>
    public ConsumerState State => _state;

    /// <summary>
    /// Gets a task that completes after the first attempt to attach, or as soon as the consumer must wait for
    /// a connection. It faults when the first attempt fails in a way that repeats on every attempt, or when
    /// the consumer fails unexpectedly before that.
    /// </summary>
    public Task Startup => _startup.Task;

    /// <summary>
    /// Starts the workers and the lifecycle task. Call it one time, after the consumer is registered.
    /// </summary>
    public void Start()
    {
        _dispatcher.Start();
        _lifecycle = Task.Run(RunAsync);
    }

    private async Task RunAsync()
    {
        var stop = _stopRequested.Token;
        var detachedSince = timeProvider.GetTimestamp();
        var attempt = 0;
        var wasAttached = false;

        try
        {
            while (true)
            {
                _state = ConsumerState.WaitingForConnection;
                // The queue can still hold deliveries of the last channel. That channel is closed, so the broker
                // delivers them again. Without the discard, their buffers stay rented while every worker is busy.
                _dispatcher.DiscardQueued();

                var getting = connectionManager.GetConnectionAsync(stop);
                if (!getting.IsCompleted)
                {
                    // An endpoint must start during a broker outage and attach later. So the registration must not
                    // wait for a connection.
                    _startup.TrySetResult();
                }

                var connection = await getting;

                // The broker does not answer requests on a connection it blocked for a memory or disk alarm.
                // An attach there would only time out.
                await connectionManager.WaitWhileBlockedAsync(stop);

                RabbitMQConsumerAttachment attachment;
                try
                {
                    attachment = await AttachAsync(connection);
                }
                catch (Exception ex)
                {
                    // A failure that comes with a stop ends the loop as a stop, so the drain runs. It is not a
                    // failed attempt.
                    stop.ThrowIfCancellationRequested();

                    // On the first attempt, a failure that repeats on every attempt fails the registration, such as
                    // a queue that exists with other arguments. The caller disposes the consumer, so a retry has no
                    // purpose.
                    if (ex.IsPermanentFailure() && _startup.TrySetException(ex))
                    {
                        return;
                    }

                    attempt++;
                    var retryDelay = ExponentialBackoff.CalculateDelay(
                        attempt,
                        options.InitialRetryDelay,
                        options.MaxRetryDelay);

                    // The registration returns this consumer, which keeps trying in the background.
                    _startup.TrySetResult();
                    _state = ConsumerState.WaitingForConnection;
                    logger.ConsumerAttachFailed(this, ex, attempt, detachedSince, retryDelay);

                    await Task.Delay(retryDelay, timeProvider, stop);
                    continue;
                }

                var attachedAt = timeProvider.GetTimestamp();
                wasAttached = true;
                _current = attachment;
                _state = ConsumerState.Attached;
                _startup.TrySetResult();
                logger.ConsumerAttached(this, attachment, detachedSince);

                // Completes when the channel shuts down or the broker cancels the consumer, for example when its
                // queue is deleted.
                var reason = await attachment.Lost.WaitAsync(stop);

                detachedSince = timeProvider.GetTimestamp();
                _current = null;
                _state = ConsumerState.WaitingForConnection;

                // An attachment that did not last counts as a failed attempt, so a consumer that the broker
                // keeps cancelling backs off.
                attempt =
                    timeProvider.GetElapsedTime(attachedAt, detachedSince) < options.InitialRetryDelay
                        ? attempt + 1
                        : 0;
                var delay =
                    attempt > 0
                        ? ExponentialBackoff.CalculateDelay(attempt, options.InitialRetryDelay, options.MaxRetryDelay)
                        : TimeSpan.Zero;
                logger.ConsumerLost(this, reason, isConnectionLost: !connection.IsOpen, delay);

                // Closing the channel returns its unacknowledged messages to the queue.
                await attachment.CloseAsync();
                await Task.Delay(delay, timeProvider, stop);
            }
        }
        catch (OperationCanceledException) when (stop.IsCancellationRequested)
        {
            var stopStarted = timeProvider.GetTimestamp();
            var attachment = _current;

            // The health check does not report an attachment during the drain.
            _current = null;

            // A consumer that is not attached still waits for the handlers of its lost channels.
            var runningHandlers = attachment is null
                ? await _dispatcher.WaitForIdleAsync(_drainDeadline.Token)
                : await attachment.StopAsync(_drainDeadline.Token);

            // A consumer that never attached writes no "stopped" entry.
            if (wasAttached)
            {
                logger.ConsumerStopped(this, stopStarted, runningHandlers);
            }
        }
        catch (Exception ex)
        {
            _startup.TrySetException(ex);

            // A disposed connection manager is the cause of the failure, and the disposal is not an error.
            if (!connectionManager.IsDisposed)
            {
                logger.ConsumerFailed(this, ex);
            }
        }
        finally
        {
            _startup.TrySetResult();

            // An unexpected failure leaves the attachment in this field. Cancel its handlers and close it.
            var attachment = _current;
            _current = null;
            if (attachment is not null)
            {
                attachment.CancelHandlers();
                await attachment.CloseAsync();
            }

            _dispatcher.Complete();

            // Stopped is final. The consumer stays in the registry, so the health check still reports it.
            _state = ConsumerState.Stopped;
        }
    }

    /// <summary>
    /// Opens a channel, prepares it, waits for a free handler and starts consuming. The attach timeout
    /// bounds the whole attempt, without the wait for a handler.
    /// </summary>
    private async Task<RabbitMQConsumerAttachment> AttachAsync(IConnection connection)
    {
        var stop = _stopRequested.Token;
        var timeout = options.AttachTimeout;
        var started = timeProvider.GetTimestamp();
        using var attachCts = CancellationTokenSource.CreateLinkedTokenSource(stop);
        var attachToken = attachCts.Token;

        RabbitMQConsumerAttachment? attachment = null;
        try
        {
            _state = ConsumerState.Attaching;

            // An open that is not started cannot time out and replace the connection of other consumers.
            stop.ThrowIfCancellationRequested();

            // The connection manager bounds the open with the same timeout. A second timer would race it and
            // hide its error, such as a blocked connection.
            var opening = connectionManager.OpenChannelAsync(connection, s_channelOptions, timeout);
            IChannel channel;
            try
            {
                channel = await opening.WaitAsync(stop);
            }
            catch (OperationCanceledException) when (stop.IsCancellationRequested)
            {
                // The open does not end with the token. The channel is closed once it is open.
                _ = opening.CloseWhenOpenedAsync(connectionManager);
                throw;
            }

            attachment = new RabbitMQConsumerAttachment(channel, connectionManager, _dispatcher, logger, QueueName);

            attachCts.CancelAfter(GetRemaining(timeout, timeProvider.GetElapsedTime(started)));
            if (prepareAsync is not null)
            {
                await attachment.PrepareAsync(connection, prepareAsync, attachToken);
            }

            // The queue and its bindings exist from here on, also while the consumer waits for a handler.
            // That wait does not count against the timeout.
            attachCts.CancelAfter(Timeout.InfiniteTimeSpan);
            var used = timeProvider.GetElapsedTime(started);

            // Handlers of a lost channel keep running, and a new attachment needs one free handler. The wait
            // also ends when this channel is lost. For an exclusive queue, the broker deletes the queue with
            // its connection. The next attempt declares the queue again instead of waiting for a stuck handler.
            using var waitCts = CancellationTokenSource.CreateLinkedTokenSource(stop, attachment.HandlerToken);
            var capacity = _dispatcher.WaitForCapacityAsync(waitCts.Token);
            if (!capacity.IsCompletedSuccessfully)
            {
                _state = ConsumerState.WaitingForHandlers;
                try
                {
                    try
                    {
                        await capacity.WaitAsync(options.BlockedGracePeriod, timeProvider);
                    }
                    catch (TimeoutException)
                    {
                        // The handlers were cancelled when their channel was lost. Only a handler that ignores
                        // the cancellation is still running after the grace period.
                        if (!capacity.IsCompleted)
                        {
                            logger.ConsumerBlockedByHandlers(this, options.BlockedGracePeriod);
                        }
                    }

                    await capacity;
                }
                catch (OperationCanceledException) when (!stop.IsCancellationRequested)
                {
                    // The wait is cancelled when this channel is lost. A cancel that is not from stop fails the
                    // attempt.
                    throw ThrowHelper.ConsumerChannelLostWhileWaiting(QueueName, await attachment.Lost);
                }

                _state = ConsumerState.Attaching;
            }

            // Subscribing comes after the wait, because a subscription without a free handler takes deliveries
            // it cannot run. For a quorum queue, each such delivery uses up the delivery limit of the message.
            attachCts.CancelAfter(GetRemaining(timeout, used));
            await attachment.SubscribeAsync(MaxPrefetch, attachToken);
            return attachment;
        }
        catch (Exception ex)
        {
            if (attachment is not null)
            {
                // A request may still be in flight, so the channel is closed without waiting for it.
                attachment.CancelHandlers();
                _ = attachment.CloseAsync();
            }

            if (ex is not OperationCanceledException || stop.IsCancellationRequested)
            {
                throw;
            }

            // A cancel that is not from stop is a timeout. It is either the attach timer or the client's own
            // request timeout.
            throw attachCts.IsCancellationRequested
                ? ThrowHelper.ConsumerAttachTimedOut(QueueName, timeout)
                : ThrowHelper.ConsumerAttachRequestTimedOut(QueueName, ex);
        }
    }

    private static TimeSpan GetRemaining(TimeSpan timeout, TimeSpan used)
        => timeout == Timeout.InfiniteTimeSpan ? timeout
        : used < timeout ? timeout - used : TimeSpan.Zero;

    /// <summary>
    /// Stops consuming and waits for running handlers until <paramref name="drainToken"/> is cancelled, then
    /// cancels the handlers that still run and closes the channel. Handlers are cancelled at once when their
    /// channel is lost, and closing the channel does not depend on the token.
    /// </summary>
    public async Task StopAsync(CancellationToken drainToken)
    {
        // The token of each caller can end the wait for running handlers. A later caller with a shorter
        // token shortens the drain.
        await using var registration = drainToken.UnsafeRegister(
            static state => ((CancellationTokenSource)state!).Cancel(),
            _drainDeadline);

        await _stopRequested.CancelAsync();
        try
        {
            // A consumer that was not started has nothing to stop. A later start ends at once.
            if (_lifecycle is { } lifecycle)
            {
                await lifecycle;
            }
        }
        finally
        {
            // Removes the consumer from the registry of the manager. Each stop call runs this, so the removal
            // is safe to repeat.
            onStopped(this);
        }
    }

    /// <summary>
    /// Stops consuming, cancels running handlers without waiting for them, and closes the channel.
    /// </summary>
    public async ValueTask DisposeAsync() => await StopAsync(new CancellationToken(canceled: true));
}
