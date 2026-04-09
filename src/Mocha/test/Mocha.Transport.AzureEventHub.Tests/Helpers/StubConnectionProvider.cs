using Azure.Core;
using Azure.Messaging.EventHubs.Producer;

namespace Mocha.Transport.AzureEventHub.Tests.Helpers;

/// <summary>
/// A stub <see cref="IEventHubConnectionProvider"/> that satisfies initialization requirements
/// for semi-integration tests that build a runtime but never start connections.
/// </summary>
internal sealed class StubConnectionProvider : IEventHubConnectionProvider
{
    public string FullyQualifiedNamespace => "test-namespace.servicebus.windows.net";

    public string? ConnectionString => null;

    public TokenCredential? Credential => null;

    public EventHubProducerClient CreateProducer(string eventHubName)
    {
        throw new NotSupportedException("StubConnectionProvider does not create real producers.");
    }
}
