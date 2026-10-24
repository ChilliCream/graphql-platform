using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Manages RabbitMQ connections and consumers with automatic reconnection.
/// </summary>
public sealed class RabbitMQConsumerManager : RabbitMQConnectionManagerBase
{
    private ImmutableArray<RegisteredConsumer> _registeredConsumers = [];

    private CancellationTokenSource? _monitoringCts;
    private Task? _monitoringTask;

    /// <summary>
    /// Creates a new consumer manager that will use the specified connection factory to establish RabbitMQ connections.
    /// </summary>
    /// <param name="logger">The logger for connection and consumer lifecycle events.</param>
    /// <param name="connectionFactory">A factory delegate that creates new RabbitMQ connections on demand.</param>
    public RabbitMQConsumerManager(
        ILogger<RabbitMQConsumerManager> logger,
        Func<CancellationToken, ValueTask<IConnection>> connectionFactory) : base(logger, connectionFactory) { }

    /// <summary>
    /// Gets or sets how long one attempt to attach a consumer may take before it is abandoned and retried.
    /// </summary>
    internal TimeSpan AttachTimeout { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Registers a new consumer for the specified queue, creating a dedicated channel with the given prefetch count.
    /// </summary>
    /// <remarks>
    /// If a connection is already established, the consumer is connected immediately.
    /// On reconnection, all registered consumers are automatically re-attached.
    /// Disposing the returned handle unregisters the consumer and closes its channel.
    /// </remarks>
    /// <param name="queueName">The name of the RabbitMQ queue to consume from.</param>
    /// <param name="messageHandler">The callback invoked for each delivered message, receiving the channel, delivery event args, and a cancellation token.</param>
    /// <param name="prefetchCount">The maximum number of unacknowledged messages the broker will deliver to this consumer.</param>
    /// <param name="consumerDispatchConcurrency">The maximum number of messages dispatched concurrently to the consumer callback. When greater than 1, multiple messages are processed in parallel.</param>
    /// <param name="cancellationToken">A token to cancel the registration operation.</param>
    /// <returns>An <see cref="IAsyncDisposable"/> handle that, when disposed, unregisters the consumer and releases its channel.</returns>
    public async Task<IAsyncDisposable> RegisterConsumerAsync(
        string queueName,
        Func<IChannel, BasicDeliverEventArgs, CancellationToken, ValueTask> messageHandler,
        ushort prefetchCount,
        ushort consumerDispatchConcurrency,
        CancellationToken cancellationToken)
        => await RegisterConsumerAsync(
            queueName,
            provisionAsync: null,
            messageHandler,
            prefetchCount,
            consumerDispatchConcurrency,
            cancellationToken);

    /// <summary>
    /// Registers a new consumer for the specified queue, and declares the topology the consumer needs
    /// on every channel before it starts consuming.
    /// </summary>
    /// <param name="queueName">The name of the RabbitMQ queue to consume from.</param>
    /// <param name="provisionAsync">
    /// Declares the queue and its bindings on the consumer's channel before every attach, or <c>null</c>
    /// to consume without declaring anything.
    /// </param>
    /// <param name="messageHandler">The callback invoked for each delivered message.</param>
    /// <param name="prefetchCount">The maximum number of unacknowledged messages the broker delivers to this consumer.</param>
    /// <param name="consumerDispatchConcurrency">The maximum number of messages processed concurrently.</param>
    /// <param name="cancellationToken">A token to cancel the registration operation.</param>
    internal async Task<RegisteredConsumer> RegisterConsumerAsync(
        string queueName,
        Func<IChannel, CancellationToken, Task>? provisionAsync,
        Func<IChannel, BasicDeliverEventArgs, CancellationToken, ValueTask> messageHandler,
        ushort prefetchCount,
        ushort consumerDispatchConcurrency,
        CancellationToken cancellationToken)
    {
        var registration = new RegisteredConsumer
        {
            Manager = this,
            QueueName = queueName,
            ProvisionAsync = provisionAsync,
            MessageHandler = messageHandler,
            PrefetchCount = prefetchCount,
            ConsumerDispatchConcurrency = consumerDispatchConcurrency
        };

        AddConsumer(registration);

        var connection = CurrentConnection;
        if (connection is { IsOpen: true })
        {
            try
            {
                await registration.ConnectAsync(connection, cancellationToken);
            }
            catch (Exception ex)
                when (!cancellationToken.IsCancellationRequested
                    && (!connection.IsOpen || ex is TimeoutException or OperationCanceledException))
            {
                // The connection was lost or the broker did not answer in time. A new connection attaches every
                // registered consumer, and an open connection is retried by the consumer itself.
                registration.EnsureAttached();
            }
            catch
            {
                // A registration the broker refused must not be attached later by a reconnect.
                await registration.DisposeAsync();
                throw;
            }
        }

        Logger.RegisteredConsumerForQueue(queueName);

        return registration;
    }

    /// <summary>
    /// Deletes a queue on the current connection, and does nothing when there is no open connection.
    /// </summary>
    /// <param name="queueName">The name of the queue to delete.</param>
    /// <param name="cancellationToken">A token to cancel the deletion.</param>
    internal async Task DeleteQueueAsync(string queueName, CancellationToken cancellationToken)
    {
        if (CurrentConnection is not { IsOpen: true } connection || cancellationToken.IsCancellationRequested)
        {
            return;
        }

        try
        {
            await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
            await channel.QueueDeleteAsync(queueName, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.FailedToDeleteQueue(ex, queueName);
        }
    }

    /// <summary>
    /// Gets the health of every registered consumer, which is degraded while the consumer is not attached.
    /// </summary>
    internal IEnumerable<TransportHealthEntry> GetConsumerHealth()
    {
        foreach (var consumer in _registeredConsumers)
        {
            yield return consumer.GetHealth();
        }
    }

    /// <summary>
    /// Called after a new RabbitMQ connection is established, either on first connect or after a
    /// connection loss that required creating an entirely new connection.
    /// </summary>
    /// <remarks>
    /// This method re-attaches every previously registered consumer to the new connection by
    /// creating fresh channels and restarting consumption. It also starts a background health
    /// monitor that polls every 30 seconds and triggers reconnection when the connection drops.
    /// Because consumers are automatically re-attached, callers of
    /// <see cref="RegisterConsumerAsync(string, Func{IChannel, BasicDeliverEventArgs, CancellationToken, ValueTask}, ushort, ushort, CancellationToken)"/>
    /// do not need to handle reconnection themselves.
    /// </remarks>
    /// <param name="connection">The newly created <see cref="IConnection"/>.</param>
    /// <param name="cancellationToken">A token to cancel the reconnection of consumers.</param>
    /// <returns>A task that completes when all consumers have been re-attached and health monitoring has started.</returns>
    protected override async Task OnAfterConnectionCreatedAsync(
        IConnection connection,
        CancellationToken cancellationToken)
    {
        // Reconnect existing consumers
        await ReconnectConsumersAsync(connection, cancellationToken);

        // Start health monitoring if not already running
        StartHealthMonitoring();
    }

    /// <summary>
    /// Called after a connection was lost, and starts creating a new connection in the background,
    /// to which every registered consumer is re-attached.
    /// </summary>
    protected override Task OnConnectionLostAsync()
    {
        StartReconnecting();
        return Task.CompletedTask;
    }

    private async Task ReconnectConsumersAsync(IConnection connection, CancellationToken cancellationToken)
    {
        var consumers = _registeredConsumers;

        if (consumers.IsEmpty)
        {
            return;
        }

        Logger.ReconnectConsumersAsync(consumers.Length);

        // Consumers attach independently, so one that does not get an answer does not hold back the others.
        await Task.WhenAll(consumers.Select(consumer => ReconnectConsumerAsync(consumer, connection, cancellationToken)));
    }

    private async Task ReconnectConsumerAsync(
        RegisteredConsumer consumer,
        IConnection connection,
        CancellationToken cancellationToken)
    {
        try
        {
            await consumer.ConnectAsync(connection, cancellationToken);
            Logger.ReconnectConsumerForQueue(consumer.QueueName);
        }
        catch (ObjectDisposedException) when (consumer.IsDisposed)
        {
            // The consumer was stopped while the connection was established.
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            Logger.FailedToReconnectConsumerForQueue(ex, consumer.QueueName);

            // The consumer retries with backoff instead of waiting for the next health check.
            consumer.EnsureAttached();
        }
    }

    private void StartHealthMonitoring()
    {
        if (_monitoringCts is { IsCancellationRequested: false })
        {
            return;
        }

        _monitoringCts = new CancellationTokenSource();
        _monitoringTask = MonitorConnectionHealthAsync(_monitoringCts.Token);
    }

    private async Task MonitorConnectionHealthAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);

                if (!IsConnected)
                {
                    Logger.ConnectionHealthCheckFailedAttemptingReconnection();
                    StartReconnecting();
                }
                else
                {
                    Logger.ConnectionHealthCheckOk();

                    foreach (var consumer in _registeredConsumers)
                    {
                        consumer.EnsureAttached();
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Logger.ErrorDuringConnectionHealthCheck(ex);
            }
        }
    }

    private void AddConsumer(RegisteredConsumer consumer)
    {
        ImmutableInterlocked.Update(ref _registeredConsumers, consumers => consumers.Add(consumer));
    }

    private void RemoveConsumer(RegisteredConsumer consumer)
    {
        ImmutableInterlocked.Update(ref _registeredConsumers, consumers => consumers.Remove(consumer));
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        // Stop monitoring
        if (_monitoringCts is not null)
        {
            await _monitoringCts.CancelAsync();
            _monitoringCts.Dispose();
            _monitoringCts = null;
        }

        if (_monitoringTask is not null)
        {
            try
            {
                await _monitoringTask;
            }
            catch
            {
                // Ignore
            }
        }

        // Dispose all consumers
        var consumers = ImmutableInterlocked.InterlockedExchange(ref _registeredConsumers, []);

        foreach (var consumer in consumers)
        {
            try
            {
                await consumer.DisposeAsync();
            }
            catch (Exception ex)
            {
                Logger.ErrorDisposingConsumer(ex, consumer.QueueName);
            }
        }
    }

    /// <summary>
    /// Represents a consumer registration that maintains a dedicated channel to a specific queue,
    /// supporting reconnection and graceful disposal.
    /// </summary>
    internal sealed class RegisteredConsumer : IAsyncDisposable
    {
        private static readonly CancellationToken s_abandonHandlers = new(canceled: true);
        private static readonly TimeSpan s_initialReattachDelay = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan s_maxReattachDelay = TimeSpan.FromSeconds(30);

        private readonly CancellationTokenSource _consumerCts = new();
        private readonly SemaphoreSlim _gate = new(1, 1);
        private readonly object _healthLock = new();
        private readonly ConcurrentDictionary<ChannelConsumer, byte> _replacedConsumers = new();
        private ChannelConsumer? _consumer;
        private int _isReattaching;
        private bool _isDisposed;
        private DateTimeOffset? _detachedSince = TimeProvider.System.GetUtcNow();
        private string? _detachReason = "Not attached yet";
        private Exception? _lastAttachError;

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
        public required ushort ConsumerDispatchConcurrency { get; init; }

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
        /// Establishes a new channel on the given connection, configures QoS prefetch, and starts consuming messages from the queue.
        /// </summary>
        /// <remarks>
        /// Any previously active channel is disconnected before a new one is created.
        /// On failure, the partially created channel is disposed and the consumer state is reset.
        /// </remarks>
        /// <param name="connection">The RabbitMQ connection to create a channel on.</param>
        /// <param name="cancellationToken">A token to cancel the connect operation.</param>
        public async Task ConnectAsync(IConnection connection, CancellationToken cancellationToken)
        {
            await _gate.WaitAsync(cancellationToken);
            try
            {
                ObjectDisposedException.ThrowIf(_isDisposed, this);
                await ConnectCoreAsync(connection, cancellationToken);
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

        /// <summary>
        /// Gets the health of the consumer, which is degraded while it is not attached.
        /// </summary>
        public TransportHealthEntry GetHealth()
        {
            var name = $"consumer:{QueueName}";

            if (IsAttached)
            {
                return new TransportHealthEntry
                {
                    Name = name,
                    Status = TransportHealthStatus.Healthy,
                    Description = "Attached"
                };
            }

            lock (_healthLock)
            {
                return new TransportHealthEntry
                {
                    Name = name,
                    Status = TransportHealthStatus.Degraded,
                    Description = $"Not attached: {_detachReason ?? "The consumer is re-attaching"}",
                    Since = _detachedSince ?? TimeProvider.System.GetUtcNow(),
                    Exception = _lastAttachError
                };
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
                // The client's dispatcher only hands deliveries to the workers of the channel consumer.
                var channelOptions = new CreateChannelOptions(
                    publisherConfirmationsEnabled: false,
                    publisherConfirmationTrackingEnabled: false,
                    consumerDispatchConcurrency: 1);
                channel = await connection.CreateChannelAsync(channelOptions, attachToken);
                consumer = new ChannelConsumer(this, channel, ConsumerDispatchConcurrency);
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

                var tag = await channel.BasicConsumeAsync(
                    queue: QueueName,
                    autoAck: false,
                    consumer: consumer,
                    cancellationToken: attachToken);

                ConsumerTag = tag;
                Channel = channel;
                _consumer = consumer;
            }
            catch (OperationCanceledException)
                when (attachToken.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                await DiscardAttemptAsync(channel, consumer);
                var timeout = ThrowHelper.ConsumerAttachTimedOut(QueueName, attachTimeout);
                RecordDetached(timeout.Message, timeout);
                throw timeout;
            }
            catch (Exception ex)
            {
                await DiscardAttemptAsync(channel, consumer);
                RecordDetached(ex.Message, ex);
                throw;
            }

            // The broker can take the channel or the consumer away before the consumer was recorded above.
            if (consumer.TryHandlePendingLoss())
            {
                HandleLoss(consumer);
                return;
            }

            RecordAttached();
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

                // Handlers that already run keep running. Their deliveries can no longer be settled, and the broker
                // redelivers them.
                consumer.StopAccepting();
                TrackReplaced(consumer);
            }

            if (channel is not null)
            {
                await CloseChannelAsync(channel, cancellationToken);
            }
        }

        private void TrackReplaced(ChannelConsumer consumer)
        {
            _replacedConsumers.TryAdd(consumer, 0);

            consumer.Workers.ContinueWith(
                static (_, state) =>
                {
                    var (owner, replaced) = ((RegisteredConsumer, ChannelConsumer))state!;
                    owner.ForgetReplaced(replaced);
                },
                (this, consumer),
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }

        private void ForgetReplaced(ChannelConsumer consumer)
        {
            _replacedConsumers.TryRemove(consumer, out _);
            consumer.Dispose();
        }

        private static async Task CloseChannelAsync(IChannel channel, CancellationToken cancellationToken)
        {
            try
            {
                if (channel.IsOpen)
                {
                    await channel.CloseAsync(cancellationToken: cancellationToken);
                }

                await channel.DisposeAsync();
            }
            catch
            {
                // Ignore
            }
        }

        private void OnConsumerLost(ChannelConsumer consumer, ShutdownEventArgs? reason)
        {
            if (_isDisposed || !ReferenceEquals(_consumer, consumer))
            {
                // The consumer is not recorded yet or no longer recorded. A consumer that is about to be recorded
                // finds the loss when its attach completes.
                consumer.MarkLostPending(reason);
                return;
            }

            if (!consumer.TryHandleLoss(reason))
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
                RecordDetached("The broker cancelled the consumer", exception: null);
            }
            else
            {
                Manager.Logger.ConsumerChannelLost(QueueName, reason.Initiator, reason.ReplyCode, reason.ReplyText);
                RecordDetached($"The channel closed with {reason.ReplyCode} {reason.ReplyText}", reason.Exception);
            }

            // Deliveries that no handler has started are left to the broker. Running handlers finish, but their
            // deliveries can no longer be settled on the lost channel.
            consumer.StopAccepting();
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
                        return;
                    }

                    TimeSpan delay;

                    await _gate.WaitAsync(cancellationToken);
                    try
                    {
                        if (IsAttached)
                        {
                            return;
                        }

                        await ConnectCoreAsync(connection, cancellationToken);

                        if (IsAttached)
                        {
                            Manager.Logger.ReattachedConsumerForQueue(QueueName);
                            return;
                        }

                        attempt++;
                        delay = RabbitMQBackoff.GetDelay(attempt, s_initialReattachDelay, s_maxReattachDelay);
                    }
                    catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
                    {
                        attempt++;
                        delay = RabbitMQBackoff.GetDelay(attempt, s_initialReattachDelay, s_maxReattachDelay);

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

        private void RecordAttached()
        {
            lock (_healthLock)
            {
                _detachedSince = null;
                _detachReason = null;
                _lastAttachError = null;
            }
        }

        private void RecordDetached(string reason, Exception? exception)
        {
            lock (_healthLock)
            {
                _detachedSince ??= TimeProvider.System.GetUtcNow();
                _detachReason = reason;
                _lastAttachError = exception ?? _lastAttachError;
            }
        }

        /// <summary>
        /// Stops consuming and waits for running handlers to finish until <paramref name="cancellationToken"/> is
        /// cancelled, then cancels the handlers that still run, closes the channel and unregisters the consumer.
        /// </summary>
        /// <param name="cancellationToken">
        /// A token that ends the wait for running handlers. Handlers still running afterwards are cancelled and
        /// abandoned, and the broker requeues their deliveries when the channel closes.
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
                    consumer.StopAccepting();
                }

                ChannelConsumer[] running = consumer is null
                    ? [.. _replacedConsumers.Keys]
                    : [consumer, .. _replacedConsumers.Keys];

                try
                {
                    // Running handlers finish, and settle their deliveries on the open channel.
                    await Task.WhenAll(running.Select(c => c.Workers)).WaitAsync(cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    foreach (var abandoned in running)
                    {
                        await abandoned.CancelHandlersAsync();
                    }
                }

                consumer?.Dispose();

                if (channel is not null)
                {
                    await CloseChannelAsync(channel, CancellationToken.None);
                }
            }
            finally
            {
                _gate.Release();
            }

            _consumerCts.Dispose();
        }

        /// <summary>
        /// Stops consuming, cancels running handlers without waiting for them, closes the channel and unregisters
        /// the consumer.
        /// </summary>
        public async ValueTask DisposeAsync() => await StopAsync(s_abandonHandlers);

        /// <summary>
        /// The consumer for one channel, which queues deliveries for its own workers so that the client's callbacks
        /// return before handlers run, and reports when the broker takes the channel or the consumer away.
        /// </summary>
        private sealed class ChannelConsumer : AsyncDefaultBasicConsumer, IDisposable
        {
            private const int NotLost = 0;
            private const int LossPending = 1;
            private const int LossHandled = 2;

            private readonly RegisteredConsumer _owner;
            private readonly CancellationTokenSource _handlerCts = new();
            private readonly CancellationToken _handlerToken;
            private readonly Channel<Delivery> _deliveries;
            private readonly Task _workers;
            private volatile bool _isAccepting = true;
            private int _lossState;
            private ShutdownEventArgs? _lossReason;

            public ChannelConsumer(RegisteredConsumer owner, IChannel channel, int concurrency)
                : base(channel)
            {
                _owner = owner;
                _handlerToken = _handlerCts.Token;
                _deliveries = System.Threading.Channels.Channel.CreateUnbounded<Delivery>(
                    new UnboundedChannelOptions { SingleReader = concurrency == 1 });

                var workers = new Task[concurrency];
                for (var i = 0; i < workers.Length; i++)
                {
                    workers[i] = Task.Run(ProcessDeliveriesAsync);
                }

                _workers = Task.WhenAll(workers);
            }

            /// <summary>
            /// Gets a task that completes once every worker has finished its last handler.
            /// </summary>
            public Task Workers => _workers;

            public bool IsLost => Volatile.Read(ref _lossState) != NotLost;

            public ShutdownEventArgs? LossReason => Volatile.Read(ref _lossReason);

            /// <summary>
            /// Records a loss that the owner handles once it records this consumer.
            /// </summary>
            public void MarkLostPending(ShutdownEventArgs? reason)
            {
                Interlocked.CompareExchange(ref _lossReason, reason, null);
                Interlocked.CompareExchange(ref _lossState, LossPending, NotLost);
            }

            /// <summary>
            /// Records a loss, and returns whether the caller is the first to handle it.
            /// </summary>
            public bool TryHandleLoss(ShutdownEventArgs? reason)
            {
                Interlocked.CompareExchange(ref _lossReason, reason, null);
                return Interlocked.Exchange(ref _lossState, LossHandled) != LossHandled;
            }

            /// <summary>
            /// Returns whether a loss was recorded before the owner recorded this consumer, and marks it handled.
            /// </summary>
            public bool TryHandlePendingLoss()
                => Interlocked.CompareExchange(ref _lossState, LossHandled, LossPending) == LossPending;

            public async Task CancelHandlersAsync()
            {
                try
                {
                    await _handlerCts.CancelAsync();
                }
                catch (ObjectDisposedException)
                {
                    // The workers already finished.
                }
            }

            /// <summary>
            /// Rejects further deliveries and makes the workers skip the ones they have not started.
            /// </summary>
            public void StopAccepting()
            {
                _isAccepting = false;
                _deliveries.Writer.TryComplete();
            }

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
                    // The channel is lost or stopping, so the broker requeues the delivery when the channel closes.
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
                    _handlerToken);

                if (!_deliveries.Writer.TryWrite(new Delivery(eventArgs, buffer)))
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
                _owner.OnConsumerLost(this, reason: null);
            }

            public Task OnChannelShutdownAsync(object? _, ShutdownEventArgs reason)
            {
                _owner.OnConsumerLost(this, reason);
                return Task.CompletedTask;
            }

            public void Dispose()
            {
                StopAccepting();

                // Workers of a lost channel can still be running, so the token source outlives them.
                _workers.ContinueWith(
                    static (_, state) => ((CancellationTokenSource)state!).Dispose(),
                    _handlerCts,
                    CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
            }

            private async Task ProcessDeliveriesAsync()
            {
                var reader = _deliveries.Reader;

                while (await reader.WaitToReadAsync())
                {
                    while (reader.TryRead(out var delivery))
                    {
                        try
                        {
                            // A delivery queued before the channel was lost or stopped is left to the broker.
                            if (_isAccepting && !IsLost && !_handlerToken.IsCancellationRequested)
                            {
                                await _owner.MessageHandler(Channel, delivery.EventArgs, _handlerToken);
                            }
                        }
                        catch (Exception ex)
                        {
                            _owner.Manager.Logger.ErrorProcessingMessageFromQueue(ex, _owner.QueueName);
                        }
                        finally
                        {
                            ArrayPool<byte>.Shared.Return(delivery.Buffer);
                        }
                    }
                }
            }

            private readonly record struct Delivery(BasicDeliverEventArgs EventArgs, byte[] Buffer);
        }
    }
}

internal static partial class Logs
{
    [LoggerMessage(LogLevel.Information, "Creating RabbitMQ connection using factory delegate")]
    public static partial void CreatingConnectionUsingFactoryDelegate(this ILogger logger);

    [LoggerMessage(LogLevel.Information, "Successfully created connection: {ConnectionName}")]
    public static partial void SuccessfullyCreatedConnection(this ILogger logger, string? connectionName);

    [LoggerMessage(LogLevel.Error, "Failed to create connection after {Attempts} attempts")]
    public static partial void FailedToCreateConnectionAfterAttempts(this ILogger logger, Exception ex, int attempts);

    [LoggerMessage(
        LogLevel.Warning,
        "Failed to create connection (attempt {Attempt}/{MaxAttempts}). Retrying in {Delay}")]
    public static partial void FailedToCreateConnectionRetrying(
        this ILogger logger,
        Exception ex,
        int attempt,
        int maxAttempts,
        TimeSpan delay);

    [LoggerMessage(LogLevel.Error, "Error processing message from queue {Queue}")]
    public static partial void ErrorProcessingMessageFromQueue(this ILogger logger, Exception ex, string queue);

    [LoggerMessage(LogLevel.Information, "Registered consumer for queue {Queue}")]
    public static partial void RegisteredConsumerForQueue(this ILogger logger, string queue);

    [LoggerMessage(LogLevel.Warning, "Cannot provision topology - connection not available")]
    public static partial void CannotProvisionTopologyConnectionNotAvailable(this ILogger logger);

    [LoggerMessage(LogLevel.Information, "Provisioning RabbitMQ topology")]
    public static partial void ProvisioningRabbitMQTopology(this ILogger logger);

    [LoggerMessage(LogLevel.Debug, "Declared exchange: {Exchange}")]
    public static partial void DeclaredExchange(this ILogger logger, string exchange);

    [LoggerMessage(LogLevel.Debug, "Declared queue: {Queue}")]
    public static partial void DeclaredQueue(this ILogger logger, string queue);

    [LoggerMessage(LogLevel.Debug, "Created binding: {Queue} -> {Exchange}/{RoutingKey}")]
    public static partial void CreatedBinding(this ILogger logger, string queue, string exchange, string routingKey);

    [LoggerMessage(LogLevel.Information, "Successfully provisioned topology")]
    public static partial void SuccessfullyProvisionedTopology(this ILogger logger);

    [LoggerMessage(LogLevel.Error, "Failed to provision topology")]
    public static partial void FailedToProvisionTopology(this ILogger logger, Exception ex);

    [LoggerMessage(LogLevel.Information, "Reconnecting {Count} registered consumers")]
    public static partial void ReconnectConsumersAsync(this ILogger logger, int count);

    [LoggerMessage(LogLevel.Warning, "Cannot recreate consumer for queue {Queue} - no factory or handler")]
    public static partial void CannotRecreateConsumerNoFactoryOrHandler(this ILogger logger, string queue);

    [LoggerMessage(LogLevel.Information, "Reconnected consumer for queue {Queue}")]
    public static partial void ReconnectConsumerForQueue(this ILogger logger, string queue);

    [LoggerMessage(
        LogLevel.Warning,
        "Consumer for queue {Queue} lost its channel. Initiator: {Initiator}, Reply: {ReplyCode} {ReplyText}")]
    public static partial void ConsumerChannelLost(
        this ILogger logger,
        string queue,
        ShutdownInitiator initiator,
        ushort replyCode,
        string replyText);

    [LoggerMessage(LogLevel.Warning, "Consumer for queue {Queue} was cancelled by the broker")]
    public static partial void ConsumerCancelledByBroker(this ILogger logger, string queue);

    [LoggerMessage(LogLevel.Information, "Re-attached consumer for queue {Queue}")]
    public static partial void ReattachedConsumerForQueue(this ILogger logger, string queue);

    [LoggerMessage(LogLevel.Error, "Failed to re-attach consumer for queue {Queue}. Retrying in {Delay}")]
    public static partial void FailedToReattachConsumerForQueue(
        this ILogger logger,
        Exception ex,
        string queue,
        TimeSpan delay);

    [LoggerMessage(
        LogLevel.Warning,
        "Skipped {Settlement} of delivery {DeliveryTag} (routing key {RoutingKey}) because its channel is closed. The broker redelivers the message.")]
    public static partial void SettlementSkippedChannelClosed(
        this ILogger logger,
        string settlement,
        ulong deliveryTag,
        string routingKey);

    [LoggerMessage(LogLevel.Error, "Failed to reconnect consumer for queue {Queue}")]
    public static partial void FailedToReconnectConsumerForQueue(this ILogger logger, Exception ex, string queue);

    [LoggerMessage(LogLevel.Information, "Connection closed by application")]
    public static partial void ConnectionClosedByApplication(this ILogger logger);

    [LoggerMessage(LogLevel.Warning, "Connection shutdown detected. Initiator: {Initiator}, Reason: {Reason}")]
    public static partial void ConnectionShutdownDetected(
        this ILogger logger,
        ShutdownInitiator initiator,
        string reason);

    [LoggerMessage(LogLevel.Error, "Failed to reconnect after shutdown")]
    public static partial void FailedToReconnectAfterShutdown(this ILogger logger, Exception ex);

    [LoggerMessage(
        LogLevel.Warning,
        "Could not connect to the broker. Connecting in the background, consumers attach once it is reachable")]
    public static partial void BrokerUnreachableConnectingInBackground(this ILogger logger, Exception ex);

    [LoggerMessage(LogLevel.Error, "Error while handling the loss of the connection")]
    public static partial void ErrorHandlingConnectionLoss(this ILogger logger, Exception ex);

    [LoggerMessage(LogLevel.Information, "Connection recovery started")]
    public static partial void ConnectionRecoveryStarted(this ILogger logger);

    [LoggerMessage(LogLevel.Warning, "Connection blocked: {Reason}")]
    public static partial void ConnectionBlocked(this ILogger logger, string reason);

    [LoggerMessage(LogLevel.Information, "Connection unblocked")]
    public static partial void ConnectionUnblocked(this ILogger logger);

    [LoggerMessage(LogLevel.Warning, "Connection health check failed - attempting reconnection")]
    public static partial void ConnectionHealthCheckFailedAttemptingReconnection(this ILogger logger);

    [LoggerMessage(LogLevel.Error, "Reconnection attempt {Attempt} failed. Retrying in {Delay}")]
    public static partial void ReconnectionAttemptFailed(this ILogger logger, Exception ex, int attempt, TimeSpan delay);

    [LoggerMessage(LogLevel.Trace, "Connection health check: OK")]
    public static partial void ConnectionHealthCheckOk(this ILogger logger);

    [LoggerMessage(LogLevel.Error, "Error during connection health check")]
    public static partial void ErrorDuringConnectionHealthCheck(this ILogger logger, Exception ex);

    [LoggerMessage(LogLevel.Error, "Error disposing connection")]
    public static partial void ErrorDisposingConnection(this ILogger logger, Exception ex);

    [LoggerMessage(LogLevel.Information, "DelegateConnectionManager disposed")]
    public static partial void DelegateConnectionManagerDisposed(this ILogger logger);

    [LoggerMessage(LogLevel.Error, "Exception in connection callback. Detail: {Detail}")]
    public static partial void ExceptionInConnectionCallback(this ILogger logger, Exception ex, string detail);

    [LoggerMessage(LogLevel.Information, "Consumer tag changed after recovery. Old: {OldTag}, New: {NewTag}")]
    public static partial void ConsumerTagChangedAfterRecovery(this ILogger logger, string oldTag, string newTag);

    [LoggerMessage(LogLevel.Debug, "Updated consumer tag for queue {Queue}: {OldTag} -> {NewTag}")]
    public static partial void UpdatedConsumerTagForQueue(
        this ILogger logger,
        string queue,
        string oldTag,
        string newTag);

    [LoggerMessage(
        LogLevel.Warning,
        "Server-named queue changed after recovery. Old: {OldName}, New: {NewName}. This may affect consumers expecting the old queue name.")]
    public static partial void ServerNamedQueueChangedAfterRecovery(
        this ILogger logger,
        string oldName,
        string newName);

    [LoggerMessage(LogLevel.Information, "Recovering consumer {ConsumerTag}")]
    public static partial void RecoveringConsumer(this ILogger logger, string consumerTag);

    [LoggerMessage(LogLevel.Error, "Error disposing consumer {ConsumerTag}")]
    public static partial void ErrorDisposingConsumer(this ILogger logger, Exception ex, string consumerTag);

    [LoggerMessage(LogLevel.Warning, "Failed to delete queue {Queue}")]
    public static partial void FailedToDeleteQueue(this ILogger logger, Exception ex, string queue);

    [LoggerMessage(
        LogLevel.Warning,
        "Reconnection attempt {Attempt} failed again. Retrying in {Delay}")]
    public static partial void ReconnectionAttemptFailedAgain(
        this ILogger logger,
        Exception ex,
        int attempt,
        TimeSpan delay);

    [LoggerMessage(
        LogLevel.Warning,
        "Re-attaching the consumer for queue {Queue} failed again (attempt {Attempt}). Retrying in {Delay}")]
    public static partial void FailedToReattachConsumerForQueueAgain(
        this ILogger logger,
        Exception ex,
        string queue,
        int attempt,
        TimeSpan delay);
}
