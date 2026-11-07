using System.Collections.Immutable;
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
        private readonly SemaphoreSlim _gate = new(1, 1);
        private readonly object _dispatchSync = new();
        private readonly CancellationToken _consumerToken;
        private readonly Task _workers;
        private TaskCompletionSource _consumerAvailable = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private ChannelConsumer? _dispatchConsumer;
        private int _cancelledHandlers;
        private int _isRecoveryBlocked;
        private ChannelConsumer? _consumer;
        private IConnection? _connection;
        private int _isReattaching;
        private bool _isDisposed;

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
        /// Gets the callback that declares the queue and its bindings on a new channel before consuming,
        /// or <c>null</c> if the consumer declares nothing.
        /// </summary>
        public Func<IChannel, CancellationToken, Task>? ProvisionAsync { get; init; }

        /// <summary>
        /// Gets resolvers for the current consumers that must be attached before this consumer starts consuming.
        /// A resolver returns <c>null</c> while its consumer is stopped; dependencies must not form cycles.
        /// </summary>
        public required ImmutableArray<Func<RegisteredConsumer?>> Dependencies { get; init; }

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
        /// Gets or sets the server-assigned consumer tag identifying this consumer on the channel.
        /// </summary>
        public string? ConsumerTag { get; set; }

        /// <summary>
        /// Gets or sets the dedicated channel used by this consumer for message delivery.
        /// </summary>
        public IChannel? Channel { get; set; }

        /// <summary>
        /// Gets a value indicating whether the consumer was stopped or disposed.
        /// </summary>
        public bool IsDisposed => _isDisposed;

        /// <summary>
        /// Gets a value indicating whether the consumer is attached to an open channel and still consuming.
        /// </summary>
        public bool IsAttached
            => ConsumerTag is not null
                && Channel is { IsOpen: true }
                && _consumer is { IsLost: false };

        /// <summary>
        /// Gets whether this consumer is attached and has capacity beyond handlers cancelled by a channel loss.
        /// </summary>
        public bool IsHealthy => IsAttached && Volatile.Read(ref _cancelledHandlers) < ConsumerDispatchConcurrency;

        /// <summary>
        /// Attaches this consumer to the given connection, or does nothing when already attached to it.
        /// </summary>
        /// <remarks>
        /// Consumption starts after the consumer's topology is declared and its dependencies are attached.
        /// </remarks>
        /// <param name="connection">The RabbitMQ connection to create a channel on.</param>
        /// <param name="cancellationToken">A token to cancel the connect operation.</param>
        public async Task ConnectAsync(IConnection connection, CancellationToken cancellationToken)
        {
            using var connectCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _consumerCts.Token);
            await _gate.WaitAsync(connectCts.Token);
            try
            {
                ObjectDisposedException.ThrowIf(_isDisposed, this);
                if (!ReferenceEquals(_connection, connection) || !IsAttached)
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
            await DetachAsync(cancellationToken);

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

                // An auto-delete queue is gone once its last consumer is, and an operator can delete any queue.
                if (ProvisionAsync is not null)
                {
                    await ProvisionAsync(channel, attachToken);
                }

                await channel.BasicQosAsync(
                    prefetchSize: 0,
                    prefetchCount: PrefetchCount,
                    global: false,
                    cancellationToken: attachToken);

                foreach (var resolveDependency in Dependencies)
                {
                    var attempt = 0;
                    while (!attachToken.IsCancellationRequested)
                    {
                        if (resolveDependency() is { IsDisposed: false } dependency)
                        {
                            try
                            {
                                await dependency.ConnectAsync(connection, attachToken);
                                if (dependency.IsAttached && ReferenceEquals(dependency, resolveDependency()))
                                {
                                    break;
                                }
                            }
                            catch (Exception ex)
                                when (dependency.IsDisposed
                                    && !attachToken.IsCancellationRequested
                                    && ex is ObjectDisposedException or OperationCanceledException)
                            {
                                // A dependency can stop while its attachment is in progress.
                            }
                        }

                        attempt++;
                        var delay = ExponentialBackoff.CalculateDelay(attempt, s_initialReattachDelay, s_maxReattachDelay);
                        await Task.Delay(delay, attachToken);
                    }

                    attachToken.ThrowIfCancellationRequested();
                }

                var tag = await channel.BasicConsumeAsync(
                    queue: QueueName,
                    autoAck: false,
                    consumer: consumer,
                    cancellationToken: attachToken);

                ConsumerTag = tag;
                Channel = channel;
                _consumer = consumer;
                _connection = connection;
                SetDispatchConsumer(consumer);
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
                return await connection.CreateChannelAsync(channelOptions, cancellationToken);
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
            consumer?.Dispose();
            ConsumerTag = null;
            Channel = null;

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
            // The channel is detached first, so that its shutdown is not treated as a loss.
            var channel = Channel;
            var consumerTag = ConsumerTag;
            var consumer = _consumer;
            Channel = null;
            ConsumerTag = null;
            _consumer = null;
            _connection = null;

            if (consumer is not null)
            {
                if (!consumer.IsLost && consumerTag is not null && channel is { IsOpen: true })
                {
                    try
                    {
                        await channel.BasicCancelAsync(consumerTag, cancellationToken: cancellationToken);
                    }
                    catch
                    {
                        // Ignore
                    }
                }

                ClearDispatchConsumer(consumer);
                consumer.StopAccepting();
                consumer.CancelHandlers();
                consumer.Dispose();
            }

            if (channel is not null)
            {
                await CloseChannelAsync(channel, cancellationToken);
            }
        }

        private void SetDispatchConsumer(ChannelConsumer consumer)
        {
            lock (_dispatchSync)
            {
                if (!_isDisposed && !consumer.IsLost)
                {
                    _dispatchConsumer = consumer;
                    _consumerAvailable.TrySetResult();
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
                    _consumerAvailable = new(TaskCreationOptions.RunContinuationsAsynchronously);
                }
            }
        }

        private async Task<ChannelConsumer?> WaitForConsumerAsync()
        {
            while (!_consumerToken.IsCancellationRequested)
            {
                Task available;
                lock (_dispatchSync)
                {
                    if (_dispatchConsumer is { } consumer && consumer.TryAcquire())
                    {
                        return consumer;
                    }

                    available = _consumerAvailable.Task;
                }

                await available.WaitAsync(_consumerToken);
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

        private void UpdateRecoveryState()
        {
            var blocked = Volatile.Read(ref _cancelledHandlers) >= ConsumerDispatchConcurrency ? 1 : 0;
            if (Interlocked.Exchange(ref _isRecoveryBlocked, blocked) == blocked || _isDisposed)
            {
                return;
            }

            if (blocked == 1)
            {
                Manager.Logger.ConsumerRecoveryBlocked(QueueName, ConsumerDispatchConcurrency);
            }
            else
            {
                Manager.Logger.ConsumerRecoveryResumed(QueueName);
            }
        }

        private static async Task CloseChannelAsync(IChannel channel, CancellationToken cancellationToken)
        {
            try
            {
                if (channel.IsOpen)
                {
                    await channel.CloseAsync(cancellationToken: cancellationToken);
                }
            }
            catch
            {
                // Ignore close errors.
            }
            finally
            {
                try
                {
                    await channel.DisposeAsync();
                }
                catch
                {
                    // Ignore disposal errors.
                }
            }
        }

        private void OnConsumerLost(ChannelConsumer consumer, ShutdownEventArgs? reason)
        {
            consumer.MarkLostPending(reason);

            if (_isDisposed
                || !ReferenceEquals(_consumer, consumer)
                || !consumer.TryHandlePendingLoss())
            {
                return;
            }

            HandleLoss(consumer);

            // Loss notifications are raised on the client's reader loop, which must not be blocked
            // by opening a new channel.
            _ = Task.Run(StartReattach);
        }

        private void HandleLoss(ChannelConsumer consumer)
        {
            var reason = consumer.LossReason;
            if (reason is null)
            {
                Manager.Logger.ConsumerCancelledByBroker(QueueName);
            }
            else
            {
                Manager.Logger.ConsumerChannelLost(QueueName, reason.Initiator, reason.ReplyCode, reason.ReplyText);
            }

            ClearDispatchConsumer(consumer);
            consumer.StopAccepting();
            consumer.CancelHandlers();
        }

        private void StartReattach()
        {
            if (Interlocked.CompareExchange(ref _isReattaching, 1, 0) != 0)
            {
                return;
            }

            _ = ReattachAsync();
        }

        private async Task ReattachAsync()
        {
            var attempt = 0;
            var cancellationToken = _consumerCts.Token;

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

                    await _gate.WaitAsync(cancellationToken);
                    try
                    {
                        if (IsAttached)
                        {
                            break;
                        }

                        await ConnectCoreAsync(connection, cancellationToken);

                        if (IsAttached)
                        {
                            Manager.Logger.ReattachedConsumerForQueue(QueueName);
                            break;
                        }

                        attempt++;
                        delay = ExponentialBackoff.CalculateDelay(attempt, s_initialReattachDelay, s_maxReattachDelay);
                    }
                    catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
                    {
                        attempt++;
                        delay = ExponentialBackoff.CalculateDelay(attempt, s_initialReattachDelay, s_maxReattachDelay);

                        if (attempt == 1)
                        {
                            Manager.Logger.FailedToReattachConsumerForQueue(ex, QueueName, delay);
                        }
                        else
                        {
                            Manager.Logger.FailedToReattachConsumerForQueueAgain(ex, QueueName, attempt, delay);
                        }
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
            if (!_isDisposed && !IsAttached && Manager.CurrentConnection is { IsOpen: true })
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
            Manager.RemoveConsumer(this);
            _isDisposed = true;
            await _consumerCts.CancelAsync();

            await _gate.WaitAsync(CancellationToken.None);
            try
            {
                var channel = Channel;
                var consumerTag = ConsumerTag;
                var consumer = _consumer;
                Channel = null;
                ConsumerTag = null;
                _consumer = null;
                _connection = null;

                if (consumer is not null)
                {
                    if (!consumer.IsLost && consumerTag is not null && channel is { IsOpen: true })
                    {
                        try
                        {
                            await channel.BasicCancelAsync(consumerTag, cancellationToken: cancellationToken);
                        }
                        catch
                        {
                            // Ignore
                        }
                    }

                    // Deliveries that no handler has started are left unsettled, so the broker requeues them on close.
                    ClearDispatchConsumer(consumer);
                    consumer.StopAccepting();
                }

                try
                {
                    // Running handlers finish and settle their deliveries on the open channel.
                    await _workers.WaitAsync(cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    consumer?.CancelHandlers();
                }

                consumer?.Dispose();

                if (channel is not null)
                {
                    // The stop token bounds handler draining; channel cleanup continues after cancellation.
                    await CloseChannelAsync(channel, CancellationToken.None);
                }
            }
            finally
            {
                _gate.Release();
            }

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
    }
}
