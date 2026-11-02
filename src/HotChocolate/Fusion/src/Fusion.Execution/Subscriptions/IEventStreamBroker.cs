namespace HotChocolate.Fusion.Subscriptions;

/// <summary>
/// Represents a broker connection that can consume event-stream messages for subscription fields.
/// </summary>
public interface IEventStreamBroker : IAsyncDisposable
{
    /// <summary>
    /// Subscribes to the specified topics for a subscription field.
    /// </summary>
    /// <param name="context">
    /// The subscription field context.
    /// </param>
    /// <param name="topics">
    /// The topics to consume as a single event stream.
    /// </param>
    /// <param name="cursor">
    /// The opaque base64 resume cursor supplied by the client, or <c>null</c> or empty to consume
    /// from the stream's default position.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <remarks>
    /// The broker owns fan-in across the topic list and exposes it as one stream.
    /// Brokers may ignore or reject the resume cursor when resumption is not supported.
    /// The caller owns each yielded <see cref="EventMessage"/> and must dispose it after consuming it.
    /// </remarks>
    IAsyncEnumerable<EventMessage> SubscribeAsync(
        ISubscriptionFieldContext context,
        string[] topics,
        string? cursor,
        CancellationToken cancellationToken);
}
