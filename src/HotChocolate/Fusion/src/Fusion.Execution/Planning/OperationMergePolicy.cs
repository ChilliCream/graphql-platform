namespace HotChocolate.Fusion.Planning;

/// <summary>
/// Controls whether structurally identical lookups merge across target paths and dependency depths.
/// </summary>
public enum OperationMergePolicy
{
    /// <summary>
    /// Keeps structurally identical lookups as separate operations.
    /// </summary>
    None = 0,

    /// <summary>
    /// Merges structurally identical lookups at the same dependency depth.
    /// </summary>
    Conservative = 1,

    /// <summary>
    /// Merges structurally identical lookups whose dependency depths differ by at most one level.
    /// </summary>
    Balanced = 2,

    /// <summary>
    /// Merges structurally identical lookups regardless of their dependency depths.
    /// </summary>
    Aggressive = 3
}
