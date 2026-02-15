using System.Collections.Immutable;

namespace HotChocolate.Fusion.Planning;

/// <summary>
/// Incremental backlog lower-bound state used to project remaining work shape.
/// </summary>
internal readonly record struct BacklogCostState(
    double OperationLowerBound,
    int MaxProjectedDepth,
    ImmutableDictionary<int, int> ProjectedOpsPerLevel)
{
    public static BacklogCostState Empty { get; } =
        new(0.0, 0, ImmutableDictionary<int, int>.Empty);
}
