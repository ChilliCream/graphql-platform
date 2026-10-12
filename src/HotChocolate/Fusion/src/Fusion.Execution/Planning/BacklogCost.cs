using System.Collections.Immutable;

namespace HotChocolate.Fusion.Planning;

/// <summary>
/// Tracks the operations that the work items still in the backlog are guaranteed to add.
/// </summary>
/// <param name="MinimumOperationCount">
/// The number of operations the backlog items add at the very least.
/// </param>
/// <param name="MaxProjectedDepth">
/// The deepest level at which backlog items are guaranteed to produce operations.
/// </param>
/// <param name="ProjectedOpsPerLevel">
/// How many operations each depth level is guaranteed to receive from the backlog.
/// </param>
internal readonly record struct BacklogCost(
    int MinimumOperationCount,
    int MaxProjectedDepth,
    ImmutableDictionary<int, int> ProjectedOpsPerLevel)
{
    /// <summary>
    /// Gets an empty backlog cost state with no projected operations.
    /// </summary>
    public static BacklogCost Empty { get; } =
#if NET10_0_OR_GREATER
        new(0, 0, []);
#else
        new(0, 0, ImmutableDictionary<int, int>.Empty);
#endif
}
