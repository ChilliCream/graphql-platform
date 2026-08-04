namespace HotChocolate.Fusion.Planning;

/// <summary>
/// Controls how aggressively structurally-identical operations are merged
/// to reduce the number of downstream requests.
/// </summary>
public enum OperationMergePolicy
{
    /// <summary>
    /// Merge only when canonical signature matches and the operations share the
    /// same dependency depth. This avoids any risk of over-serialization by
    /// ensuring merged operations were already at equivalent execution levels.
    /// </summary>
    Conservative = 0,

    /// <summary>
    /// Merge when canonical signature matches and cycle-safe, but reject merges
    /// where the depth difference between candidates exceeds a single level.
    /// This provides a middle ground between request reduction and serialization risk.
    /// </summary>
    Balanced = 1,

    /// <summary>
    /// Merge whenever canonical signature matches and cycle-safe, regardless of
    /// depth or dependency differences. This maximizes request-count reduction.
    /// </summary>
    Aggressive = 2
}
