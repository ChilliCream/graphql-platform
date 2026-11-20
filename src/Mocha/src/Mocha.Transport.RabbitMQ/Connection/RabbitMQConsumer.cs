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
/// <param name="prepareAsync">Runs on every new channel before the consumer consumes.</param>
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
    Func<IConnection, IChannel, CancellationToken, Task> prepareAsync,
    Func<IChannel, BasicDeliverEventArgs, CancellationToken, ValueTask> messageHandler,
    ushort maxPrefetch,
    ushort maxConcurrency,
    Action<RabbitMQConsumer> onStopped) : IAsyncDisposable
{
    private readonly RabbitMQConnectionManager _connectionManager = connectionManager;
    private readonly RabbitMQConsumerLogger _logger = logger;
    private readonly RabbitMQConsumerManagerOptions _options = options;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly Func<IConnection, IChannel, CancellationToken, Task> _prepareAsync = prepareAsync;

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
    /// The phase the lifecycle task is in. Only that task replaces it.
    /// </summary>
    private volatile Phase _phase = new WaitingForConnection(timeProvider.GetTimestamp(), failures: 0, TimeSpan.Zero);

    private bool _wasAttached;

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
    public bool IsAttached
        => !_stopRequested.IsCancellationRequested && _phase.Attachment is { IsLost: false, Channel.IsOpen: true };

    /// <summary>
    /// Gets the number of handlers that have not returned, including cancelled handlers of a lost channel.
    /// </summary>
    public int RunningHandlers => _dispatcher.RunningCount;

    /// <summary>
    /// Gets the state of the consumer.
    /// </summary>
    public ConsumerState State => _phase.State;

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

        try
        {
            while (await _phase.NextAsync(this, stop) is { } next)
            {
                _phase = next;
            }
        }
        catch (OperationCanceledException) when (stop.IsCancellationRequested)
        {
            var stopStarted = _timeProvider.GetTimestamp();

            // Wait until all handlers return. This also applies when the consumer is not attached.
            // Handlers of lost channels can still run while the consumer is not attached.
            // _drainDeadline.Token limits the wait. It is cancelled when the token given to StopAsync is cancelled.
            // That cancellation does not throw. The wait ends and returns the number of handlers still running.
            // The result is 0 when all handlers return in time, and otherwise the count at cancellation.
            var runningHandlers = _phase.Attachment is { } attachment
                ? await attachment.StopAsync(_drainDeadline.Token)
                : await _dispatcher.WaitForIdleAsync(_drainDeadline.Token);

            // A consumer that never attached writes no "stopped" entry.
            if (_wasAttached)
            {
                _logger.ConsumerStopped(this, stopStarted, runningHandlers);
            }
        }
        catch (Exception ex)
        {
            _startup.TrySetException(ex);

            // An unexpected failure leaves the attachment in the phase. Cancel its handlers and close it.
            if (_phase.Attachment is { } attachment)
            {
                attachment.CancelHandlers();
                await attachment.CloseAsync();
            }

            // A disposed connection manager is the cause of the failure, and the disposal is not an error.
            if (!_connectionManager.IsDisposed)
            {
                _logger.ConsumerFailed(this, ex);
            }
        }
        finally
        {
            _startup.TrySetResult();
            _dispatcher.Complete();

            // Stopped is final. The consumer stays in the registry, so the health check still reports it.
            _phase = new Stopped();
        }
    }

    private TimeSpan GetRetryDelay(int failures)
        => ExponentialBackoff.CalculateDelay(failures, _options.InitialRetryDelay, _options.MaxRetryDelay);

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

    /// <summary>
    /// One phase of the lifecycle of the consumer. The lifecycle task runs the phase the consumer is in.
    /// </summary>
    private abstract class Phase
    {
        /// <summary>
        /// Gets the state the consumer reports while it is in the phase.
        /// </summary>
        public abstract ConsumerState State { get; }

        /// <summary>
        /// Gets the attachment the broker delivers to, or <c>null</c> when the phase has none.
        /// </summary>
        public virtual RabbitMQConsumerAttachment? Attachment => null;

        /// <summary>
        /// Does the work of the phase. Returns the phase that follows, or <c>null</c> when the consumer ends.
        /// A stop ends the phase with an <see cref="OperationCanceledException"/>.
        /// </summary>
        public abstract Task<Phase?> NextAsync(RabbitMQConsumer consumer, CancellationToken stop);
    }

    /// <summary>
    /// The consumer has no channel. It waits for the delay, and then for a connection that is ready and not
    /// blocked.
    /// </summary>
    /// <param name="detachedSince">The timestamp since which the consumer is not attached.</param>
    /// <param name="failures">The number of attempts that failed in a row.</param>
    /// <param name="delay">How long the consumer waits before it asks for a connection.</param>
    private sealed class WaitingForConnection(long detachedSince, int failures, TimeSpan delay) : Phase
    {
        public override ConsumerState State => ConsumerState.WaitingForConnection;

        public override async Task<Phase?> NextAsync(RabbitMQConsumer consumer, CancellationToken stop)
        {
            await Task.Delay(delay, consumer._timeProvider, stop);

            // The queue can still hold deliveries of the last channel. That channel is closed, so the broker
            // delivers them again. Without the discard, their buffers stay rented while every worker is busy.
            consumer._dispatcher.DiscardQueued();

            var getting = consumer._connectionManager.GetConnectionAsync(stop);
            if (!getting.IsCompleted)
            {
                // An endpoint must start during a broker outage and attach later. So the registration must not
                // wait for a connection.
                consumer._startup.TrySetResult();
            }

            var connection = await getting;

            // The broker does not answer requests on a connection it blocked for a memory or disk alarm.
            // An attach there would only time out.
            await consumer._connectionManager.WaitWhileBlockedAsync(stop);

            // Handlers of a lost channel keep running, and a new attachment needs one free handler. No handler
            // starts while the consumer is not attached, so a free handler stays free until the consumer
            // subscribes.
            var capacity = consumer._dispatcher.WaitForCapacityAsync(stop);
            return capacity.IsCompletedSuccessfully
                ? new Attaching(connection, detachedSince, failures)
                : new WaitingForHandlers(capacity, detachedSince, failures);
        }
    }

    /// <summary>
    /// Every handler still runs for a channel that is lost, and the consumer waits until one of them returns.
    /// </summary>
    /// <param name="capacity">Completes when a handler is free.</param>
    /// <param name="detachedSince">The timestamp since which the consumer is not attached.</param>
    /// <param name="failures">The number of attempts that failed in a row.</param>
    private sealed class WaitingForHandlers(Task capacity, long detachedSince, int failures) : Phase
    {
        public override ConsumerState State => ConsumerState.WaitingForHandlers;

        public override async Task<Phase?> NextAsync(RabbitMQConsumer consumer, CancellationToken stop)
        {
            var gracePeriod = consumer._options.BlockedGracePeriod;
            try
            {
                await capacity.WaitAsync(gracePeriod, consumer._timeProvider);
            }
            catch (TimeoutException)
            {
                // The handlers were cancelled when their channel was lost. Only a handler that ignores the
                // cancellation is still running after the grace period.
                if (!capacity.IsCompleted)
                {
                    consumer._logger.ConsumerBlockedByHandlers(consumer, gracePeriod);
                }
            }

            await capacity;

            // The connection can change or become blocked during the wait. So the consumer asks for it again.
            return new WaitingForConnection(detachedSince, failures, TimeSpan.Zero);
        }
    }

    /// <summary>
    /// The consumer opens a channel, prepares it and starts consuming. A failure closes the channel, and the
    /// consumer backs off before the next attempt.
    /// </summary>
    /// <param name="connection">The connection to attach on.</param>
    /// <param name="detachedSince">The timestamp since which the consumer is not attached.</param>
    /// <param name="failures">The number of attempts that failed in a row before this one.</param>
    private sealed class Attaching(IConnection connection, long detachedSince, int failures) : Phase
    {
        public override ConsumerState State => ConsumerState.Attaching;

        public override async Task<Phase?> NextAsync(RabbitMQConsumer consumer, CancellationToken stop)
        {
            var timeout = consumer._options.AttachTimeout;
            using var attachCts = CancellationTokenSource.CreateLinkedTokenSource(stop);

            RabbitMQConsumerAttachment? attachment = null;
            try
            {
                var channel = await consumer._connectionManager.OpenChannelAsync(
                    connection,
                    s_channelOptions,
                    timeout,
                    stop);

                attachment = new RabbitMQConsumerAttachment(
                    channel,
                    consumer._connectionManager,
                    consumer._dispatcher,
                    consumer._logger,
                    consumer.QueueName);

                // The client limits the wait for each answer of the broker, but that is not sufficient. When its
                // automatic recovery is on, the client records the consumer after the broker confirmed it. For
                // that it waits for a lock that a running recovery holds, and that wait has no limit and no
                // token. This time limit is the only way out. It starts after the open, which has its own limit.
                attachCts.CancelAfter(timeout);
                await attachment.PrepareAsync(connection, consumer._prepareAsync, attachCts.Token);

                await attachment.SubscribeAsync(consumer.MaxPrefetch, attachCts.Token);

                return new Attached(attachment, connection, detachedSince, failures);
            }
            catch (Exception ex)
            {
                if (attachment is not null)
                {
                    // A request may still be in flight, so the channel is closed without waiting for it.
                    attachment.CancelHandlers();
                    _ = attachment.CloseAsync();
                }

                // A failure that comes with a stop ends the consumer as a stop, so the drain runs. It is not a
                // failed attempt.
                stop.ThrowIfCancellationRequested();

                // A cancel that is not from stop is a timeout. It is either the attach timer or the client's
                // own request timeout.
                var failure =
                    ex is not OperationCanceledException ? ex
                    : attachCts.IsCancellationRequested
                        ? ThrowHelper.ConsumerAttachTimedOut(consumer.QueueName, timeout)
                    : ThrowHelper.ConsumerAttachRequestTimedOut(consumer.QueueName, ex);

                // On the first attempt, a failure that repeats on every attempt fails the registration, such as
                // a queue that exists with other arguments. The caller disposes the consumer, so a retry has no
                // purpose.
                if (failure.IsPermanentFailure() && consumer._startup.TrySetException(failure))
                {
                    return null;
                }

                var retryDelay = consumer.GetRetryDelay(failures + 1);

                // The registration returns this consumer, which keeps trying in the background.
                consumer._startup.TrySetResult();
                consumer._logger.ConsumerAttachFailed(consumer, failure, failures + 1, detachedSince, retryDelay);

                return new WaitingForConnection(detachedSince, failures + 1, retryDelay);
            }
        }
    }

    /// <summary>
    /// The broker delivers to the consumer, until the channel or the broker-side consumer is lost.
    /// </summary>
    /// <param name="attachment">The attachment the broker delivers to.</param>
    /// <param name="connection">The connection of the attachment.</param>
    /// <param name="detachedSince">The timestamp since which the consumer was not attached.</param>
    /// <param name="failures">The number of attempts that failed in a row before this attachment.</param>
    private sealed class Attached(
        RabbitMQConsumerAttachment attachment,
        IConnection connection,
        long detachedSince,
        int failures) : Phase
    {
        public override ConsumerState State => ConsumerState.Attached;

        public override RabbitMQConsumerAttachment? Attachment => attachment;

        public override async Task<Phase?> NextAsync(RabbitMQConsumer consumer, CancellationToken stop)
        {
            var attachedAt = consumer._timeProvider.GetTimestamp();
            consumer._wasAttached = true;
            consumer._startup.TrySetResult();
            consumer._logger.ConsumerAttached(consumer, attachment, detachedSince);

            // Completes when the channel shuts down or the broker cancels the consumer, for example when its
            // queue is deleted.
            var reason = await attachment.Lost.WaitAsync(stop);

            var lostAt = consumer._timeProvider.GetTimestamp();

            // An attachment that did not last counts as a failed attempt, so a consumer that the broker
            // keeps cancelling backs off.
            var isShort =
                consumer._timeProvider.GetElapsedTime(attachedAt, lostAt) < consumer._options.InitialRetryDelay;
            var nextFailures = isShort ? failures + 1 : 0;
            var delay = isShort ? consumer.GetRetryDelay(nextFailures) : TimeSpan.Zero;
            consumer._logger.ConsumerLost(consumer, reason, isConnectionLost: !connection.IsOpen, delay);

            // Closing the channel returns its unacknowledged messages to the queue.
            await attachment.CloseAsync();

            return new WaitingForConnection(lostAt, nextFailures, delay);
        }
    }

    /// <summary>
    /// The consumer ended. This phase is final.
    /// </summary>
    private sealed class Stopped : Phase
    {
        public override ConsumerState State => ConsumerState.Stopped;

        public override Task<Phase?> NextAsync(RabbitMQConsumer consumer, CancellationToken stop)
            => Task.FromResult<Phase?>(null);
    }
}
