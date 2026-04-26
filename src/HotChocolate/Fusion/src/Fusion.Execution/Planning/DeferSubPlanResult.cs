using System.Collections.Immutable;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

/// <summary>
/// The outcome of planning a single deferred sub-plan. Carries the sub-plan's
/// own steps and internal operation. Plan-scope requirements are NOT part of
/// this result: under the variable-wiring model they live on the owning
/// <see cref="IncrementalPlanDescriptor"/> and are populated by the parent
/// plan's requirement-resolution pass, not by the sub-plan itself.
/// </summary>
/// <param name="Steps">
/// The sub-plan's final list of plan steps.
/// </param>
/// <param name="InternalOperationDefinition">
/// The sub-plan's internal <see cref="OperationDefinitionNode"/> produced by
/// the planner, or the descriptor's original operation when no planning
/// occurred. Used downstream for result mapping and execution node
/// construction.
/// </param>
internal readonly record struct DeferSubPlanResult(
    ImmutableList<PlanStep> Steps,
    OperationDefinitionNode? InternalOperationDefinition);
