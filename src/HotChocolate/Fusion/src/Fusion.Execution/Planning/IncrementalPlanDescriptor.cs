using System.Collections.Immutable;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

/// <summary>
/// Describes an incremental plan operation and the delivery group set that keys
/// it. Fields whose effective delivery group set equals
/// <see cref="DeliveryGroupSet"/> belong to this plan.
/// </summary>
internal sealed class IncrementalPlanDescriptor(
    ImmutableArray<DeliveryGroup> deliveryGroupSet,
    OperationDefinitionNode operation,
    SelectionPath path,
    IncrementalPlanDescriptor? parent)
{
    /// <summary>
    /// The <see cref="DeliveryGroup"/> set this incremental plan is keyed by,
    /// sorted ascending by <see cref="DeliveryGroup.Id"/>.
    /// </summary>
    public ImmutableArray<DeliveryGroup> DeliveryGroupSet { get; } = deliveryGroupSet;

    /// <summary>
    /// The operation definition for this incremental plan.
    /// </summary>
    public OperationDefinitionNode Operation { get; internal set; } = operation;

    /// <summary>
    /// The anchor path for this incremental plan.
    /// </summary>
    public SelectionPath Path { get; } = path;

    /// <summary>
    /// The parent incremental plan for nested <c>@defer</c>, or <c>null</c>
    /// for a top-level incremental plan.
    /// </summary>
    public IncrementalPlanDescriptor? Parent { get; } = parent;

    /// <summary>
    /// Requirements this incremental plan resolves from its enclosing plan
    /// scope. Each entry maps a variable used by this plan to a selection in
    /// the enclosing scope's result. Entries are keyed by requirement name.
    /// </summary>
    public SortedDictionary<string, OperationRequirement> Requirements { get; } =
        new(StringComparer.Ordinal);
}
