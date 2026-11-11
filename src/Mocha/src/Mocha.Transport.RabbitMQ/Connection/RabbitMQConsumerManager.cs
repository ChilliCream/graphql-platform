using System.Collections.Immutable;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Owns the RabbitMQ connection that consumes messages, and the consumers on it.
/// </summary>
internal sealed class RabbitMQConsumerManager : IAsyncDisposable
{
    private static readonly CreateChannelOptions s_probeChannelOptions = new(
        publisherConfirmationsEnabled: false,
        publisherConfirmationTrackingEnabled: false);

    private readonly RabbitMQConnectionManager _connection;
    private readonly object _sync = new();
    private ImmutableArray<RabbitMQConsumer> _consumers = [];
    private Task? _disposeTask;
    private volatile bool _isDisposed;

    /// <summary>
    /// Creates a consumer manager.
    /// </summary>
    /// <param name="logger">The logger for consumer events.</param>
    /// <param name="connection">The manager of the connection that consumes messages.</param>
    public RabbitMQConsumerManager(ILogger logger, RabbitMQConnectionManager connection)
    {
        Logger = logger;
        _connection = connection;
    }

    /// <summary>
    /// Gets the logger for consumer events.
    /// </summary>
    public ILogger Logger { get; }

    /// <summary>
    /// Gets the manager of the connection that consumes messages.
    /// </summary>
    public RabbitMQConnectionManager Connection => _connection;

    /// <summary>
    /// Gets or sets how long one attempt to attach a consumer may take before it is abandoned and retried.
    /// </summary>
    public TimeSpan AttachTimeout { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Gets or sets how long a consumer waits for a handler of a lost channel to return before it reports
    /// that it is blocked.
    /// </summary>
    public TimeSpan BlockedGracePeriod { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets whether a ready connection exists.
    /// </summary>
    public bool IsConnected => _connection.IsConnected;

    /// <summary>
    /// Gets whether the connection is ready and every consumer is attached.
    /// </summary>
    public bool IsHealthy => HealthFailureReason is null;

    /// <summary>
    /// Gets the reason the connection or a consumer is unhealthy, or <c>null</c> when all are healthy.
    /// </summary>
    public string? HealthFailureReason
    {
        get
        {
            if (!_connection.IsConnected)
            {
                return "The consumer connection is unavailable.";
            }

            foreach (var consumer in _consumers)
            {
                if (consumer.HealthFailureReason is { } reason)
                {
                    return $"Queue '{consumer.QueueName}': {reason}";
                }
            }

            return null;
        }
    }

    /// <summary>
    /// Connects, as <see cref="RabbitMQConnectionManager.StartAsync"/> describes.
    /// </summary>
    public Task StartAsync(CancellationToken cancellationToken) => _connection.StartAsync(cancellationToken);

    /// <summary>
    /// Makes one round trip on a new connection, so a connection the broker refuses after the handshake
    /// does not count as ready.
    /// </summary>
    public static async Task SetUpConnectionAsync(IConnection connection, CancellationToken cancellationToken)
    {
        await using var channel = await connection.CreateChannelAsync(s_probeChannelOptions, cancellationToken);
        await channel.CloseAsync(cancellationToken);
    }

    /// <summary>
    /// Registers a consumer for a queue and waits for its first attempt to attach. The consumer attaches
    /// again by itself whenever its channel or its connection is lost, until it is stopped.
    /// </summary>
    /// <param name="queueName">The name of the queue to consume.</param>
    /// <param name="prepareAsync">
    /// Runs on every new channel of the consumer before it consumes, for example to declare the queue and
    /// its bindings, or <c>null</c> to consume at once.
    /// </param>
    /// <param name="messageHandler">Handles one delivered message.</param>
    /// <param name="maxPrefetch">The largest number of unacknowledged messages the broker delivers.</param>
    /// <param name="maxConcurrency">The largest number of messages that are handled at the same time.</param>
    /// <param name="cancellationToken">Ends the wait for the first attempt and stops the consumer.</param>
    /// <returns>
    /// The consumer. It is not attached when no connection was ready or the first attempt failed for a
    /// reason that a later attempt can overcome.
    /// </returns>
    /// <exception cref="Exception">The broker refused the first attempt. The consumer is stopped.</exception>
    public async Task<RabbitMQConsumer> RegisterConsumerAsync(
        string queueName,
        Func<IConnection, IChannel, CancellationToken, Task>? prepareAsync,
        Func<IChannel, BasicDeliverEventArgs, CancellationToken, ValueTask> messageHandler,
        ushort maxPrefetch,
        ushort maxConcurrency,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentOutOfRangeException.ThrowIfZero(maxConcurrency);

        var consumer = new RabbitMQConsumer(this, queueName, prepareAsync, messageHandler, maxPrefetch, maxConcurrency);
        ImmutableInterlocked.Update(ref _consumers, static (consumers, added) => consumers.Add(added), consumer);

        if (_isDisposed)
        {
            await consumer.DisposeAsync();
            ObjectDisposedException.ThrowIf(_isDisposed, this);
        }

        Exception? failure;
        try
        {
            failure = await consumer.FirstAttempt.WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            await consumer.DisposeAsync();
            throw;
        }

        // The disposal of the manager stopped the consumer while it attached.
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        if (failure is not null && RabbitMQFailure.IsPermanent(failure))
        {
            using (_connection.BeginLogScope())
            {
                Logger.ConsumerRegistrationFailed(failure, queueName);
            }

            // A consumer the broker refuses does not keep attaching in the background.
            await consumer.DisposeAsync();
            ExceptionDispatchInfo.Throw(failure);
        }

        return consumer;
    }

    /// <summary>
    /// Deletes a queue on the ready connection. Does nothing when there is no ready connection, and logs
    /// a failure instead of throwing it.
    /// </summary>
    public async Task DeleteQueueAsync(string queueName, CancellationToken cancellationToken)
    {
        if (_connection.Connection is not { } connection || cancellationToken.IsCancellationRequested)
        {
            return;
        }

        // A cancelled open would cost the connection a channel number, so the open runs to its end.
        var opening = _connection.OpenChannelAsync(connection, s_probeChannelOptions, AttachTimeout);
        try
        {
            var channel = await opening.WaitOrAbandonAsync(cancellationToken);
            try
            {
                await channel
                    .QueueDeleteAsync(queueName, cancellationToken: cancellationToken)
                    .WaitOrAbandonAsync(cancellationToken);
            }
            finally
            {
                _ = RabbitMQConnectionManager.CloseChannelAsync(channel);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The caller cancelled the cleanup. A channel that still opens is closed.
            _ = CloseWhenOpenedAsync(opening);
        }
        catch (Exception ex)
        {
            using (_connection.BeginLogScope())
            {
                Logger.QueueDeleteFailed(ex, queueName);
            }
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

    internal void Remove(RabbitMQConsumer consumer)
        => ImmutableInterlocked.Update(ref _consumers, static (consumers, removed) => consumers.Remove(removed), consumer);

    /// <summary>
    /// Stops every consumer without waiting for running handlers, and closes the connection.
    /// </summary>
    public ValueTask DisposeAsync()
    {
        lock (_sync)
        {
            return new ValueTask(_disposeTask ??= DisposeCoreAsync());
        }
    }

    private async Task DisposeCoreAsync()
    {
        // Leaves the caller's lock.
        await Task.Yield();
        _isDisposed = true;

        // The consumers stop side by side, so a broker that does not answer delays the disposal only once.
        await Task.WhenAll(_consumers.Select(StopConsumerAsync));
        await _connection.DisposeAsync();
    }

    private async Task StopConsumerAsync(RabbitMQConsumer consumer)
    {
        try
        {
            await consumer.DisposeAsync();
        }
        catch (Exception ex)
        {
            using (_connection.BeginLogScope())
            {
                Logger.ConsumerStopFailed(ex, consumer.QueueName);
            }
        }
    }
}
