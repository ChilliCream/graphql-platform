namespace HotChocolate.Fusion.Composition.Pipeline;

/// <summary>
/// Represents the possible status of a merge operation.
/// </summary>
internal enum MergeStatus
{
    /// <summary>
    /// The merge operation was skipped.
    /// </summary>
    Skipped,

    /// <summary>
    /// The merge operation completed successfully.
    /// </summary>
    Completed,
}
