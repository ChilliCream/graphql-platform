using System.Collections.Immutable;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

/// <summary>
/// Result of <see cref="DeferPartitioner.Partition"/>. Carries the complete
/// <see cref="DeliveryGroup"/> tree (in declaration order) together with the
/// <see cref="InlineFragmentNode"/>-keyed lookup so downstream stages can
/// resolve any <c>... @defer</c> in the same AST to the canonical
/// <see cref="DeliveryGroup"/> instance.
/// </summary>
internal sealed class DeferPartitioningResult(
    ImmutableArray<DeliveryGroup> allDeliveryGroups,
    IReadOnlyDictionary<InlineFragmentNode, DeliveryGroup> byFragment)
{
    /// <summary>
    /// Every <see cref="DeliveryGroup"/> discovered in the operation, in
    /// declaration order (depth-first, left-to-right as encountered).
    /// The array index equals each usage's <see cref="DeliveryGroup.Id"/>.
    /// </summary>
    public ImmutableArray<DeliveryGroup> AllDeliveryGroups { get; } = allDeliveryGroups;

    /// <summary>
    /// Maps each <c>... @defer</c> inline fragment to its canonical
    /// <see cref="DeliveryGroup"/>. Lookup is by reference identity on the AST
    /// node.
    /// </summary>
    public IReadOnlyDictionary<InlineFragmentNode, DeliveryGroup> ByFragment { get; } = byFragment;
}
