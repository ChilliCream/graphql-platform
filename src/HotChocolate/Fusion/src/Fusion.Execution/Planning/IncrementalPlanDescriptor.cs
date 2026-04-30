using System.Collections.Immutable;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

/// <summary>
/// Describes a single incremental plan: the compiled operation for a unique
/// <see cref="DeliveryGroup"/> set together with the set itself (sorted by
/// <see cref="DeliveryGroup.Id"/> for stability). Fields whose active delivery
/// group set equals <see cref="DeliveryGroupSet"/> are fetched by this plan and
/// delivered to every delivery group in the set.
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
    /// The compiled operation for this incremental plan.
    /// </summary>
    public OperationDefinitionNode Operation { get; internal set; } = operation;

    /// <summary>
    /// The path where the incremental plan's data is inserted in the response tree.
    /// Derived from the deepest <see cref="DeliveryGroup.Path"/> in the set.
    /// </summary>
    public SelectionPath Path { get; } = path;

    /// <summary>
    /// The parent incremental plan for nested <c>@defer</c>, or <c>null</c>
    /// for a top-level incremental plan. Determined by walking each set
    /// member's <see cref="DeliveryGroup.Parent"/> chain and finding the first
    /// already-emitted plan whose set contains a matching ancestor.
    /// </summary>
    public IncrementalPlanDescriptor? Parent { get; } = parent;

    /// <summary>
    /// Plan-scope requirements this incremental plan sources from the parent
    /// plan's variable flow. Populated by the parent plan's requirement system
    /// during parent planning so that the parent plan produces every value
    /// the incremental plan needs before it executes. A resolved entry maps
    /// a variable name used inside the incremental plan to a selection in the
    /// parent plan's result tree. Keyed by requirement name to dedup across groups
    /// that share the same requirement.
    /// </summary>
    public SortedDictionary<string, OperationRequirement> Requirements { get; } =
        new(StringComparer.Ordinal);
}
