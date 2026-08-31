namespace ChilliCream.Nitro.CommandLine.Services.Tasks;

/// <summary>
/// A task row. Null <see cref="Assignee"/> means unassigned; null date values
/// mean the corresponding lifecycle step has not happened.
/// </summary>
internal sealed class TaskItem
{
    /// <summary>
    /// The column list matching this type's properties, for use in SELECT
    /// statements against the tasks table.
    /// </summary>
    public const string Columns =
        "id AS Id, title AS Title, description AS Description, design AS Design, "
        + "acceptance_criteria AS AcceptanceCriteria, notes AS Notes, status AS Status, "
        + "priority AS Priority, task_type AS Type, assignee AS Assignee, "
        + "estimated_minutes AS EstimatedMinutes, due_at AS DueAt, defer_until AS DeferUntil, "
        + "created_at AS CreatedAt, created_by AS CreatedBy, updated_at AS UpdatedAt, "
        + "closed_at AS ClosedAt, close_reason AS CloseReason, deleted_at AS DeletedAt, "
        + "delete_reason AS DeleteReason";

    public required string Id { get; init; }
    public required string Title { get; set; }
    public string Description { get; set; } = "";
    public string Design { get; set; } = "";
    public string AcceptanceCriteria { get; set; } = "";
    public string Notes { get; set; } = "";
    public string Status { get; set; } = TaskStates.Open;
    public int Priority { get; set; } = TaskPriorities.Medium;
    public string Type { get; set; } = TaskTypes.Task;
    public string? Assignee { get; set; }
    public int? EstimatedMinutes { get; set; }
    public DateTimeOffset? DueAt { get; set; }
    public DateTimeOffset? DeferUntil { get; set; }
    public required DateTimeOffset CreatedAt { get; init; }
    public string CreatedBy { get; init; } = "";
    public required DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
    public string CloseReason { get; set; } = "";
    public DateTimeOffset? DeletedAt { get; set; }
    public string DeleteReason { get; set; } = "";
}
