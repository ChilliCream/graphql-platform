using System.Buffers;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Mocha.Threading;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Consumes one queue. A single task attaches the consumer to a channel, waits until the channel or the
/// broker-side consumer is lost, and attaches again, until the consumer is stopped.
/// </summary>
internal sealed class RabbitMQConsumer : IAsyncDisposable
{
    private static readonly CancellationToken s_noDrain = new(canceled: true);
    private static readonly TimeSpan s_initialRetryDelay = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan s_maxRetryDelay = TimeSpan.FromSeconds(30);

    private static readonly CreateChannelOptions s_channelOptions = new(
        publisherConfirmationsEnabled: false,
        publisherConfirmationTrackingEnabled: false,
        consumerDispatchConcurrency: 1);

    private readonly RabbitMQConsumerManager _manager;
    private readonly Func<IConnection, IChannel, CancellationToken, Task>? _prepareAsync;
    private readonly Func<IChannel, BasicDeliverEventArgs, CancellationToken, ValueTask> _messageHandler;

    // Ends the lifecycle task.
    private readonly CancellationTokenSource _stopRequested = new();

    // Ends the wait for running handlers during a stop.
    private readonly CancellationTokenSource _drainDeadline = new();

    // One queue for the lifetime of the consumer. Every delivery names the attachment it arrived on.
    private readonly Channel<Delivery> _deliveries =
        System.Threading.Channels.Channel.CreateUnbounded<Delivery>();

    // One permit per concurrent handler. A handler holds its permit until it returns, also after its
    // channel was lost.
    private readonly SemaphoreSlim _slots;

    private readonly TaskCompletionSource<Exception?> _firstAttempt =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly object _stopSync = new();
    private readonly Task _lifecycle;
    private readonly Task _workers;

    // Written by the lifecycle task, and by the stop after the lifecycle task ended.
    private volatile Attachment? _attachment;
    private volatile ConsumerState _state;

    // Completed and replaced on every change of the state.
    private TaskCompletionSource _stateChanged = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private Task? _stopTask;
    private bool _wasAttached;

    public RabbitMQConsumer(
        RabbitMQConsumerManager manager,
        string queueName,
        Func<IConnection, IChannel, CancellationToken, Task>? prepareAsync,
        Func<IChannel, BasicDeliverEventArgs, CancellationToken, ValueTask> messageHandler,
        ushort maxPrefetch,
        ushort maxConcurrency)
    {
        _manager = manager;
        _prepareAsync = prepareAsync;
        _messageHandler = messageHandler;
        QueueName = queueName;
        MaxPrefetch = maxPrefetch;
        MaxConcurrency = maxConcurrency;
        _slots = new SemaphoreSlim(maxConcurrency, maxConcurrency);

        var workers = new Task[maxConcurrency];
        for (var i = 0; i < workers.Length; i++)
        {
            workers[i] = Task.Run(WorkAsync);
        }

        _workers = Task.WhenAll(workers);
        _lifecycle = Task.Run(RunAsync);
    }

    /// <summary>
    /// Gets the name of the queue the consumer consumes.
    /// </summary>
    public string QueueName { get; }

    /// <summary>
    /// Gets the largest number of unacknowledged messages the broker delivers to the consumer.
    /// </summary>
    public ushort MaxPrefetch { get; }

    /// <summary>
    /// Gets the largest number of messages that are handled at the same time.
    /// </summary>
    public ushort MaxConcurrency { get; }

    /// <summary>
    /// Gets the consumer tag the broker assigned, or <c>null</c> while the consumer is not attached.
    /// </summary>
    public string? ConsumerTag => _attachment?.Tag;

    /// <summary>
    /// Gets the channel the consumer receives deliveries on, or <c>null</c> while it is not attached.
    /// </summary>
    public IChannel? Channel => _attachment?.Channel;

    /// <summary>
    /// Gets whether the consumer is attached to an open channel and the broker delivers to it.
    /// </summary>
    public bool IsAttached => HealthFailureReason is null;

    /// <summary>
    /// Gets the number of handlers that have not returned, including cancelled handlers of a lost channel.
    /// </summary>
    public int RunningHandlers => Math.Max(0, MaxConcurrency - _slots.CurrentCount);

    /// <summary>
    /// Gets the reason the consumer does not consume, or <c>null</c> when it is attached.
    /// </summary>
    public string? HealthFailureReason
        => _state switch
        {
            ConsumerState.Attached when _attachment is { IsLost: false, Channel.IsOpen: true } => null,
            ConsumerState.WaitingForHandlers => "The consumer waits for a handler of a lost channel to return.",
            ConsumerState.Stopped => "The consumer is stopped.",
            _ => "The consumer is not attached."
        };

    /// <summary>
    /// Gets a task that completes after the first attempt to attach: with <c>null</c> when the consumer
    /// attached or no connection was ready, and with the failure when the attempt failed.
    /// </summary>
    public Task<Exception?> FirstAttempt => _firstAttempt.Task;

    /// <summary>
    /// Waits until the consumer is attached. Fails when the consumer is stopped.
    /// </summary>
    public ValueTask WaitUntilAttachedAsync(CancellationToken cancellationToken)
    {
        return IsAttached ? ValueTask.CompletedTask : WaitAsync(cancellationToken);

        async ValueTask WaitAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                // Read before the state, so a change after the check below completes this task.
                var changed = Volatile.Read(ref _stateChanged).Task;
                if (IsAttached)
                {
                    return;
                }

                if (_state == ConsumerState.Stopped)
                {
                    throw ThrowHelper.ConsumerStopped(QueueName);
                }

                await changed.WaitAsync(cancellationToken);
            }
        }
    }

    private void SetState(ConsumerState state)
    {
        _state = state;
        Interlocked
            .Exchange(ref _stateChanged, new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously))
            .TrySetResult();
    }

    private async Task RunAsync()
    {
        var stop = _stopRequested.Token;
        var connections = _manager.Connection;
        var timeProvider = connections.TimeProvider;
        var logger = _manager.Logger;
        var detachedSince = timeProvider.GetTimestamp();
        var attempt = 0;

        try
        {
            while (true)
            {
                SetState(ConsumerState.WaitingForConnection);
                if (!connections.IsConnected)
                {
                    // A registration does not wait for an outage to end.
                    _firstAttempt.TrySetResult(null);
                }

                IConnection connection;
                Attachment attachment;
                try
                {
                    connection = await connections.GetConnectionAsync(stop);
                    SetState(ConsumerState.Attaching);
                    attachment = await AttachAsync(connection, stop);
                }
                catch (Exception ex) when (!stop.IsCancellationRequested)
                {
                    attempt++;
                    var delay = ExponentialBackoff.CalculateDelay(attempt, s_initialRetryDelay, s_maxRetryDelay);

                    // A first attempt the broker refuses fails the registration, which reports it.
                    if (!_firstAttempt.TrySetResult(ex) || !RabbitMQFailure.IsPermanent(ex))
                    {
                        using (connections.BeginLogScope())
                        {
                            logger.ConsumerAttachFailed(
                                ex,
                                RabbitMQFailure.GetLogLevel(ex),
                                QueueName,
                                attempt,
                                timeProvider.GetElapsedTime(detachedSince),
                                delay);
                        }
                    }

                    await Task.Delay(delay, timeProvider, stop);
                    continue;
                }

                _attachment = attachment;
                _wasAttached = true;
                SetState(ConsumerState.Attached);
                _firstAttempt.TrySetResult(null);
                using (connections.BeginLogScope())
                {
                    logger.ConsumerAttached(
                        attachment.Tag,
                        QueueName,
                        timeProvider.GetElapsedTime(detachedSince),
                        MaxPrefetch,
                        MaxConcurrency);
                }

                // Completes on a channel shutdown and on a cancel by the broker, also when either came first.
                var reason = await attachment.Lost.WaitAsync(stop);

                detachedSince = timeProvider.GetTimestamp();
                _attachment = null;
                SetState(ConsumerState.WaitingForConnection);
                var cancelledHandlers = RunningHandlers;
                attachment.CancelHandlers();
                LogLoss(reason, cancelledHandlers, isConnectionLost: !connection.IsOpen);
                await RabbitMQConnectionManager.CloseChannelAsync(attachment.Channel);

                // An attachment that did not last counts as a failed attempt, so a consumer that the broker
                // keeps cancelling backs off.
                attempt = timeProvider.GetElapsedTime(attachment.AttachedAt, detachedSince) < s_initialRetryDelay
                    ? attempt + 1
                    : 0;
                if (attempt > 0)
                {
                    var delay = ExponentialBackoff.CalculateDelay(attempt, s_initialRetryDelay, s_maxRetryDelay);
                    await Task.Delay(delay, timeProvider, stop);
                }
            }
        }
        catch (Exception) when (stop.IsCancellationRequested)
        {
            // The stop takes over. A registration that still waits for the first attempt ends with it.
            _firstAttempt.TrySetResult(null);
        }
        catch (Exception ex)
        {
            _firstAttempt.TrySetResult(ex);
            SetState(ConsumerState.Stopped);
            if (!connections.IsDisposed)
            {
                using (connections.BeginLogScope())
                {
                    logger.ConsumerFailed(ex, QueueName);
                }
            }
        }
    }

    /// <summary>
    /// Opens a channel, prepares it, waits for a free handler slot and starts consuming.
    /// </summary>
    private async Task<Attachment> AttachAsync(IConnection connection, CancellationToken stop)
    {
        var connections = _manager.Connection;
        var timeout = _manager.AttachTimeout;

        // The broker does not answer on a connection it blocked.
        await connections.WaitWhileBlockedAsync(stop);

        var opening = connections.OpenChannelAsync(connection, s_channelOptions, timeout);
        IChannel channel;
        try
        {
            channel = await opening.WaitAsync(stop);
        }
        catch (OperationCanceledException) when (stop.IsCancellationRequested)
        {
            // The consumer was stopped. The channel is closed once it is open.
            _ = CloseWhenOpenedAsync(opening);
            throw;
        }

        var attachment = new Attachment(this, channel);
        using var attachCts = CancellationTokenSource.CreateLinkedTokenSource(stop);
        var attachToken = attachCts.Token;
        try
        {
            channel.ChannelShutdownAsync += attachment.OnChannelShutdownAsync;

            attachCts.CancelAfter(timeout);
            if (_prepareAsync is not null)
            {
                await _prepareAsync(connection, channel, attachToken).WaitOrAbandonAsync(attachToken);
            }

            // The queue and its bindings exist from here on, also while the consumer waits for a handler.
            attachCts.CancelAfter(Timeout.InfiniteTimeSpan);
            await WaitForFreeSlotAsync(stop);

            attachCts.CancelAfter(timeout);
            await channel
                .BasicQosAsync(prefetchSize: 0, prefetchCount: MaxPrefetch, global: false, attachToken)
                .WaitOrAbandonAsync(attachToken);

            attachment.AttachedAt = connections.TimeProvider.GetTimestamp();
            attachment.Tag = await channel
                .BasicConsumeAsync(QueueName, autoAck: false, consumer: attachment, attachToken)
                .WaitOrAbandonAsync(attachToken);

            return attachment;
        }
        catch (Exception ex)
        {
            // A request may still be in flight, so the channel is closed without waiting for it.
            attachment.StopAccepting();
            _ = RabbitMQConnectionManager.CloseChannelAsync(channel);

            if (ex is not OperationCanceledException || stop.IsCancellationRequested)
            {
                throw;
            }

            // Without the timeout the client's own timeout for a request ended the attempt.
            throw attachCts.IsCancellationRequested
                ? ThrowHelper.ConsumerAttachTimedOut(QueueName, timeout)
                : ThrowHelper.ConsumerAttachRequestTimedOut(QueueName, ex);
        }
    }

    private static async Task CloseWhenOpenedAsync(Task<IChannel> opening)
    {
        try
        {
            await RabbitMQConnectionManager.CloseChannelAsync(await opening);
        }
        catch
        {
            // The channel could not be opened.
        }
    }

    /// <summary>
    /// Waits until at least one handler slot is free. Handlers of a lost channel keep their slot until they
    /// return, and a new attachment needs one free slot.
    /// </summary>
    private async ValueTask WaitForFreeSlotAsync(CancellationToken stop)
    {
        if (_slots.CurrentCount > 0)
        {
            return;
        }

        SetState(ConsumerState.WaitingForHandlers);

        // The handlers were cancelled when their channel was lost. Only a handler that ignores the
        // cancellation is still running after the grace period.
        var gracePeriod = _manager.BlockedGracePeriod;
        if (!await _slots.WaitAsync(gracePeriod, stop))
        {
            var connections = _manager.Connection;
            var blockedSince = connections.TimeProvider.GetTimestamp();
            using (connections.BeginLogScope())
            {
                _manager.Logger.ConsumerBlockedByHandlers(
                    QueueName,
                    MaxConcurrency,
                    gracePeriod);
            }

            await _slots.WaitAsync(stop);

            using (connections.BeginLogScope())
            {
                _manager.Logger.ConsumerUnblocked(
                    QueueName,
                    connections.TimeProvider.GetElapsedTime(blockedSince) + gracePeriod);
            }
        }

        _slots.Release();
        SetState(ConsumerState.Attaching);
    }

    private void LogLoss(ShutdownEventArgs? reason, int cancelledHandlers, bool isConnectionLost)
    {
        var connections = _manager.Connection;
        using (connections.BeginLogScope())
        {
            if (reason is null)
            {
                _manager.Logger.ConsumerCancelled(QueueName, cancelledHandlers);
                return;
            }

            // The loss of the connection has its own line. This one adds what happened to running handlers.
            var level = isConnectionLost && cancelledHandlers == 0 ? LogLevel.Debug : LogLevel.Warning;
            _manager.Logger.ConsumerChannelLost(
                level,
                QueueName,
                reason.ReplyCode,
                reason.ReplyText,
                reason.Initiator,
                cancelledHandlers);
        }
    }

    private async Task WorkAsync()
    {
        var reader = _deliveries.Reader;
        while (await reader.WaitToReadAsync())
        {
            while (reader.TryRead(out var delivery))
            {
                try
                {
                    // What a lost or stopping attachment left in the queue is skipped. The broker delivers
                    // it again once the channel is closed.
                    var attachment = delivery.Attachment;
                    if (!attachment.IsAccepting)
                    {
                        continue;
                    }

                    await _slots.WaitAsync();
                    try
                    {
                        await _messageHandler(attachment.Channel, delivery.EventArgs, attachment.HandlerToken);
                    }
                    catch (OperationCanceledException) when (attachment.HandlerToken.IsCancellationRequested)
                    {
                        // The channel was lost or the stop stopped waiting.
                    }
                    catch (Exception ex)
                    {
                        LogDeliveryFailed(ex, delivery.EventArgs);
                    }
                    finally
                    {
                        _slots.Release();
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(delivery.Buffer);
                }
            }
        }
    }

    private void LogDeliveryFailed(Exception exception, BasicDeliverEventArgs delivery)
    {
        var connections = _manager.Connection;
        using (connections.BeginLogScope())
        {
            _manager.Logger.DeliveryFailed(
                exception,
                delivery.BasicProperties.MessageId,
                QueueName,
                delivery.ConsumerTag,
                delivery.DeliveryTag,
                delivery.BasicProperties.CorrelationId);
        }
    }

    /// <summary>
    /// Stops consuming and waits for running handlers until <paramref name="drainToken"/> is cancelled, then
    /// cancels the handlers that still run and closes the channel. Closing the channel does not depend on
    /// the token, and the broker delivers unacknowledged messages again.
    /// </summary>
    public Task StopAsync(CancellationToken drainToken)
    {
        Task stopTask;
        lock (_stopSync)
        {
            stopTask = _stopTask ??= StopCoreAsync();
        }

        return stopTask.IsCompleted ? stopTask : WaitForStopAsync(stopTask, drainToken);
    }

    private async Task WaitForStopAsync(Task stopTask, CancellationToken drainToken)
    {
        // The token of any caller ends the wait for running handlers.
        await using var registration = drainToken.UnsafeRegister(
            static state => ((CancellationTokenSource)state!).Cancel(),
            _drainDeadline);

        await stopTask;
    }

    private async Task StopCoreAsync()
    {
        // Leaves the caller's lock.
        await Task.Yield();

        var connections = _manager.Connection;
        var logger = _manager.Logger;
        var started = connections.TimeProvider.GetTimestamp();
        var drainToken = _drainDeadline.Token;

        await _stopRequested.CancelAsync();
        await _lifecycle;

        // From here on this method owns the attachment.
        var attachment = _attachment;
        _attachment = null;

        if (RunningHandlers > 0)
        {
            using (connections.BeginLogScope())
            {
                logger.ConsumerDrainStarted(QueueName, RunningHandlers);
            }
        }

        if (attachment is not null)
        {
            if (attachment is { IsLost: false, Channel.IsOpen: true })
            {
                try
                {
                    // The broker stops delivering. Handlers that run settle their messages on the open channel.
                    await attachment.Channel
                        .BasicCancelAsync(attachment.Tag, noWait: false, drainToken)
                        .WaitOrAbandonAsync(drainToken);
                }
                catch
                {
                    // The channel is closed below.
                }

                attachment.StopAccepting();
            }
            else
            {
                // The channel is gone, so its handlers cannot settle their messages any more.
                attachment.CancelHandlers();
            }
        }

        // The workers finish the handlers they started, skip what is left in the queue, and end.
        _deliveries.Writer.TryComplete();
        try
        {
            await _workers.WaitAsync(drainToken);
        }
        catch (OperationCanceledException)
        {
            var runningHandlers = RunningHandlers;
            if (runningHandlers > 0)
            {
                using (connections.BeginLogScope())
                {
                    logger.ConsumerDrainTimedOut(
                        QueueName,
                        runningHandlers,
                        connections.TimeProvider.GetElapsedTime(started));
                }
            }

            attachment?.CancelHandlers();
        }

        if (attachment is not null)
        {
            await RabbitMQConnectionManager.CloseChannelAsync(attachment.Channel);
        }

        SetState(ConsumerState.Stopped);
        _manager.Remove(this);

        if (_wasAttached)
        {
            using (connections.BeginLogScope())
            {
                logger.ConsumerStopped(
                    QueueName,
                    connections.TimeProvider.GetElapsedTime(started),
                    RunningHandlers);
            }
        }
    }

    /// <summary>
    /// Stops consuming, cancels running handlers without waiting for them, and closes the channel.
    /// </summary>
    public async ValueTask DisposeAsync() => await StopAsync(s_noDrain);

    private readonly record struct Delivery(Attachment Attachment, BasicDeliverEventArgs EventArgs, byte[] Buffer);

    /// <summary>
    /// The broker-side consumer on one channel. A lost attachment is never used again.
    /// </summary>
    private sealed class Attachment : AsyncDefaultBasicConsumer
    {
        // Never disposed: it has no timer and is not linked, so it holds nothing that needs disposing.
        private readonly CancellationTokenSource _handlers = new();
        private readonly TaskCompletionSource<ShutdownEventArgs?> _lost =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly RabbitMQConsumer _owner;
        private volatile bool _isAccepting = true;

        public Attachment(RabbitMQConsumer owner, IChannel channel)
            : base(channel)
        {
            _owner = owner;
            HandlerToken = _handlers.Token;
        }

        /// <summary>
        /// Gets or sets the consumer tag the broker assigned.
        /// </summary>
        public string Tag { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the timestamp at which consuming was requested.
        /// </summary>
        public long AttachedAt { get; set; }

        /// <summary>
        /// Gets a task that completes when the channel shut down, with the reason, or when the broker
        /// cancelled the consumer, with <c>null</c>.
        /// </summary>
        public Task<ShutdownEventArgs?> Lost => _lost.Task;

        /// <summary>
        /// Gets whether the channel shut down or the broker cancelled the consumer.
        /// </summary>
        public bool IsLost => _lost.Task.IsCompleted;

        /// <summary>
        /// Gets the token that is cancelled for the handlers of this attachment.
        /// </summary>
        public CancellationToken HandlerToken { get; }

        /// <summary>
        /// Gets whether handlers may still start for deliveries of this attachment.
        /// </summary>
        public bool IsAccepting => _isAccepting;

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
            if (!_isAccepting)
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

            if (!_owner._deliveries.Writer.TryWrite(new Delivery(this, eventArgs, buffer)))
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }

            return Task.CompletedTask;
        }

        public override async Task HandleBasicCancelAsync(
            string consumerTag,
            CancellationToken cancellationToken = default)
        {
            await base.HandleBasicCancelAsync(consumerTag, cancellationToken);
            OnLost(reason: null);
        }

        public Task OnChannelShutdownAsync(object? _, ShutdownEventArgs reason)
        {
            OnLost(reason);
            return Task.CompletedTask;
        }

        private void OnLost(ShutdownEventArgs? reason)
        {
            _isAccepting = false;
            _lost.TrySetResult(reason);
        }

        /// <summary>
        /// Stops starting handlers for deliveries of this attachment.
        /// </summary>
        public void StopAccepting() => _isAccepting = false;

        /// <summary>
        /// Stops starting handlers and cancels the handlers that run.
        /// </summary>
        public void CancelHandlers()
        {
            _isAccepting = false;
            _ = CancelHandlersAsync();
        }

        private async Task CancelHandlersAsync()
        {
            try
            {
                // The callbacks of handlers run on the thread pool, not on the caller.
                await _handlers.CancelAsync();
            }
            catch (Exception ex)
            {
                var connections = _owner._manager.Connection;
                using (connections.BeginLogScope())
                {
                    _owner._manager.Logger.HandlerCancellationFailed(
                        ex,
                        _owner.QueueName);
                }
            }
        }
    }
}
