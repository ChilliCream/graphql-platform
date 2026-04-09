using System.Collections.Concurrent;
using Azure.Messaging.EventHubs.Producer;
using Microsoft.Extensions.Logging;

namespace Mocha.Transport.AzureEventHub;

/// <summary>
/// Manages singleton <see cref="EventHubProducerClient"/> instances per hub name.
/// Producers are thread-safe and long-lived.
/// </summary>
public sealed class EventHubConnectionManager : IAsyncDisposable
{
    private readonly ILogger<EventHubConnectionManager> _logger;
    private readonly IEventHubConnectionProvider _connectionProvider;
    private readonly ConcurrentDictionary<string, EventHubProducerClient> _producers = new();

    /// <summary>
    /// Creates a new connection manager for the specified connection provider.
    /// </summary>
    /// <param name="logger">Logger for connection lifecycle events.</param>
    /// <param name="connectionProvider">The connection provider for creating producer clients.</param>
    public EventHubConnectionManager(
        ILogger<EventHubConnectionManager> logger,
        IEventHubConnectionProvider connectionProvider)
    {
        _logger = logger;
        _connectionProvider = connectionProvider;
    }

    /// <summary>
    /// Gets the underlying connection provider.
    /// </summary>
    public IEventHubConnectionProvider ConnectionProvider => _connectionProvider;

    /// <summary>
    /// Gets or creates a producer client for the specified Event Hub name.
    /// </summary>
    /// <param name="eventHubName">The name of the Event Hub entity.</param>
    /// <returns>A thread-safe, singleton <see cref="EventHubProducerClient"/>.</returns>
    public EventHubProducerClient GetOrCreateProducer(string eventHubName)
    {
        return _producers.GetOrAdd(eventHubName, static (name, state) =>
        {
            state._logger.CreatingProducerForHub(name);
            return state._connectionProvider.CreateProducer(name);
        }, this);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Callers must ensure all dispatching has stopped before disposing.
    /// Producers that are mid-send when <see cref="DisposeAsync"/> is called may throw
    /// <see cref="ObjectDisposedException"/>.
    /// </remarks>
    public async ValueTask DisposeAsync()
    {
        foreach (var (_, producer) in _producers)
        {
            await producer.DisposeAsync();
        }

        _producers.Clear();
    }
}

internal static partial class EventHubConnectionManagerLogMessages
{
    [LoggerMessage(LogLevel.Debug, "Creating producer client for Event Hub '{EventHubName}'")]
    public static partial void CreatingProducerForHub(this ILogger logger, string eventHubName);
}
