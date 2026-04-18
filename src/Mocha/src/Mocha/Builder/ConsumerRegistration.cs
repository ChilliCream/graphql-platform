namespace Mocha;

/// <summary>
/// Captures a deferred consumer registration with its factory and stacked configuration.
/// </summary>
internal sealed class ConsumerRegistration
{
    /// <summary>
    /// The handler type that identifies this registration. Used for linear scan lookups.
    /// </summary>
    public required Type HandlerType { get; init; }

    /// <summary>
    /// The composed consumer descriptor configuration action.
    /// Multiple AddHandler calls stack onto this via closure composition.
    /// Null when no configuration has been applied.
    /// </summary>
    public Action<IConsumerDescriptor>? Configure { get; set; }

    /// <summary>
    /// Factory that creates the consumer instance.
    /// First registration wins - this is never replaced after initial creation.
    /// </summary>
    public required Func<Action<IConsumerDescriptor>?, Consumer> Factory { get; init; }
}
