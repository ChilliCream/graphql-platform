using Mocha.Sagas;

namespace Mocha;

/// <summary>
/// Describes a transition between saga states triggered by a specific event.
/// </summary>
/// <param name="EventType">The short type name of the event that triggers this transition.</param>
/// <param name="EventTypeFullName">The fully qualified type name, or <c>null</c> if unavailable.</param>
/// <param name="TransitionTo">The name of the target state.</param>
/// <param name="TransitionKind">The kind of transition (e.g., move, complete).</param>
/// <param name="AutoProvision">Whether the saga instance is auto-provisioned when this event arrives.</param>
/// <param name="Publish">Events published during this transition, or <c>null</c> if none.</param>
/// <param name="Send">Commands sent during this transition, or <c>null</c> if none.</param>
internal sealed record SagaTransitionDescription(
    string EventType,
    string? EventTypeFullName,
    string TransitionTo,
    SagaTransitionKind TransitionKind,
    bool AutoProvision,
    IReadOnlyList<SagaEventDescription>? Publish,
    IReadOnlyList<SagaEventDescription>? Send);
