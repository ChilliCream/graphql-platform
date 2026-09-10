namespace HotChocolate.Fusion.Subscriptions;

/// <summary>
/// Publishes events into the in-memory broker hub.
/// </summary>
public interface IInMemoryEventStreamPublisher
{
    /// <summary>
    /// Publishes an event and takes ownership of <paramref name="message"/>.
    /// </summary>
    ValueTask PublishAsync(
        string topic,
        EventMessage message,
        CancellationToken cancellationToken = default);
}
