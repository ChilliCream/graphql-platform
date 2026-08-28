namespace ChilliCream.Nitro.CommandLine.Services.Tasks;

/// <summary>
/// The parameters for <see cref="ITaskStore.CreateTaskAsync"/>.
/// </summary>
internal sealed record TaskCreation
{
    public required string Title { get; init; }
    public string Description { get; init; } = "";
    public required int Priority { get; init; }
    public required string Type { get; init; }
    public string? Assignee { get; init; }
    public int? EstimatedMinutes { get; init; }
    public DateTimeOffset? DueAt { get; init; }
    public DateTimeOffset? DeferUntil { get; init; }
    public IReadOnlyList<string> Labels { get; init; } = [];

    /// <summary>
    /// Dependencies to create alongside the task, applied in addition to the
    /// parent-child edge implied by <see cref="ParentId"/>.
    /// </summary>
    public IReadOnlyList<TaskDependencyRequest> DependsOn { get; init; } = [];

    /// <summary>
    /// The parent task to create this task under, or null for a top-level
    /// task. Determines both the parent-child dependency and ID allocation.
    /// </summary>
    public string? ParentId { get; init; }

    public required string Actor { get; init; }
}
