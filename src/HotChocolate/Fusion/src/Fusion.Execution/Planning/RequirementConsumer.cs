namespace HotChocolate.Fusion.Planning;

/// <summary>
/// Identifies the entity that owns a resolved field requirement. Regular
/// plan steps own their own requirements today, and deferred sub-plans
/// express plan-scope requirements against the parent plan through the
/// <see cref="DeferConsumer"/> variant. The planner's field-requirement
/// machinery pattern-matches on this discriminator to route resolved
/// <see cref="Execution.Nodes.OperationRequirement"/> entries into either
/// a step's Requirements dictionary or a descriptor's requirements builder.
/// </summary>
internal abstract record RequirementConsumer;

/// <summary>
/// A requirement consumer that lives on a concrete <see cref="PlanStep"/>
/// in the current plan scope. The resolved requirement writes to the step
/// identified by <paramref name="StepId"/>.
/// </summary>
internal sealed record StepConsumer(int StepId) : RequirementConsumer;

/// <summary>
/// A requirement consumer that lives on a <see cref="DeferSubPlanDescriptor"/>.
/// The resolved requirement becomes a plan-scope requirement on the descriptor's
/// <see cref="DeferSubPlanDescriptor.Requirements"/> collection so the runtime
/// can source the value from the parent plan's result tree before the
/// sub-plan executes.
/// </summary>
internal sealed record DeferConsumer(DeferSubPlanDescriptor Descriptor) : RequirementConsumer;
