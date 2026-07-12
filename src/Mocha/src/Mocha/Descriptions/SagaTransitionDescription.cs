using Mocha.Sagas;

namespace Mocha;

/// <summary>
/// Describes a transition between saga states triggered by a specific event.
/// </summary>
/// <param name="Id">The stable URN identity of this saga transition.</param>
/// <param name="EventType">The short type name of the event that triggers this transition.</param>
/// <param name="EventTypeFullName">The fully qualified type name, or <c>null</c> if unavailable.</param>
/// <param name="TransitionTo">The name of the target state.</param>
/// <param name="TransitionKind">The kind of transition (e.g., move, complete).</param>
/// <param name="Publish">Events published during this transition, or <c>null</c> if none.</param>
/// <param name="Send">Commands sent during this transition, or <c>null</c> if none.</param>
public sealed record SagaTransitionDescription(
    string Id,
    string EventType,
    string? EventTypeFullName,
    string TransitionTo,
    SagaTransitionKind TransitionKind,
    IReadOnlyList<SagaEventDescription>? Publish,
    IReadOnlyList<SagaEventDescription>? Send);
