namespace ChilliCream.Nitro.CommandLine.Services.Tasks;

/// <summary>
/// The sort order applied by <see cref="ITaskStore.QueryTasksAsync"/>.
/// </summary>
internal enum TaskOrdering
{
    /// <summary>
    /// Priority ascending, then created_at ascending, then id ascending. The
    /// default for list and search.
    /// </summary>
    PriorityCreatedId,

    /// <summary>
    /// updated_at ascending, then id ascending. Used by stale.
    /// </summary>
    UpdatedAtAscending,

    /// <summary>
    /// Priority 0 or 1 first, then created_at ascending, then id ascending.
    /// Used by ready.
    /// </summary>
    ReadyPick
}
