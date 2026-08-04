using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Manages RabbitMQ connections and provides channel pooling for publishing messages.
/// </summary>
public sealed class RabbitMQDispatcher(
    ILogger<RabbitMQDispatcher> logger,
    Func<CancellationToken, ValueTask<IConnection>> connectionFactory,
    Func<IConnection, CancellationToken, Task> onConnectionEstablished)
    : RabbitMQConnectionManagerBase(logger, connectionFactory)
{
    private const int MaxPooledChannels = 10;

    private readonly ConcurrentQueue<IChannel> _channelPool = new();
    private int _pooledChannelCount;

    /// <summary>
    /// Rents a channel from the pool or creates a new one.
    /// The caller is responsible for returning the channel via ReturnChannelAsync.
    /// </summary>
    public async ValueTask<IChannel> RentChannelAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);

        // Try to get a channel from the pool
        while (_channelPool.TryDequeue(out var pooledChannel))
        {
            Interlocked.Decrement(ref _pooledChannelCount);

            if (pooledChannel.IsOpen)
            {
                Logger.RentedChannelFromPool();

                return pooledChannel;
            }

            // Channel is closed, dispose it
            await DisposeChannelSafelyAsync(pooledChannel);
        }

        // No valid pooled channel, create a new one
        var connection = await GetConnectionAsync(cancellationToken);
        var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        Logger.CreatedNewChannel();

        return channel;
    }

    /// <summary>
    /// Returns a channel to the pool for reuse.
    /// If the channel is closed or the pool is full, the channel is disposed.
    /// </summary>
    public async ValueTask ReturnChannelAsync(IChannel channel)
    {
        if (IsDisposed || !channel.IsOpen)
        {
            await DisposeChannelSafelyAsync(channel);

            return;
        }

        // Check if pool has room
        if (Interlocked.Increment(ref _pooledChannelCount) <= MaxPooledChannels)
        {
            _channelPool.Enqueue(channel);

            Logger.ReturnedChannelToPool(_pooledChannelCount);
        }
        else
        {
            // Pool is full, dispose the channel
            Interlocked.Decrement(ref _pooledChannelCount);

            await DisposeChannelSafelyAsync(channel);

            Logger.ChannelPoolFullDisposedChannel();
        }
    }

    protected override async Task OnConnectionEstablished(IConnection connection, CancellationToken cancellationToken)
    {
        await onConnectionEstablished(connection, cancellationToken);
    }

    protected override async Task OnBeforeConnectionCreatedAsync(CancellationToken cancellationToken)
    {
        await ClearChannelPoolAsync();
    }

    protected override async Task OnConnectionLostAsync()
    {
        await ClearChannelPoolAsync();
    }

    protected override async Task OnConnectionRecoveredAsync(CancellationToken cancellationToken)
    {
        await ClearChannelPoolAsync();
    }

    private async Task ClearChannelPoolAsync()
    {
        while (_channelPool.TryDequeue(out var channel))
        {
            Interlocked.Decrement(ref _pooledChannelCount);
            await DisposeChannelSafelyAsync(channel);
        }

        Logger.ClearedChannelPool();
    }

    private static async ValueTask DisposeChannelSafelyAsync(IChannel channel)
    {
        try
        {
            if (channel.IsOpen)
            {
                await channel.CloseAsync();
            }

            await channel.DisposeAsync();
        }
        catch
        {
            // Ignore disposal errors
        }
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        await ClearChannelPoolAsync();
    }
}

// Add these log messages to your Logs class
internal static partial class Logs
{
    [LoggerMessage(LogLevel.Debug, "Rented channel from pool")]
    public static partial void RentedChannelFromPool(this ILogger logger);

    [LoggerMessage(LogLevel.Debug, "Created new channel")]
    public static partial void CreatedNewChannel(this ILogger logger);

    [LoggerMessage(LogLevel.Debug, "Returned channel to pool. Pool size: {PoolSize}")]
    public static partial void ReturnedChannelToPool(this ILogger logger, int poolSize);

    [LoggerMessage(LogLevel.Debug, "Channel pool full, disposed channel")]
    public static partial void ChannelPoolFullDisposedChannel(this ILogger logger);

    [LoggerMessage(LogLevel.Debug, "Cleared channel pool")]
    public static partial void ClearedChannelPool(this ILogger logger);

    [LoggerMessage(LogLevel.Information, "RabbitMQ Dispatcher disposed")]
    public static partial void DispatcherDisposed(this ILogger logger);
}
