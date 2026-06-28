using Mocha.Middlewares;

namespace Mocha;

/// <summary>
/// Represents the rule that decides whether an inbound route selects its consumer for a received
/// message. The receive router evaluates the condition against the message envelope metadata, the
/// message type and the headers, both of which are available before the message body is deserialized.
/// </summary>
public abstract class RouteCondition
{
    /// <summary>
    /// Prepares the condition against the messaging configuration so that any message types it
    /// references are resolved and registered before the route starts evaluating received messages.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    public virtual void Initialize(IMessagingConfigurationContext context)
    {
    }

    /// <summary>
    /// Determines whether the route should select its consumer for the given received message.
    /// </summary>
    /// <param name="context">The receive context exposing the resolved message type and the headers.</param>
    /// <returns><c>true</c> if the route matches the message; otherwise, <c>false</c>.</returns>
    public abstract bool Matches(IReceiveContext context);

    /// <summary>
    /// Creates a structured description of this condition for visualization and diagnostic purposes.
    /// </summary>
    /// <returns>A <see cref="RouteConditionDescription"/> representing this condition.</returns>
    public abstract RouteConditionDescription Describe();
}
