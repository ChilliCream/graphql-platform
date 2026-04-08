using System.Collections.Immutable;

namespace HotChocolate.Fusion.Planning;

/// <summary>
/// Tracks the minimum guaranteed cost of all work items still in the backlog.
/// </summary>
/// <param name="MinimumCost">
/// Sum of the cheapest possible cost for each backlog item.
/// </param>
/// <param name="MaxProjectedDepth">
/// The deepest level at which backlog items are expected to produce operations.
/// </param>
/// <param name="ProjectedOpsPerLevel">
/// How many operations each depth level is expected to add.
/// </param>
internal readonly record struct BacklogCost(
    double MinimumCost,
    int MaxProjectedDepth,
    ImmutableDictionary<int, int> ProjectedOpsPerLevel)
{
    /// <summary>
    /// Gets an empty backlog cost state with no projected operations.
    /// </summary>
    public static BacklogCost Empty { get; } =
        new(0.0, 0, ImmutableDictionary<int, int>.Empty);
}
