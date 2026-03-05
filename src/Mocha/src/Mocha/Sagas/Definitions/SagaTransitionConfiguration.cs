using Mocha;

namespace Mocha.Sagas;

/// <summary>
/// Configuration for a state transition within a saga state machine, including the triggering event,
/// target state, side-effect messages, and the transition action.
/// </summary>
public class SagaTransitionConfiguration : MessagingConfiguration
{
    /// <summary>
    /// Gets or sets the CLR type of the event that triggers this transition.
    /// </summary>
    public Type? EventType { get; set; }

    /// <summary>
    /// Gets or sets the kind of transition (event, send, request, or reply).
    /// </summary>
    public SagaTransitionKind? TransitionKind { get; set; }

    /// <summary>
    /// Gets or sets the name of the target state to transition to.
    /// </summary>
    public string? TransitionTo { get; set; }

    /// <summary>
    /// Gets the list of messages to publish as side effects of this transition.
    /// </summary>
    public List<SagaEventPublishConfiguration> Publish { get; } = [];

    /// <summary>
    /// Gets the list of messages to send as side effects of this transition.
    /// </summary>
    public List<SagaEventSendConfiguration> Send { get; } = [];

    /// <summary>
    /// Gets or sets the action to execute on the saga state when this transition triggers.
    /// </summary>
    public Action<object, object>? Action { get; set; }

    /// <summary>
    /// Gets or sets a factory that creates new saga state instances for transitions from the initial state.
    /// </summary>
    public Func<object, SagaStateBase>? StateFactory { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the messaging infrastructure for this transition is automatically provisioned.
    /// </summary>
    public bool AutoProvision { get; set; } = true;
}
