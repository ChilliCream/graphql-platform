namespace ChilliCream.Nitro.CommandLine.Services.Tasks;

/// <summary>
/// An audit log entry for a task mutation. Null old or new values mean the
/// event carries no field transition.
/// </summary>
internal sealed class TaskEvent
{
    /// <summary>
    /// The column list matching this type's properties, for use in SELECT
    /// statements against the events table.
    /// </summary>
    public const string Columns =
        "id AS Id, task_id AS TaskId, event_type AS Type, actor AS Actor, "
        + "old_value AS OldValue, new_value AS NewValue, comment AS Comment, "
        + "created_at AS CreatedAt";

    public long Id { get; init; }
    public required string TaskId { get; init; }
    public required string Type { get; init; }
    public string Actor { get; init; } = "";
    public string? OldValue { get; init; }
    public string? NewValue { get; init; }
    public string? Comment { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}
