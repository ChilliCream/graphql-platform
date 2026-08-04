using System.Collections.Immutable;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

/// <summary>
/// Describes the planning result for an incremental plan.
/// </summary>
/// <param name="Steps">
/// The final plan steps for the incremental plan.
/// </param>
/// <param name="InternalOperationDefinition">
/// The <see cref="OperationDefinitionNode"/> for the incremental plan, or
/// <c>null</c> when planning produced no operation.
/// </param>
internal readonly record struct DeferIncrementalPlanResult(
    ImmutableList<PlanStep> Steps,
    OperationDefinitionNode? InternalOperationDefinition);
