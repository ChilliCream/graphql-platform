namespace ChilliCream.Nitro.CommandLine.Services.Tasks;

/// <summary>
/// An epic's direct child completion counts, as returned by
/// <see cref="ITaskStore.GetEpicStatusesAsync"/>. <see cref="Total"/> and
/// <see cref="Closed"/> count only non-tombstone children.
/// </summary>
internal sealed record TaskEpicStatus
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string Status { get; init; }
    public required int Total { get; init; }
    public required int Closed { get; init; }

    /// <summary>
    /// An epic is eligible for close when it has at least one child, all of
    /// its children are closed, and the epic itself is not already closed.
    /// </summary>
    public bool IsEligibleForClose
        => Total > 0 && Closed == Total && Status != TaskStates.Closed;
}
