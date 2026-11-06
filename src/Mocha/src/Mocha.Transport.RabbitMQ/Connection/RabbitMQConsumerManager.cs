using System.Collections.Immutable;
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
    private IConnection? _observedConnection;
    private volatile bool _receiveTopologyProvisioned;

    /// <summary>
    /// Creates a new consumer manager that will use the specified connection factory to establish RabbitMQ connections.
    /// </summary>
    /// <param name="logger">The logger for connection and consumer lifecycle events.</param>
    /// <param name="connectionFactory">A factory delegate that creates new RabbitMQ connections on demand.</param>
    public RabbitMQConsumerManager(
        ILogger<RabbitMQConsumerManager> logger,
        Func<CancellationToken, ValueTask<IConnection>> connectionFactory) : base(logger, connectionFactory) { }

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
    public Task<IAsyncDisposable> RegisterConsumerAsync(
        string queueName,
        Func<IChannel, BasicDeliverEventArgs, CancellationToken, ValueTask> messageHandler,
        ushort prefetchCount,
        ushort consumerDispatchConcurrency,
        CancellationToken cancellationToken)
        => RegisterConsumerAsync(
            queueName,
            messageHandler,
            receiveTopology: null,
            prefetchCount,
            consumerDispatchConcurrency,
            cancellationToken);

    /// <inheritdoc cref="RegisterConsumerAsync(string, Func{IChannel, BasicDeliverEventArgs, CancellationToken, ValueTask}, ushort, ushort, CancellationToken)"/>
    /// <param name="queueName">The name of the RabbitMQ queue to consume from.</param>
    /// <param name="messageHandler">The callback invoked for each delivered message, receiving the channel, delivery event args, and a cancellation token.</param>
    /// <param name="receiveTopology">
    /// The receive topology to provision before consumption starts, or <see langword="null"/> when
    /// automatic provisioning is disabled.
    /// </param>
    /// <param name="prefetchCount">The maximum number of unacknowledged messages the broker will deliver to this consumer.</param>
    /// <param name="consumerDispatchConcurrency">The maximum number of messages dispatched concurrently to the consumer callback. When greater than 1, multiple messages are processed in parallel.</param>
    /// <param name="cancellationToken">A token to cancel the registration operation.</param>
    internal async Task<IAsyncDisposable> RegisterConsumerAsync(
        string queueName,
        Func<IChannel, BasicDeliverEventArgs, CancellationToken, ValueTask> messageHandler,
        RabbitMQReceiveTopology? receiveTopology,
        ushort prefetchCount,
        ushort consumerDispatchConcurrency,
        CancellationToken cancellationToken)
    {
        var registration = new RegisteredConsumer(consumerDispatchConcurrency)
        {
            Manager = this,
            QueueName = queueName,
            MessageHandler = messageHandler,
            ReceiveTopology = receiveTopology,
            PrefetchCount = prefetchCount
        };

        AddConsumer(registration);

        if (IsConnected)
        {
            await registration.ConnectAsync(CurrentConnection!, cancellationToken);
        }

        Logger.RegisteredConsumerForQueue(queueName);

        return registration;
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
        SubscribeToConsumerRecovery(connection);

        _receiveTopologyProvisioned = false;
        await ProvisionReceiveTopologiesAsync(connection, cancellationToken);
        await ReconnectConsumersAsync(connection, cancellationToken);

        // Start health monitoring if not already running
        StartHealthMonitoring();
    }

    /// <summary>
    /// Called when the RabbitMQ client library's built-in automatic recovery succeeds and the
    /// existing connection object is restored rather than replaced.
    /// </summary>
    /// <remarks>
    /// Even though the client library recovers the connection, channels and consumers may be in
    /// an inconsistent state. This method iterates all registered consumers and reconnects each
    /// one with a fresh channel, ensuring that message delivery resumes correctly after recovery.
    /// </remarks>
    /// <param name="cancellationToken">A token to cancel the consumer reconnection.</param>
    /// <returns>A task that completes when all consumers have been re-attached to the recovered connection.</returns>
    protected override async Task OnConnectionRecoveredAsync(CancellationToken cancellationToken)
    {
        _receiveTopologyProvisioned = false;

        if (CurrentConnection is not null)
        {
            await ProvisionReceiveTopologiesAsync(CurrentConnection, cancellationToken);
            await ReconnectConsumersAsync(CurrentConnection, cancellationToken);
        }
    }

    private async Task ReconnectConsumersAsync(IConnection connection, CancellationToken cancellationToken)
    {
        var consumers = _registeredConsumers;

        if (consumers.IsEmpty)
        {
            return;
        }

        Logger.ReconnectConsumersAsync(consumers.Length);

        foreach (var consumer in consumers)
        {
            try
            {
                await consumer.AttachAsync(connection, cancellationToken);
                Logger.ReconnectConsumerForQueue(consumer.QueueName);
            }
            catch (Exception ex)
            {
                Logger.FailedToReconnectConsumerForQueue(ex, consumer.QueueName);
            }
        }
    }

    private void SubscribeToConsumerRecovery(IConnection connection)
    {
        _observedConnection?.RecoveringConsumerAsync -= OnRecoveringConsumerAsync;
        _observedConnection = connection;
        connection.RecoveringConsumerAsync += OnRecoveringConsumerAsync;
    }

    protected override Task OnConnectionLostAsync()
    {
        _receiveTopologyProvisioned = false;
        return Task.CompletedTask;
    }

    private async Task OnRecoveringConsumerAsync(object? sender, RecoveringConsumerEventArgs e)
    {
        if (_receiveTopologyProvisioned || sender is not IConnection connection)
        {
            return;
        }

        // Provision every receive topology before any consumer resumes.
        while (true)
        {
            try
            {
                await ProvisionReceiveTopologiesAsync(connection, e.CancellationToken);
                _receiveTopologyProvisioned = true;
                return;
            }
            catch (Exception) when (connection.IsOpen && !e.CancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), e.CancellationToken);
            }
        }
    }

    private async Task ProvisionReceiveTopologiesAsync(IConnection connection, CancellationToken cancellationToken)
    {
        foreach (var consumer in _registeredConsumers)
        {
            await consumer.ProvisionReceiveTopologyAsync(connection, cancellationToken);
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

                    try
                    {
                        await EnsureConnectedAsync(cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        Logger.ReconnectionAttemptFailed(ex);
                    }
                }
                else
                {
                    Logger.ConnectionHealthCheckOk();
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

        _observedConnection?.RecoveringConsumerAsync -= OnRecoveringConsumerAsync;
        _observedConnection = null;

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
    internal sealed class RegisteredConsumer(ushort consumerDispatchConcurrency) : IAsyncDisposable
    {
        /// <summary>
        /// Gets the owning consumer manager responsible for connection lifecycle.
        /// </summary>
        public required RabbitMQConsumerManager Manager { get; init; }

        /// <summary>
        /// Gets the name of the RabbitMQ queue this consumer is bound to.
        /// </summary>
        public required string QueueName { get; init; }

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
        /// Gets the receive topology, or <see langword="null"/> when automatic provisioning is disabled.
        /// </summary>
        public RabbitMQReceiveTopology? ReceiveTopology { get; init; }

        /// <summary>
        /// Gets the maximum number of unacknowledged messages the broker delivers to this consumer.
        /// </summary>
        public required ushort PrefetchCount { get; init; }

        /// <summary>
        /// Gets the maximum number of messages dispatched concurrently to the consumer callback.
        /// </summary>
        public ushort ConsumerDispatchConcurrency { get; } = consumerDispatchConcurrency;

        /// <summary>
        /// Gets or sets the server-assigned consumer tag identifying this consumer on the channel.
        /// </summary>
        public string? ConsumerTag { get; set; }

        /// <summary>
        /// Gets or sets the dedicated channel used by this consumer for message delivery.
        /// </summary>
        public IChannel? Channel { get; set; }

        private readonly CancellationTokenSource _consumerCts = new();
        private readonly SemaphoreSlim _dispatchSemaphore = new(consumerDispatchConcurrency, consumerDispatchConcurrency);

        /// <summary>
        /// Provisions the enabled receive topology and starts consuming on a new channel.
        /// </summary>
        public async Task ConnectAsync(IConnection connection, CancellationToken cancellationToken)
        {
            await ProvisionReceiveTopologyAsync(connection, cancellationToken);
            await AttachAsync(connection, cancellationToken);
        }

        /// <summary>
        /// Starts consuming from an existing queue and disconnects the previous consumer after attachment is attempted.
        /// </summary>
        public async Task AttachAsync(IConnection connection, CancellationToken cancellationToken)
        {
            // Keep the previous consumer attached until the replacement attaches.
            var previousChannel = Channel;
            var previousConsumerTag = ConsumerTag;

            try
            {
                await ConsumeAsync(connection, cancellationToken);
            }
            finally
            {
                await DisconnectAsync(previousChannel, previousConsumerTag, cancellationToken);
            }
        }

        /// <summary>
        /// Provisions the queue, exchanges, and bindings enabled for this consumer.
        /// </summary>
        public async Task ProvisionReceiveTopologyAsync(IConnection connection, CancellationToken cancellationToken)
        {
            if (ReceiveTopology is not { AutoProvision: true } topology)
            {
                return;
            }

            Manager.Logger.ProvisioningReceiveTopology(QueueName);

            try
            {
                await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
                await topology.ProvisionAsync(channel, cancellationToken);
            }
            catch (Exception ex)
            {
                Manager.Logger.FailedToProvisionReceiveTopology(ex, QueueName);
                throw;
            }
        }

        private async Task ConsumeAsync(IConnection connection, CancellationToken cancellationToken)
        {
            IChannel? channel = null;
            try
            {
                var channelOptions = new CreateChannelOptions(
                    publisherConfirmationsEnabled: false,
                    publisherConfirmationTrackingEnabled: false,
                    consumerDispatchConcurrency: ConsumerDispatchConcurrency);
                channel = await connection.CreateChannelAsync(channelOptions, cancellationToken);
                var consumer = new AsyncEventingBasicConsumer(channel);
                consumer.ReceivedAsync += async (_, eventArgs) =>
                {
                    await _dispatchSemaphore.WaitAsync(_consumerCts.Token);
                    try
                    {
                        await MessageHandler(channel, eventArgs, _consumerCts.Token);
                    }
                    finally
                    {
                        _dispatchSemaphore.Release();
                    }
                };

                await channel.BasicQosAsync(
                    prefetchSize: 0,
                    prefetchCount: PrefetchCount, //100
                    global: false,
                    cancellationToken: cancellationToken);

                var tag = await channel.BasicConsumeAsync(
                    queue: QueueName,
                    autoAck: false,
                    consumer: consumer,
                    cancellationToken: cancellationToken);

                ConsumerTag = tag;
                Channel = channel;
            }
            catch
            {
                if (channel is not null)
                {
                    await channel.DisposeAsync();
                }

                ConsumerTag = null;
                Channel = null;
                throw;
            }
        }

        private static async Task DisconnectAsync(
            IChannel? channel,
            string? consumerTag,
            CancellationToken cancellationToken)
        {
            if (consumerTag is not null && channel is { IsOpen: true })
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

            if (channel is not null)
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
        }

        /// <summary>
        /// Disconnects the consumer from its channel, unregisters it from the manager, and releases all resources.
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            Manager.RemoveConsumer(this);
            await _consumerCts.CancelAsync();

            var channel = Channel;
            var consumerTag = ConsumerTag;
            ConsumerTag = null;
            Channel = null;
            await DisconnectAsync(channel, consumerTag, CancellationToken.None);

            _dispatchSemaphore.Dispose();
            _consumerCts.Dispose();
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

    [LoggerMessage(LogLevel.Information, "Connection recovery succeeded")]
    public static partial void ConnectionRecoverySucceeded(this ILogger logger);

    [LoggerMessage(LogLevel.Information, "Successfully recovered connection, topology, and consumers")]
    public static partial void SuccessfullyRecoveredConnectionTopologyAndConsumers(this ILogger logger);

    [LoggerMessage(LogLevel.Error, "Error during post-recovery operations")]
    public static partial void ErrorDuringPostRecoveryOperations(this ILogger logger, Exception ex);

    [LoggerMessage(LogLevel.Information, "Connection recovery started")]
    public static partial void ConnectionRecoveryStarted(this ILogger logger);

    [LoggerMessage(LogLevel.Warning, "Connection blocked: {Reason}")]
    public static partial void ConnectionBlocked(this ILogger logger, string reason);

    [LoggerMessage(LogLevel.Information, "Connection unblocked")]
    public static partial void ConnectionUnblocked(this ILogger logger);

    [LoggerMessage(LogLevel.Warning, "Connection health check failed - attempting reconnection")]
    public static partial void ConnectionHealthCheckFailedAttemptingReconnection(this ILogger logger);

    [LoggerMessage(LogLevel.Error, "Reconnection attempt failed")]
    public static partial void ReconnectionAttemptFailed(this ILogger logger, Exception ex);

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

    [LoggerMessage(LogLevel.Error, "Connection recovery failed. Will retry. Error: {ErrorMessage}")]
    public static partial void ConnectionRecoveryFailedWillRetry(
        this ILogger logger,
        Exception? exception,
        string? errorMessage);

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

    [LoggerMessage(LogLevel.Debug, "Provisioning receive topology for queue {Queue}")]
    public static partial void ProvisioningReceiveTopology(this ILogger logger, string queue);

    [LoggerMessage(LogLevel.Error, "Failed to provision receive topology for queue {Queue}")]
    public static partial void FailedToProvisionReceiveTopology(this ILogger logger, Exception ex, string queue);
}
