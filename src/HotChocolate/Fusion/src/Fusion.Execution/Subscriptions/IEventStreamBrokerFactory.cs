namespace HotChocolate.Fusion.Subscriptions;

/// <summary>
/// Creates per-subscription broker sessions by broker label.
/// </summary>
public interface IEventStreamBrokerFactory
{
    /// <summary>
    /// Creates a broker session for the specified broker label.
    /// </summary>
    /// <param name="broker">
    /// The broker label from the execution schema, or <c>null</c> to use the default broker.
    /// </param>
    /// <returns>
    /// A broker session for a single subscription. The caller owns the returned session and must
    /// dispose it when the subscription ends.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// No broker provider is registered for the specified label.
    /// </exception>
    IEventStreamBroker Create(string? broker);
}
