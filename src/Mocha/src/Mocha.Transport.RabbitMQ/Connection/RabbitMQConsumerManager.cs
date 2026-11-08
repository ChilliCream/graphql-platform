using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Manages RabbitMQ connections and consumers with automatic reconnection.
/// </summary>
public sealed partial class RabbitMQConsumerManager : RabbitMQConnectionManagerBase
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
    /// Gets whether the connection and every registered consumer are healthy.
    /// </summary>
    public bool IsHealthy => IsConnected && _registeredConsumers.All(static consumer => consumer.IsHealthy);

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
            dependencies: [],
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
    /// <param name="dependencies">
    /// Resolves the current consumers that must be attached before this consumer starts consuming.
    /// A resolver returns <c>null</c> while its consumer is stopped.
    /// </param>
    /// <param name="messageHandler">The callback invoked for each delivered message.</param>
    /// <param name="prefetchCount">The maximum number of unacknowledged messages the broker delivers to this consumer.</param>
    /// <param name="consumerDispatchConcurrency">The maximum number of messages processed concurrently.</param>
    /// <param name="cancellationToken">A token to cancel the registration operation.</param>
    internal async Task<RegisteredConsumer> RegisterConsumerAsync(
        string queueName,
        Func<IConnection, IChannel, CancellationToken, Task>? provisionAsync,
        ImmutableArray<Func<RegisteredConsumer?>> dependencies,
        Func<IChannel, BasicDeliverEventArgs, CancellationToken, ValueTask> messageHandler,
        ushort prefetchCount,
        ushort consumerDispatchConcurrency,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentOutOfRangeException.ThrowIfZero(consumerDispatchConcurrency);

        var registration = new RegisteredConsumer(consumerDispatchConcurrency)
        {
            Manager = this,
            QueueName = queueName,
            ProvisionAsync = provisionAsync,
            Dependencies = dependencies,
            MessageHandler = messageHandler,
            PrefetchCount = prefetchCount
        };

        AddConsumer(registration);

        if (IsDisposed)
        {
            await registration.DisposeAsync();
            ObjectDisposedException.ThrowIf(IsDisposed, this);
        }

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
    /// Restores registered consumers on a new connection and starts monitoring connection health.
    /// </summary>
    /// <param name="connection">The newly created <see cref="IConnection"/>.</param>
    /// <param name="cancellationToken">A token to cancel the reconnection of consumers.</param>
    /// <returns>A task that completes after consumer attachment attempts finish and health monitoring starts.</returns>
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
        catch (OperationCanceledException) when (consumer.IsDisposed)
        {
            // The consumer was stopped while it was attaching.
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
}

internal static partial class Logs
{
    [LoggerMessage(LogLevel.Error, "Error processing message from queue {Queue}")]
    public static partial void ErrorProcessingMessageFromQueue(this ILogger logger, Exception ex, string queue);

    [LoggerMessage(LogLevel.Information, "Registered consumer for queue {Queue}")]
    public static partial void RegisteredConsumerForQueue(this ILogger logger, string queue);

    [LoggerMessage(LogLevel.Information, "Reconnecting {Count} registered consumers")]
    public static partial void ReconnectConsumersAsync(this ILogger logger, int count);

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

    [LoggerMessage(LogLevel.Warning, "Connection health check failed - attempting reconnection")]
    public static partial void ConnectionHealthCheckFailedAttemptingReconnection(this ILogger logger);

    [LoggerMessage(LogLevel.Trace, "Connection health check: OK")]
    public static partial void ConnectionHealthCheckOk(this ILogger logger);

    [LoggerMessage(LogLevel.Error, "Error during connection health check")]
    public static partial void ErrorDuringConnectionHealthCheck(this ILogger logger, Exception ex);

    [LoggerMessage(LogLevel.Error, "Error disposing consumer {ConsumerTag}")]
    public static partial void ErrorDisposingConsumer(this ILogger logger, Exception ex, string consumerTag);

    [LoggerMessage(LogLevel.Warning, "Failed to delete queue {Queue}")]
    public static partial void FailedToDeleteQueue(this ILogger logger, Exception ex, string queue);

    [LoggerMessage(
        LogLevel.Warning,
        "Re-attaching the consumer for queue {Queue} failed again (attempt {Attempt}). Retrying in {Delay}")]
    public static partial void FailedToReattachConsumerForQueueAgain(
        this ILogger logger,
        Exception ex,
        string queue,
        int attempt,
        TimeSpan delay);

    [LoggerMessage(LogLevel.Error, "Consumer for queue {Queue} is unhealthy: all {Concurrency} handler slots are occupied by cancelled handlers. Processing resumes when a handler exits")]
    public static partial void ConsumerRecoveryBlocked(this ILogger logger, string queue, int concurrency);

    [LoggerMessage(LogLevel.Information, "Consumer for queue {Queue} has handler capacity available again")]
    public static partial void ConsumerRecoveryResumed(this ILogger logger, string queue);

    [LoggerMessage(LogLevel.Error, "Error cancelling handlers for queue {Queue}")]
    public static partial void ErrorCancellingConsumerHandlers(this ILogger logger, Exception exception, string queue);
}
