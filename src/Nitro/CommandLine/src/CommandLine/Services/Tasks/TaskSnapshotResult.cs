namespace ChilliCream.Nitro.CommandLine.Services.Tasks;

/// <summary>
/// A task's core fields, as returned by the structured (JSON) output of task
/// mutation commands. <see cref="Blockers"/> is populated only where the
/// mutation already computes it; otherwise it is empty.
/// </summary>
internal sealed record TaskSnapshotResult
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string Status { get; init; }
    public required int Priority { get; init; }
    public required string Type { get; init; }
    public string? Assignee { get; init; }
    public int? EstimatedMinutes { get; init; }
    public DateTimeOffset? DueAt { get; init; }
    public DateTimeOffset? DeferUntil { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required string CreatedBy { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
    public DateTimeOffset? ClosedAt { get; init; }
    public string? CloseReason { get; init; }
    public required string Description { get; init; }
    public required string Design { get; init; }
    public required string AcceptanceCriteria { get; init; }
    public required string Notes { get; init; }
    public IReadOnlyList<string> Blockers { get; init; } = [];

    public static TaskSnapshotResult From(TaskItem task, IReadOnlyList<string>? blockers = null) => new()
    {
        Id = task.Id,
        Title = task.Title,
        Status = task.Status,
        Priority = task.Priority,
        Type = task.Type,
        Assignee = task.Assignee,
        EstimatedMinutes = task.EstimatedMinutes,
        DueAt = task.DueAt,
        DeferUntil = task.DeferUntil,
        CreatedAt = task.CreatedAt,
        CreatedBy = task.CreatedBy,
        UpdatedAt = task.UpdatedAt,
        ClosedAt = task.ClosedAt,
        CloseReason = string.IsNullOrEmpty(task.CloseReason) ? null : task.CloseReason,
        Description = task.Description,
        Design = task.Design,
        AcceptanceCriteria = task.AcceptanceCriteria,
        Notes = task.Notes,
        Blockers = blockers ?? []
    };
}
