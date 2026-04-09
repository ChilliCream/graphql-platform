using Azure.Core;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;

namespace Mocha.Transport.AzureEventHub;

/// <summary>
/// Connection-string-based Event Hub connection provider.
/// </summary>
public sealed class ConnectionStringEventHubConnectionProvider : IEventHubConnectionProvider
{
    private readonly string _connectionString;

    /// <summary>
    /// Creates a new connection provider using the specified connection string.
    /// </summary>
    /// <param name="connectionString">
    /// The Event Hubs connection string (e.g., from the Azure portal).
    /// </param>
    public ConnectionStringEventHubConnectionProvider(string connectionString)
    {
        _connectionString = connectionString;

        var props = EventHubsConnectionStringProperties.Parse(connectionString);
        FullyQualifiedNamespace = props.FullyQualifiedNamespace;
    }

    /// <inheritdoc />
    public string FullyQualifiedNamespace { get; }

    /// <inheritdoc />
    public string? ConnectionString => _connectionString;

    /// <inheritdoc />
    public TokenCredential? Credential => null;

    /// <inheritdoc />
    public EventHubProducerClient CreateProducer(string eventHubName)
        => new(_connectionString, eventHubName);
}
