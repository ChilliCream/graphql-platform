using System.Collections.Concurrent;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;

namespace Mocha.Transport.AzureServiceBus;

/// <summary>
/// Manages the single <see cref="ServiceBusClient"/> (one AMQP connection) and caches
/// <see cref="ServiceBusSender"/> instances per entity path. Also exposes administration
/// operations for topology provisioning.
/// </summary>
public sealed class AzureServiceBusClientManager : IAsyncDisposable
{
    private readonly ServiceBusConnection _connection;
    private readonly ConcurrentDictionary<string, ServiceBusSender> _senders = new();
    private volatile bool _isDisposed;

    /// <summary>
    /// Creates a new client manager from the given transport configuration.
    /// </summary>
    /// <param name="configuration">The transport configuration containing connection settings.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when neither a connection string nor a fully qualified namespace with credential is provided.
    /// </exception>
    public AzureServiceBusClientManager(AzureServiceBusTransportConfiguration configuration)
    {
        var clientOptions = new ServiceBusClientOptions
        {
            TransportType = configuration.TransportType,
            RetryOptions =
                configuration.RetryOptions
                ?? new ServiceBusRetryOptions
                {
                    MaxRetries = 3,
                    Mode = ServiceBusRetryMode.Exponential,
                    Delay = TimeSpan.FromSeconds(0.8),
                    MaxDelay = TimeSpan.FromMinutes(1)
                }
        };

        _connection = ServiceBusConnection.Create(configuration, clientOptions);
    }

    /// <summary>
    /// Gets a cached <see cref="ServiceBusSender"/> for the specified entity path, creating one if necessary.
    /// </summary>
    /// <param name="entityPath">The queue or topic name to send to.</param>
    /// <returns>A thread-safe sender for the entity.</returns>
    public ServiceBusSender GetSender(string entityPath)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        // Fast path: avoid delegate allocation on cache hit
        if (_senders.TryGetValue(entityPath, out var sender))
        {
            return sender;
        }

        return _senders.GetOrAdd(entityPath, static (path, conn) => conn.CreateSender(path), _connection);
    }

    /// <summary>
    /// Removes the cached <see cref="ServiceBusSender"/> for <paramref name="entityPath"/> so the
    /// next <see cref="GetSender"/> call builds a fresh one against a recreated entity. The
    /// orphaned sender is left to GC: a concurrent dispatcher may still be mid-send on it, and
    /// disposing here would race them into <see cref="ObjectDisposedException"/>.
    /// </summary>
    /// <param name="entityPath">The queue or topic name whose cached sender should be invalidated.</param>
    public void InvalidateSender(string entityPath)
    {
        if (_isDisposed)
        {
            return;
        }

        _senders.TryRemove(entityPath, out _);
    }

    /// <summary>
    /// Creates a new <see cref="ServiceBusProcessor"/> for consuming messages from a queue.
    /// </summary>
    /// <param name="queueName">The queue to consume from.</param>
    /// <param name="options">The processor options controlling concurrency and prefetch.</param>
    /// <returns>A new processor instance.</returns>
    public ServiceBusProcessor CreateProcessor(string queueName, ServiceBusProcessorOptions options)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        return _connection.CreateProcessor(queueName, options);
    }

    /// <summary>
    /// Creates a new <see cref="ServiceBusProcessor"/> for consuming messages from a topic subscription.
    /// </summary>
    /// <param name="topicName">The topic name.</param>
    /// <param name="subscriptionName">The subscription name.</param>
    /// <param name="options">The processor options controlling concurrency and prefetch.</param>
    /// <returns>A new processor instance.</returns>
    public ServiceBusProcessor CreateProcessor(
        string topicName,
        string subscriptionName,
        ServiceBusProcessorOptions options)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        return _connection.CreateProcessor(topicName, subscriptionName, options);
    }

    /// <summary>
    /// Creates a new <see cref="ServiceBusReceiver"/> for the specified queue. The returned
    /// receiver is fresh on every call (not cached); the caller owns its lifetime and must
    /// dispose it when finished.
    /// </summary>
    /// <param name="queueName">The queue to receive from.</param>
    /// <returns>A new receiver instance.</returns>
    public ServiceBusReceiver CreateReceiver(string queueName)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        return _connection.CreateReceiver(queueName);
    }

    /// <summary>
    /// Creates a new <see cref="ServiceBusSessionProcessor"/> for consuming session-bound messages
    /// from a queue.
    /// </summary>
    /// <param name="queueName">The queue to consume from.</param>
    /// <param name="options">The session processor options controlling concurrency, prefetch, and
    /// session-lock renewal.</param>
    /// <returns>A new session processor instance.</returns>
    public ServiceBusSessionProcessor CreateSessionProcessor(
        string queueName,
        ServiceBusSessionProcessorOptions options)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        return _connection.CreateSessionProcessor(queueName, options);
    }

    /// <summary>
    /// Provisions a queue using the administration client.
    /// </summary>
    public Task CreateQueueAsync(CreateQueueOptions options, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        return _connection.CreateQueueAsync(options, cancellationToken);
    }

    /// <summary>
    /// Provisions a topic using the administration client.
    /// </summary>
    public Task CreateTopicAsync(CreateTopicOptions options, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        return _connection.CreateTopicAsync(options, cancellationToken);
    }

    /// <summary>
    /// Provisions a subscription using the administration client.
    /// </summary>
    public Task CreateSubscriptionAsync(CreateSubscriptionOptions options, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        return _connection.CreateSubscriptionAsync(options, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        foreach (var sender in _senders.Values)
        {
            await sender.DisposeAsync();
        }

        _senders.Clear();

        await _connection.DisposeAsync();
    }
}
