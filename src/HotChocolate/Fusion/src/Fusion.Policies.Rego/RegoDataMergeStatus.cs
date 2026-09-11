namespace HotChocolate.Fusion.Policies.Rego;

/// <summary>
/// The outcome of <see cref="RegoDataAggregator.TryBuildMergedData"/>.
/// </summary>
internal enum RegoDataMergeStatus
{
    /// <summary>
    /// At least one registered provider has not completed its initial data load yet.
    /// </summary>
    NotReady,

    /// <summary>
    /// Every provider has data, but merging it with the FAR data document failed.
    /// </summary>
    Failed,

    /// <summary>
    /// Every provider has data and the merge succeeded.
    /// </summary>
    Ready
}
