using System.Collections.Concurrent;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;

namespace Mocha.Transport.AzureServiceBus;

/// <summary>
/// Manages the single <see cref="ServiceBusClient"/> (one AMQP connection) and caches
/// <see cref="ServiceBusSender"/> instances per entity path. Also provides access to the
/// <see cref="ServiceBusAdministrationClient"/> for topology provisioning.
/// </summary>
public sealed class AzureServiceBusClientManager : IAsyncDisposable
{
    private readonly ServiceBusClient _client;
    private readonly ServiceBusAdministrationClient? _adminClient;
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
            RetryOptions = configuration.RetryOptions ?? new ServiceBusRetryOptions
            {
                MaxRetries = 3,
                Mode = ServiceBusRetryMode.Exponential,
                Delay = TimeSpan.FromSeconds(0.8),
                MaxDelay = TimeSpan.FromMinutes(1)
            }
        };

        if (configuration.ConnectionString is not null)
        {
            _client = new ServiceBusClient(configuration.ConnectionString, clientOptions);
            _adminClient = new ServiceBusAdministrationClient(configuration.ConnectionString);
        }
        else if (configuration.FullyQualifiedNamespace is not null
                 && configuration.Credential is not null)
        {
            _client = new ServiceBusClient(
                configuration.FullyQualifiedNamespace,
                configuration.Credential,
                clientOptions);
            _adminClient = new ServiceBusAdministrationClient(
                configuration.FullyQualifiedNamespace,
                configuration.Credential);
        }
        else
        {
            throw new InvalidOperationException(
                "Either ConnectionString or FullyQualifiedNamespace + Credential must be provided");
        }
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

        return _senders.GetOrAdd(entityPath, path => _client.CreateSender(path));
    }

    /// <summary>
    /// Creates a new <see cref="ServiceBusProcessor"/> for consuming messages from a queue.
    /// </summary>
    /// <param name="queueName">The queue to consume from.</param>
    /// <param name="options">The processor options controlling concurrency and prefetch.</param>
    /// <returns>A new processor instance.</returns>
    public ServiceBusProcessor CreateProcessor(
        string queueName,
        ServiceBusProcessorOptions options)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        return _client.CreateProcessor(queueName, options);
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
        return _client.CreateProcessor(topicName, subscriptionName, options);
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
        return _client.CreateSessionProcessor(queueName, options);
    }

    /// <summary>
    /// Gets the administration client used for topology provisioning, or <c>null</c> if unavailable.
    /// </summary>
    public ServiceBusAdministrationClient? AdminClient => _adminClient;

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

        await _client.DisposeAsync();
    }
}
