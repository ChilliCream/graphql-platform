using System.Collections.Immutable;

namespace Mocha.Sagas;

/// <summary>
/// Defines a state transition within a saga, triggered by a specific event type, with associated actions, messages, and the target state.
/// </summary>
public sealed class SagaTransition(
    Type eventType,
    string transitionTo,
    SagaTransitionKind transitionKind,
    Action<object, object> action,
    IEnumerable<SagaEventPublish> publish,
    IEnumerable<SagaEventSend> send,
    Func<object, SagaStateBase>? stateFactory,
    bool autoProvision)
{
    /// <summary>
    /// Gets the CLR type of the event that triggers this transition.
    /// </summary>
    public Type EventType { get; } = eventType;

    /// <summary>
    /// Gets the name of the target state after the transition.
    /// </summary>
    public string TransitionTo { get; } = transitionTo;

    /// <summary>
    /// Gets the events to publish as part of this transition.
    /// </summary>
    public ImmutableArray<SagaEventPublish> Publish { get; } = [.. publish];

    /// <summary>
    /// Gets the events to send as part of this transition.
    /// </summary>
    public ImmutableArray<SagaEventSend> Send { get; } = [.. send];

    /// <summary>
    /// Gets the action to execute on the saga state when this transition occurs.
    /// </summary>
    public Action<object, object> Action { get; } = action;

    /// <summary>
    /// Gets the factory that creates new saga state instances, or <c>null</c> if the state is not auto-provisioned.
    /// </summary>
    public Func<object, SagaStateBase>? StateFactory { get; } = stateFactory;

    /// <summary>
    /// Gets a value indicating whether the saga instance should be automatically created when this transition is triggered.
    /// </summary>
    public bool AutoProvision { get; } = autoProvision;

    /// <summary>
    /// Gets the kind of transition (initial, transition, or final).
    /// </summary>
    public SagaTransitionKind TransitionKind { get; } = transitionKind;
}
