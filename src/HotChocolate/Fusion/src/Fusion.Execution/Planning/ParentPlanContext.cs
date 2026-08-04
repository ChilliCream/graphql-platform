using System.Collections.Immutable;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

/// <summary>
/// Describes the plan scope that encloses an incremental plan.
/// </summary>
/// <param name="ParentSteps">
/// The plan steps available in the enclosing scope.
/// </param>
/// <param name="ParentSelectionSetIndex">
/// The selection set index for the enclosing scope.
/// </param>
/// <param name="ParentInternalOperation">
/// The <see cref="OperationDefinitionNode"/> for the enclosing scope.
/// </param>
/// <param name="Kind">
/// The kind of enclosing scope.
/// </param>
/// <param name="OwnerDescriptor">
/// The descriptor for an enclosing incremental plan, or <see langword="null"/>
/// for the root scope.
/// </param>
internal sealed record ParentPlanContext(
    ImmutableList<PlanStep> ParentSteps,
    ISelectionSetIndex ParentSelectionSetIndex,
    OperationDefinitionNode ParentInternalOperation,
    ParentScope Kind,
    IncrementalPlanDescriptor? OwnerDescriptor = null);

/// <summary>
/// Identifies the kind of plan scope that encloses an incremental plan.
/// </summary>
internal enum ParentScope
{
    /// <summary>
    /// The enclosing scope is the root operation plan.
    /// </summary>
    Root,

    /// <summary>
    /// The enclosing scope is another incremental plan.
    /// </summary>
    EnclosingIncrementalPlan
}
