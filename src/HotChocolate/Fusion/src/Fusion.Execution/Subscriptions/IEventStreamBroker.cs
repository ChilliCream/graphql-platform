namespace HotChocolate.Fusion.Subscriptions;

/// <summary>
/// Represents a broker connection that can consume event-stream messages for subscription fields.
/// </summary>
public interface IEventStreamBroker : IAsyncDisposable
{
    /// <summary>
    /// Subscribes to the specified topics for a subscription field.
    /// </summary>
    /// <remarks>
    /// The broker owns fan-in across the topic list and exposes it as one stream.
    /// The caller owns each yielded <see cref="EventMessage"/> and must dispose it after consuming it.
    /// </remarks>
    IAsyncEnumerable<EventMessage> SubscribeAsync(
        ISubscriptionFieldContext context,
        string[] topics,
        CancellationToken cancellationToken);
}
