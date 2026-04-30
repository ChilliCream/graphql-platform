namespace HotChocolate.Fusion.Planning;

/// <summary>
/// Identifies the owner of a resolved field requirement.
/// </summary>
internal abstract record RequirementConsumer;

/// <summary>
/// A requirement owned by a <see cref="PlanStep"/> in the current plan scope.
/// </summary>
internal sealed record StepConsumer(int StepId) : RequirementConsumer;

/// <summary>
/// A requirement owned by an <see cref="IncrementalPlanDescriptor"/> and
/// satisfied by the enclosing plan scope.
/// </summary>
internal sealed record DeferConsumer(IncrementalPlanDescriptor Descriptor) : RequirementConsumer;
