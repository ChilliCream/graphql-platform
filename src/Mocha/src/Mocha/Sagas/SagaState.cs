namespace Mocha.Sagas;

/// <summary>
/// Represents a state within a saga state machine, including its transitions, lifecycle actions, and whether it is an initial or final state.
/// </summary>
public sealed class SagaState(
    string state,
    bool isInitial,
    bool isFinal,
    SagaLifeCycle? onEntry,
    SagaResponse? response,
    IEnumerable<SagaTransition> transitions)
{
    /// <summary>
    /// Gets the name of this state.
    /// </summary>
    public string State => state;

    /// <summary>
    /// Gets a value indicating whether this is an initial state where new saga instances are created.
    /// </summary>
    public bool IsInitial => isInitial;

    /// <summary>
    /// Gets a value indicating whether this is a final state that completes the saga.
    /// </summary>
    public bool IsFinal => isFinal;

    /// <summary>
    /// Gets the response to send when the saga enters this state, or <c>null</c> if no response is configured.
    /// </summary>
    public SagaResponse? Response => response;

    /// <summary>
    /// Gets the lifecycle actions (publish/send) to execute when the saga enters this state, or <c>null</c> if none.
    /// </summary>
    public SagaLifeCycle? OnEntry => onEntry;

    /// <summary>
    /// Gets the transitions from this state, indexed by the event type that triggers each transition.
    /// </summary>
    public Dictionary<Type, SagaTransition> Transitions { get; } = transitions.ToDictionary(x => x.EventType, x => x);
}
