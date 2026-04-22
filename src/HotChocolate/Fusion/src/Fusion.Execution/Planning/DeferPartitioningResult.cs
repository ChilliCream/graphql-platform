using System.Collections.Immutable;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

/// <summary>
/// Result of <see cref="DeferPartitioner.Partition"/>. Carries the complete
/// <see cref="DeferUsage"/> tree (in declaration order) together with the
/// <see cref="InlineFragmentNode"/>-keyed lookup so downstream stages can
/// resolve any <c>... @defer</c> in the same AST to the canonical
/// <see cref="DeferUsage"/> instance.
/// </summary>
internal sealed class DeferPartitioningResult(
    ImmutableArray<DeferUsage> allDeferUsages,
    IReadOnlyDictionary<InlineFragmentNode, DeferUsage> byFragment)
{
    /// <summary>
    /// Every <see cref="DeferUsage"/> discovered in the operation, in
    /// declaration order (depth-first, left-to-right as encountered).
    /// The array index equals each usage's <see cref="DeferUsage.Id"/>.
    /// </summary>
    public ImmutableArray<DeferUsage> AllDeferUsages { get; } = allDeferUsages;

    /// <summary>
    /// Maps each <c>... @defer</c> inline fragment to its canonical
    /// <see cref="DeferUsage"/>. Lookup is by reference identity on the AST
    /// node.
    /// </summary>
    public IReadOnlyDictionary<InlineFragmentNode, DeferUsage> ByFragment { get; } = byFragment;
}
