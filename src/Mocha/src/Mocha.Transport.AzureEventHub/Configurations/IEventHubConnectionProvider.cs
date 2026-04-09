using Azure.Core;
using Azure.Messaging.EventHubs.Producer;

namespace Mocha.Transport.AzureEventHub;

/// <summary>
/// Provides connection details and the ability to create Event Hub producer clients.
/// </summary>
public interface IEventHubConnectionProvider
{
    /// <summary>
    /// Gets the fully qualified namespace (e.g., "mynamespace.servicebus.windows.net").
    /// </summary>
    string FullyQualifiedNamespace { get; }

    /// <summary>
    /// Creates an <see cref="EventHubProducerClient"/> for the specified hub.
    /// </summary>
    /// <param name="eventHubName">The name of the Event Hub to create a producer for.</param>
    /// <returns>A new <see cref="EventHubProducerClient"/> instance.</returns>
    EventHubProducerClient CreateProducer(string eventHubName);

    /// <summary>
    /// Gets the connection string for this provider, or <c>null</c> if using token credentials.
    /// Used by <see cref="MochaEventProcessor"/> for connection creation.
    /// </summary>
    string? ConnectionString { get; }

    /// <summary>
    /// Gets the token credential for this provider, or <c>null</c> if using a connection string.
    /// </summary>
    TokenCredential? Credential { get; }
}
