using Azure.Messaging.EventHubs.Producer;
using Mocha.Features;

namespace Mocha.Transport.AzureEventHub.Features;

/// <summary>
/// Pooled feature that carries dispatch-specific state through the dispatch middleware pipeline.
/// </summary>
public sealed class EventHubDispatchFeature : IPooledFeature
{
    /// <summary>
    /// Gets or sets the send options for the current dispatch operation, including partition key.
    /// </summary>
    public SendEventOptions? SendOptions { get; set; }

    /// <inheritdoc />
    public void Initialize(object state) => SendOptions = null;

    /// <inheritdoc />
    public void Reset() => SendOptions = null;
}
