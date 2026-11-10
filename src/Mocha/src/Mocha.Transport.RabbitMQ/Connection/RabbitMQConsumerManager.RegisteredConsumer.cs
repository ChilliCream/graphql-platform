using Mocha.Threading;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace Mocha.Transport.RabbitMQ;

public sealed partial class RabbitMQConsumerManager
{
    /// <summary>
    /// Represents a consumer registration that maintains a dedicated channel to a specific queue,
    /// supporting reconnection and graceful disposal.
    /// </summary>
    internal sealed partial class RegisteredConsumer : IAsyncDisposable
    {
        private static readonly CancellationToken s_abandonHandlers = new(canceled: true);
        private static readonly TimeSpan s_initialReattachDelay = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan s_maxReattachDelay = TimeSpan.FromSeconds(30);

        private readonly CancellationTokenSource _consumerCts = new();
        private readonly CancellationToken _consumerToken;
        private readonly CancellationTokenSource _stopCts = new();
        private readonly SemaphoreSlim _gate = new(1, 1);
        private readonly object _dispatchSync = new();
        private readonly object _stopSync = new();
        private readonly Task _workers;

        private volatile Attachment? _attachment;
        private ChannelConsumer? _dispatchConsumer;

        // Completes when the next consumer starts dispatching, and is replaced by a pending source at that moment.
        private TaskCompletionSource _consumerAttached = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private bool _isRecoveryBlocked;
        private int _cancelledHandlers;
        private int _runningHandlers;
        private int _isReattaching;
        private Task? _stopTask;
        private bool _stopCompleted;
        private volatile bool _isDisposed;

        public RegisteredConsumer(ushort consumerDispatchConcurrency)
        {
            ConsumerDispatchConcurrency = consumerDispatchConcurrency;
            _consumerToken = _consumerCts.Token;
            var workers = new Task[consumerDispatchConcurrency];
            for (var i = 0; i < workers.Length; i++)
            {
                workers[i] = Task.Run(ProcessDeliveriesAsync);
            }

            _workers = Task.WhenAll(workers);
        }

        /// <summary>
        /// Gets the owning consumer manager responsible for connection lifecycle.
        /// </summary>
        public required RabbitMQConsumerManager Manager { get; init; }

        /// <summary>
        /// Gets the name of the RabbitMQ queue this consumer is bound to.
        /// </summary>
        public required string QueueName { get; init; }

        /// <summary>
        /// Gets the callback that runs on every new channel before the consumer starts consuming on it,
        /// or <c>null</c> if the consumer starts consuming at once.
        /// </summary>
        public Func<IConnection, IChannel, CancellationToken, Task>? PrepareAsync { get; init; }

        /// <summary>
        /// Gets the callback invoked for each message delivered from the queue.
        /// </summary>
        public required Func<
            IChannel,
            BasicDeliverEventArgs,
            CancellationToken,
            ValueTask
        > MessageHandler
        { get; init; }

        /// <summary>
        /// Gets the maximum number of unacknowledged messages the broker delivers to this consumer.
        /// </summary>
        public required ushort PrefetchCount { get; init; }

        /// <summary>
        /// Gets the maximum number of messages dispatched concurrently to the consumer callback.
        /// </summary>
        public ushort ConsumerDispatchConcurrency { get; }

        /// <summary>
        /// Gets the server-assigned consumer tag, or <c>null</c> while the consumer is detached.
        /// </summary>
        public string? ConsumerTag => _attachment?.ConsumerTag;

        /// <summary>
        /// Gets the dedicated channel the consumer receives deliveries on, or <c>null</c> while it is detached.
        /// </summary>
        public IChannel? Channel => _attachment?.Channel;

        /// <summary>
        /// Gets a value indicating whether the consumer was stopped or disposed.
        /// </summary>
        public bool IsDisposed => _isDisposed;

        /// <summary>
        /// Gets a value indicating whether the consumer is attached to an open channel and still consuming.
        /// </summary>
        public bool IsAttached => _attachment is { IsActive: true };

        /// <summary>
        /// Gets whether this consumer is attached and has capacity beyond handlers cancelled by a channel loss.
        /// </summary>
        public bool IsHealthy => HealthFailureReason is null;

        /// <summary>
        /// Gets the reason the consumer is unhealthy, or <c>null</c> when it is healthy.
        /// </summary>
        internal string? HealthFailureReason
            => _isDisposed ? "The consumer is stopped."
                : !HasCapacity ? "All handler slots are occupied by cancelled handlers."
                : !IsAttached ? "The consumer is not attached."
                : null;

        private bool HasCapacity => Volatile.Read(ref _cancelledHandlers) < ConsumerDispatchConcurrency;

        /// <summary>
        /// Waits until the consumer is healthy, and is cancelled when the consumer stops.
        /// </summary>
        internal async ValueTask WaitUntilHealthyAsync(CancellationToken cancellationToken)
        {
            if (IsHealthy)
            {
                return;
            }

            using var waitCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _consumerToken);
            while (true)
            {
                waitCts.Token.ThrowIfCancellationRequested();
                Task attached;
                lock (_dispatchSync)
                {
                    if (IsHealthy)
                    {
                        return;
                    }

                    attached = _consumerAttached.Task;
                }

                await attached.WaitAsync(waitCts.Token);
            }
        }

        /// <summary>
        /// Attaches this consumer to the given connection, or does nothing when already attached to it.
        /// </summary>
        /// <param name="connection">The RabbitMQ connection to create a channel on.</param>
        /// <param name="cancellationToken">A token to cancel the connect operation.</param>
        public async Task ConnectAsync(IConnection connection, CancellationToken cancellationToken)
        {
            using var connectCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _consumerToken);
            await _gate.WaitAsync(connectCts.Token);
            try
            {
                ObjectDisposedException.ThrowIf(_isDisposed, this);
                if (_attachment is not { IsActive: true } attachment
                    || !ReferenceEquals(attachment.Connection, connection))
                {
                    await ConnectCoreAsync(connection, connectCts.Token);
                }
            }
            finally
            {
                _gate.Release();
            }

            // A loss that arrived while the consumer started is recovered by the re-attach loop.
            EnsureAttached();
        }

        /// <summary>
        /// Starts re-attaching the consumer in the background if it is not attached to an open channel.
        /// </summary>
        public void EnsureAttached()
        {
            if (!_isDisposed && !IsAttached)
            {
                StartReattach();
            }
        }

        private async Task ConnectCoreAsync(IConnection connection, CancellationToken cancellationToken)
        {
            using var _ = Manager.BeginLogScope(connection);
            await DetachAsync(cancellationToken);

            if (!HasCapacity)
            {
                return;
            }

            var attachTimeout = Manager.AttachTimeout;
            using var attachCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            attachCts.CancelAfter(attachTimeout);
            var attachToken = attachCts.Token;

            IChannel? channel = null;
            ChannelConsumer? consumer = null;
            try
            {
                channel = await CreateChannelAsync(connection, attachToken);
                consumer = new ChannelConsumer(this, channel);
                channel.ChannelShutdownAsync += consumer.OnChannelShutdownAsync;

                if (PrepareAsync is not null)
                {
                    await PrepareAsync(connection, channel, attachToken);
                }

                await channel.BasicQosAsync(
                    prefetchSize: 0,
                    prefetchCount: PrefetchCount,
                    global: false,
                    cancellationToken: attachToken);

                var tag = await channel.BasicConsumeAsync(
                    queue: QueueName,
                    autoAck: false,
                    consumer: consumer,
                    cancellationToken: attachToken);

                _attachment = new Attachment(connection, channel, consumer, tag);
                SetDispatchConsumer(consumer);
                using (Manager.BeginLogScope(channel))
                {
                    Manager.Logger.ConsumerAttached(QueueName, tag, PrefetchCount, ConsumerDispatchConcurrency);
                }
            }
            catch (OperationCanceledException)
                when (attachToken.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                await DiscardAttemptAsync(channel, consumer);
                throw ThrowHelper.ConsumerAttachTimedOut(QueueName, attachTimeout);
            }
            catch
            {
                await DiscardAttemptAsync(channel, consumer);
                throw;
            }

            // The broker can take the channel or the consumer away before the consumer was recorded above.
            if (consumer.TryHandlePendingLoss())
            {
                HandleLoss(consumer);
            }
        }

        private async Task<IChannel> CreateChannelAsync(IConnection connection, CancellationToken cancellationToken)
        {
            try
            {
                var channelOptions = new CreateChannelOptions(
                    publisherConfirmationsEnabled: false,
                    publisherConfirmationTrackingEnabled: false,
                    consumerDispatchConcurrency: 1);
                var channel = await connection.CreateChannelAsync(channelOptions, cancellationToken);
                Manager.TrackChannel(connection, channel);
                return channel;
            }
            catch (Exception ex) when (ex is not ChannelAllocationException)
            {
                // Closing the connection releases channels from incomplete open attempts.
                try
                {
                    await connection.AbortAsync();
                }
                catch
                {
                    // The connection is already closing.
                }

                Manager.StartReconnecting();
                throw;
            }
        }

        private async Task DiscardAttemptAsync(IChannel? channel, ChannelConsumer? consumer)
        {
            _attachment = null;
            consumer?.Dispose();

            if (channel is not null)
            {
                try
                {
                    await channel.DisposeAsync();
                }
                catch
                {
                    // Ignore
                }
            }
        }

        private async Task DetachAsync(CancellationToken cancellationToken)
        {
            // Once the attachment is taken, the shutdown of its channel is no longer reported as a loss.
            if (Interlocked.Exchange(ref _attachment, null) is not { } attachment)
            {
                return;
            }

            await StopConsumingAsync(attachment, cancellationToken);
            attachment.Consumer.CancelHandlers();
            await CloseAsync(attachment, cancellationToken);
        }

        /// <summary>
        /// Cancels the broker-side consumer and discards deliveries whose handlers have not started, which the
        /// broker requeues when the channel closes. Handlers that already run continue on the open channel.
        /// </summary>
        private async Task StopConsumingAsync(Attachment attachment, CancellationToken cancellationToken)
        {
            if (attachment.IsActive)
            {
                try
                {
                    await attachment.Channel.BasicCancelAsync(
                        attachment.ConsumerTag,
                        cancellationToken: cancellationToken);
                }
                catch
                {
                    // Ignore
                }
            }

            ClearDispatchConsumer(attachment.Consumer);
            attachment.Consumer.StopAccepting();
        }

        private static async Task CloseAsync(Attachment attachment, CancellationToken cancellationToken)
        {
            attachment.Consumer.Dispose();
            await CloseChannelAsync(attachment.Channel, cancellationToken);
        }

        private void SetDispatchConsumer(ChannelConsumer consumer)
        {
            lock (_dispatchSync)
            {
                if (!_isDisposed && !consumer.IsLost)
                {
                    _dispatchConsumer = consumer;

                    // Wakes the workers, and the callers that wait for the consumer to become healthy.
                    _consumerAttached.TrySetResult();
                    _consumerAttached = new(TaskCreationOptions.RunContinuationsAsynchronously);
                }
            }
        }

        private void ClearDispatchConsumer(ChannelConsumer consumer)
        {
            lock (_dispatchSync)
            {
                if (ReferenceEquals(_dispatchConsumer, consumer))
                {
                    _dispatchConsumer = null;
                }
            }
        }

        private async Task<ChannelConsumer?> WaitForConsumerAsync()
        {
            while (!_consumerToken.IsCancellationRequested)
            {
                Task attached;
                lock (_dispatchSync)
                {
                    if (_dispatchConsumer is { } consumer && consumer.TryAcquire())
                    {
                        return consumer;
                    }

                    attached = _consumerAttached.Task;
                }

                await attached.WaitAsync(_consumerToken);
            }

            return null;
        }

        private async Task ProcessDeliveriesAsync()
        {
            try
            {
                while (await WaitForConsumerAsync() is { } consumer)
                {
                    try
                    {
                        await consumer.ProcessDeliveriesAsync(_consumerToken);
                    }
                    finally
                    {
                        consumer.Release();
                    }
                }
            }
            catch (OperationCanceledException) when (_consumerToken.IsCancellationRequested)
            {
                // The registration was stopped.
            }
        }

        // The handler counters are updated by channel consumers while they hold their handler lock.
        private void OnHandlerStarted() => Interlocked.Increment(ref _runningHandlers);

        private void OnHandlerFinished(bool wasCancelled)
        {
            Interlocked.Decrement(ref _runningHandlers);
            if (wasCancelled)
            {
                Interlocked.Decrement(ref _cancelledHandlers);
            }
        }

        private void OnHandlersCancelled(int count) => Interlocked.Add(ref _cancelledHandlers, count);

        private void UpdateRecoveryState()
        {
            bool isBlocked;
            lock (_dispatchSync)
            {
                isBlocked = !HasCapacity;
                if (_isRecoveryBlocked == isBlocked || _isDisposed)
                {
                    return;
                }

                _isRecoveryBlocked = isBlocked;
            }

            using var logScope = Manager.BeginLogScope(Channel);
            if (isBlocked)
            {
                Manager.Logger.ConsumerRecoveryBlocked(Manager.ConnectionName, QueueName, ConsumerDispatchConcurrency);
            }
            else
            {
                Manager.Logger.ConsumerRecoveryResumed(Manager.ConnectionName, QueueName);
                _ = Task.Run(EnsureAttached);
            }
        }

        /// <summary>
        /// Called by a channel consumer once its channel or its broker-side consumer is lost.
        /// </summary>
        private void OnConsumerLost(ChannelConsumer consumer)
        {
            if (_isDisposed
                || !ReferenceEquals(_attachment?.Consumer, consumer)
                || !consumer.TryHandlePendingLoss())
            {
                return;
            }

            HandleLoss(consumer);

            // Loss notifications are raised on the client's reader loop.
            _ = Task.Run(StartReattach);
        }

        private void HandleLoss(ChannelConsumer consumer)
        {
            using var _ = Manager.BeginLogScope(consumer.Channel);
            var reason = consumer.LossReason;
            if (reason is null)
            {
                Manager.Logger.ConsumerCancelledByBroker(Manager.ConnectionName, QueueName);
            }
            else
            {
                Manager.Logger.ConsumerChannelLost(
                    Manager.ConnectionName,
                    QueueName,
                    reason.Initiator,
                    reason.ReplyCode,
                    reason.ReplyText);
            }

            ClearDispatchConsumer(consumer);
        }

        private void StartReattach()
        {
            if (_isDisposed || Interlocked.CompareExchange(ref _isReattaching, 1, 0) != 0)
            {
                return;
            }

            _ = ReattachAsync();
        }

        private async Task ReattachAsync()
        {
            var timeProvider = Manager.TimeProvider;
            var started = timeProvider.GetTimestamp();
            var attempt = 0;
            var cancellationToken = _consumerToken;

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    // A lost connection is recovered by the manager, which re-attaches all consumers.
                    var connection = Manager.CurrentConnection;
                    if (connection is not { IsOpen: true })
                    {
                        break;
                    }

                    TimeSpan delay;

                    using var _ = Manager.BeginLogScope(connection);
                    await _gate.WaitAsync(cancellationToken);
                    try
                    {
                        if (IsAttached)
                        {
                            break;
                        }

                        await ConnectCoreAsync(connection, cancellationToken);

                        if (!HasCapacity)
                        {
                            break;
                        }

                        if (IsAttached)
                        {
                            Manager.Logger.ReattachedConsumerForQueue(
                                Manager.ConnectionName,
                                QueueName,
                                timeProvider.GetElapsedTime(started));
                            break;
                        }

                        attempt++;
                        delay = ExponentialBackoff.CalculateDelay(attempt, s_initialReattachDelay, s_maxReattachDelay);
                    }
                    catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
                    {
                        attempt++;
                        delay = ExponentialBackoff.CalculateDelay(attempt, s_initialReattachDelay, s_maxReattachDelay);
                        Manager.Logger.FailedToReattachConsumerForQueue(
                            ex,
                            RabbitMQFailure.GetLogLevel(ex),
                            Manager.ConnectionName,
                            QueueName,
                            attempt,
                            delay,
                            timeProvider.GetElapsedTime(started));
                    }
                    finally
                    {
                        _gate.Release();
                    }

                    await Task.Delay(delay, cancellationToken);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // The consumer was disposed.
            }
            catch (ObjectDisposedException) when (_isDisposed)
            {
                // The consumer was disposed.
            }
            finally
            {
                Volatile.Write(ref _isReattaching, 0);
            }

            // A loss that arrived while this loop was finishing did not start a new one.
            if (!_isDisposed && HasCapacity && !IsAttached && Manager.CurrentConnection is { IsOpen: true })
            {
                StartReattach();
            }
        }

        /// <summary>
        /// Stops consuming and waits for running handlers to finish until <paramref name="cancellationToken"/> is
        /// cancelled, then cancels the handlers that still run, closes the channel and unregisters the consumer.
        /// </summary>
        /// <param name="cancellationToken">
        /// A token that ends the wait for running handlers and cancels handlers still running afterwards.
        /// Channel cleanup continues independently of this token, and the broker requeues unsettled deliveries
        /// when the channel closes.
        /// </param>
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await using var cancellation = cancellationToken.UnsafeRegister(
                static state => ((RegisteredConsumer)state!).CancelStop(),
                this);
            Task stopTask;
            lock (_stopSync)
            {
                stopTask = _stopTask ??= StopCoreAsync(_stopCts.Token);
            }

            await stopTask;
        }

        private void CancelStop()
        {
            lock (_stopSync)
            {
                if (!_stopCompleted)
                {
                    _stopCts.Cancel();
                }
            }
        }

        private async Task StopCoreAsync(CancellationToken cancellationToken)
        {
            using var logScope = Manager.BeginLogScope(Channel);
            var started = Manager.TimeProvider.GetTimestamp();
            Manager.Logger.ConsumerDrainStarted(QueueName, Volatile.Read(ref _runningHandlers));
            Manager.RemoveConsumer(this);
            _isDisposed = true;
            await _consumerCts.CancelAsync();

            await _gate.WaitAsync(CancellationToken.None);
            try
            {
                var attachment = Interlocked.Exchange(ref _attachment, null);
                if (attachment is not null)
                {
                    await StopConsumingAsync(attachment, cancellationToken);
                }

                try
                {
                    // Running handlers finish and settle their deliveries on the open channel.
                    await _workers.WaitAsync(cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    Manager.Logger.ConsumerDrainCancelled(
                        QueueName,
                        Volatile.Read(ref _runningHandlers),
                        Manager.TimeProvider.GetElapsedTime(started));
                    attachment?.Consumer.CancelHandlers();
                }

                if (attachment is not null)
                {
                    // The stop token bounds handler draining; channel cleanup continues after cancellation.
                    await CloseAsync(attachment, CancellationToken.None);
                }
            }
            finally
            {
                _gate.Release();
                lock (_stopSync)
                {
                    _stopCompleted = true;
                    _stopCts.Dispose();
                }
            }

            Manager.Logger.ConsumerStopped(
                Manager.ConnectionName,
                QueueName,
                Volatile.Read(ref _runningHandlers),
                Manager.TimeProvider.GetElapsedTime(started));

            _ = _workers.ContinueWith(
                static (_, state) => ((CancellationTokenSource)state!).Dispose(),
                _consumerCts,
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }

        /// <summary>
        /// Stops consuming, cancels running handlers without waiting for them, closes the channel and unregisters
        /// the consumer.
        /// </summary>
        public async ValueTask DisposeAsync() => await StopAsync(s_abandonHandlers);

        /// <summary>
        /// The channel and broker-side consumer of one attach, which are replaced together.
        /// </summary>
        private sealed record Attachment(
            IConnection Connection,
            IChannel Channel,
            ChannelConsumer Consumer,
            string ConsumerTag)
        {
            /// <summary>
            /// Gets whether the channel is open and the broker has not taken the consumer away.
            /// </summary>
            public bool IsActive => Channel.IsOpen && !Consumer.IsLost;
        }
    }
}
