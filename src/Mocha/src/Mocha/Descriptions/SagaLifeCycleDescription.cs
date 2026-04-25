namespace Mocha;

/// <summary>
/// Describes the lifecycle actions (publish and send) triggered on entry to a saga state.
/// </summary>
/// <param name="Publish">Events published on state entry, or <c>null</c> if none.</param>
/// <param name="Send">Commands sent on state entry, or <c>null</c> if none.</param>
internal sealed record SagaLifeCycleDescription(
    IReadOnlyList<SagaEventDescription>? Publish,
    IReadOnlyList<SagaEventDescription>? Send);
