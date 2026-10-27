namespace ChilliCream.Nitro.CommandLine.Services.Tasks;

/// <summary>
/// The outcome of <see cref="ITaskStore.UpdateTaskAsync"/>.
/// </summary>
internal sealed record TaskUpdateResult
{
    /// <summary>
    /// Changed column names excluding status, priority, assignee, and updated_at.
    /// Status, priority, and assignee changes produce their own audit events.
    /// </summary>
    public IReadOnlyList<string> ChangedFields { get; init; } = [];
}
