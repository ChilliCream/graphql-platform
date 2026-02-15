using System.Collections.Immutable;

namespace HotChocolate.Fusion.Planning;

/// <summary>
/// Incremental backlog lower-bound state used to project remaining work shape.
/// </summary>
/// <param name="OperationLowerBound">
/// Guaranteed remaining operation floor from all backlog items.
/// </param>
/// <param name="MaxProjectedDepth">
/// Deepest projected operation depth across remaining guaranteed operations.
/// </param>
/// <param name="ProjectedOpsPerLevel">
/// Projected guaranteed operation count per depth level.
/// </param>
internal readonly record struct BacklogCostState(
    double OperationLowerBound,
    int MaxProjectedDepth,
    ImmutableDictionary<int, int> ProjectedOpsPerLevel)
{
    /// <summary>
    /// Gets an empty backlog cost state with no projected operations.
    /// </summary>
    public static BacklogCostState Empty { get; } =
        new(0.0, 0, ImmutableDictionary<int, int>.Empty);
}
