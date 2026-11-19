using System.Collections.Immutable;
using System.Runtime.ExceptionServices;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Owns the RabbitMQ connection that consumes messages, and the consumers on it.
/// </summary>
public sealed class RabbitMQConsumerManager : IAsyncDisposable
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
    /// <param name="timeProvider">The clock used for elapsed times, delays and timeouts.</param>
    /// <param name="options">The timeouts and the retry delay of the consumers.</param>
    /// <param name="connection">The manager of the connection that consumes messages.</param>
    internal RabbitMQConsumerManager(
        RabbitMQConsumerLogger logger,
        TimeProvider timeProvider,
        RabbitMQConsumerManagerOptions options,
        RabbitMQConnectionManager connection)
    {
        Logger = logger;
        TimeProvider = timeProvider;
        Options = options;
        _connection = connection;
    }

    /// <summary>
    /// Gets the logger for consumer events.
    /// </summary>
    internal RabbitMQConsumerLogger Logger { get; }

    /// <summary>
    /// Gets the clock used for elapsed times, delays and timeouts.
    /// </summary>
    internal TimeProvider TimeProvider { get; }

    /// <summary>
    /// Gets the timeouts and the retry delay of the consumers.
    /// </summary>
    internal RabbitMQConsumerManagerOptions Options { get; }

    /// <summary>
    /// Gets the manager of the connection that consumes messages.
    /// </summary>
    internal RabbitMQConnectionManager Connection => _connection;

    /// <summary>
    /// Gets whether a ready connection exists.
    /// </summary>
    public bool IsConnected => _connection.IsConnected;

    /// <summary>
    /// Gets the registered consumers.
    /// </summary>
    internal ImmutableArray<RabbitMQConsumer> Consumers => _consumers;

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
    /// <exception cref="Exception">
    /// The first attempt failed for a reason that repeats on every attempt, or the consumer failed
    /// unexpectedly. The consumer is stopped.
    /// </exception>
    internal async Task<RabbitMQConsumer> RegisterConsumerAsync(
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

        var consumer = new RabbitMQConsumer(
            _connection,
            Logger,
            Options,
            TimeProvider,
            queueName,
            prepareAsync,
            messageHandler,
            maxPrefetch,
            maxConcurrency,
            Remove);
        ImmutableInterlocked.Update(ref _consumers, static (consumers, added) => consumers.Add(added), consumer);
        consumer.Start();

        if (_isDisposed)
        {
            await consumer.DisposeAsync();
            ObjectDisposedException.ThrowIf(_isDisposed, this);
        }

        Exception? failure = null;
        try
        {
            await consumer.Startup.WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await consumer.DisposeAsync();
            throw;
        }
        catch (Exception ex)
        {
            failure = ex;
        }

        // The disposal of the manager stopped the consumer while it attached.
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        if (failure is not null)
        {
            Logger.ConsumerRegistrationFailed(failure, queueName);

            // A consumer whose attempts all fail the same way does not keep attaching in the background.
            await consumer.DisposeAsync();
            ExceptionDispatchInfo.Throw(failure);
        }

        return consumer;
    }

    /// <summary>
    /// Deletes a queue on the ready connection, waiting no longer than the queue delete timeout for the
    /// broker. Does nothing when there is no
    /// ready connection, and logs a failure instead of throwing it.
    /// </summary>
    public async Task DeleteQueueAsync(string queueName)
    {
        if (_connection.Connection is not { } connection)
        {
            return;
        }

        var timeout = Options.QueueDeleteTimeout;
        using var deadline = new CancellationTokenSource(timeout, TimeProvider);
        var deadlineToken = deadline.Token;

        // A cancelled open would cost the connection a channel number, so the open runs to its end.
        var opening = _connection.OpenChannelAsync(connection, s_probeChannelOptions, Options.AttachTimeout);
        IChannel? channel = null;
        try
        {
            channel = await opening.WaitOrAbandonAsync(deadlineToken);
            try
            {
                await channel
                    .QueueDeleteAsync(queueName, cancellationToken: deadlineToken)
                    .WaitOrAbandonAsync(deadlineToken);
            }
            finally
            {
                _ = _connection.CloseChannelAsync(channel);
            }
        }
        catch (OperationCanceledException ex) when (deadlineToken.IsCancellationRequested)
        {
            if (channel is null)
            {
                // A channel that this method did not get is closed once it is open.
                _ = opening.CloseWhenOpenedAsync(_connection);
            }

            LogQueueDeleteFailed(ThrowHelper.QueueDeleteTimedOut(queueName, timeout, ex), queueName);
        }
        catch (Exception ex)
        {
            LogQueueDeleteFailed(ex, queueName);
        }
    }

    private void LogQueueDeleteFailed(Exception exception, string queueName)
    {
        Logger.QueueDeleteFailed(exception, queueName);
    }

    internal void Remove(RabbitMQConsumer consumer)
        => ImmutableInterlocked.Update(
            ref _consumers,
            static (consumers, removed) => consumers.Remove(removed),
            consumer);

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
            Logger.ConsumerStopFailed(ex, consumer.QueueName);
        }
    }
}
