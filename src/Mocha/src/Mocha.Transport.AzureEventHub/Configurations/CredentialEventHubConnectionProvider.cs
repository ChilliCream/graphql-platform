using Azure.Core;
using Azure.Messaging.EventHubs.Producer;

namespace Mocha.Transport.AzureEventHub;

/// <summary>
/// Azure Identity credential-based Event Hub connection provider.
/// </summary>
public sealed class CredentialEventHubConnectionProvider : IEventHubConnectionProvider
{
    private readonly string _fullyQualifiedNamespace;
    private readonly TokenCredential _credential;

    /// <summary>
    /// Creates a new connection provider using the specified namespace and token credential.
    /// </summary>
    /// <param name="fullyQualifiedNamespace">
    /// The fully qualified Event Hubs namespace (e.g., "mynamespace.servicebus.windows.net").
    /// </param>
    /// <param name="credential">The Azure token credential for authentication.</param>
    public CredentialEventHubConnectionProvider(
        string fullyQualifiedNamespace,
        TokenCredential credential)
    {
        _fullyQualifiedNamespace = fullyQualifiedNamespace;
        _credential = credential;
    }

    /// <inheritdoc />
    public string FullyQualifiedNamespace => _fullyQualifiedNamespace;

    /// <inheritdoc />
    public string? ConnectionString => null;

    /// <inheritdoc />
    public TokenCredential? Credential => _credential;

    /// <inheritdoc />
    public EventHubProducerClient CreateProducer(string eventHubName)
        => new(_fullyQualifiedNamespace, eventHubName, _credential);
}
