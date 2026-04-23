using System.Collections.Immutable;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

/// <summary>
/// Describes an enclosing plan scope of a deferred sub-plan at planning time.
/// The parent scope is either the root operation (a top-level defer) or
/// another deferred sub-plan (a nested defer). The <see cref="OwnerDescriptor"/>
/// identifies the enclosing defer (when applicable) and lets the requirement
/// resolution walker escalate requirements that the immediate enclosing scope
/// cannot serve via <see cref="DeferContextGraph.GetEnclosingScope"/>.
/// </summary>
/// <param name="ParentSteps">
/// The parent plan scope's current list of <see cref="PlanStep"/> instances.
/// Routing a deferred sub-plan's requirement into the parent scope produces
/// an updated list that the defer planning pass pushes back via
/// <see cref="DeferContextGraph.UpdateRootSteps(ImmutableList{PlanStep})"/>
/// or <see cref="DeferContextGraph.UpdateDeferContext(DeferSubPlanDescriptor, ImmutableList{PlanStep}, OperationDefinitionNode)"/>.
/// </param>
/// <param name="ParentSelectionSetIndex">
/// The <see cref="ISelectionSetIndex"/> associated with the parent plan scope's
/// internal operation. Used when inlining selections into a parent step.
/// </param>
/// <param name="ParentInternalOperation">
/// The planner's internal <see cref="OperationDefinitionNode"/> for the parent
/// plan scope. For <see cref="ParentScope.Root"/> this is the stripped main
/// operation, for <see cref="ParentScope.EnclosingDefer"/> this is the
/// enclosing sub-plan's internal operation.
/// </param>
/// <param name="Kind">
/// The kind of parent scope, distinguishing the root plan from an enclosing
/// deferred sub-plan.
/// </param>
/// <param name="OwnerDescriptor">
/// When <see cref="Kind"/> is <see cref="ParentScope.EnclosingDefer"/>, the
/// descriptor that owns this scope. <see langword="null"/> for the root scope.
/// The requirement-resolution walker uses this to escalate to the next
/// enclosing scope via <see cref="DeferContextGraph.GetEnclosingScope"/>.
/// </param>
internal sealed record DeferParentContext(
    ImmutableList<PlanStep> ParentSteps,
    ISelectionSetIndex ParentSelectionSetIndex,
    OperationDefinitionNode ParentInternalOperation,
    ParentScope Kind,
    DeferSubPlanDescriptor? OwnerDescriptor = null);

/// <summary>
/// Identifies the kind of plan scope that encloses a deferred sub-plan.
/// </summary>
internal enum ParentScope
{
    /// <summary>
    /// The enclosing scope is the root operation plan.
    /// </summary>
    Root,

    /// <summary>
    /// The enclosing scope is another deferred sub-plan (nested defer).
    /// </summary>
    EnclosingDefer
}
