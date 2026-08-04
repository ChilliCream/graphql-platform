namespace HotChocolate.Fusion.Subscriptions;

/// <summary>
/// Provides broker sessions for one registered broker label.
/// </summary>
public interface IEventStreamBrokerProvider
{
    /// <summary>
    /// Creates a broker session for the provider's registered broker label.
    /// </summary>
    /// <returns>
    /// A broker session for a single subscription. The caller owns the returned session and must
    /// dispose it when the subscription ends.
    /// </returns>
    IEventStreamBroker Create();
}
