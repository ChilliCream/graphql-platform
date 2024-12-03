using HotChocolate.Execution.Processing;

namespace HotChocolate.Execution.Instrumentation;

/// <summary>
/// The subscription event context provides access to the subscription instance
/// and the event payload.
/// </summary>
public readonly ref struct SubscriptionEventContext
{
    /// <summary>
    /// Creates a new instance of the subscription event context.
    /// </summary>
    /// <param name="subscription">
    /// The subscription.
    /// </param>
    /// <param name="payload">
    /// The event payload.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="subscription"/> or <paramref name="payload"/> is <c>null</c>.
    /// </exception>
    public SubscriptionEventContext(ISubscription subscription, object payload)
    {
        Subscription = subscription ?? throw new ArgumentNullException(nameof(subscription));
        Payload = payload ?? throw new ArgumentNullException(nameof(payload));
    }

    /// <summary>
    /// Gets the subscription.
    /// </summary>
    public ISubscription Subscription { get; }

    /// <summary>
    /// Gets the event payload.
    /// </summary>
    public object Payload { get; }
}
