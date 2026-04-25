namespace Mocha;

/// <summary>
/// Describes a single state within a saga for diagnostic and visualization purposes.
/// </summary>
/// <param name="Name">The state name.</param>
/// <param name="IsInitial">Whether this is the initial state of the saga.</param>
/// <param name="IsFinal">Whether this is a final (terminal) state.</param>
/// <param name="OnEntry">Lifecycle actions on state entry, or <c>null</c> if none.</param>
/// <param name="Response">The response sent from this state, or <c>null</c> if none.</param>
/// <param name="Transitions">The transitions available from this state.</param>
internal sealed record SagaStateDescription(
    string Name,
    bool IsInitial,
    bool IsFinal,
    SagaLifeCycleDescription? OnEntry,
    SagaResponseDescription? Response,
    IReadOnlyList<SagaTransitionDescription> Transitions);
