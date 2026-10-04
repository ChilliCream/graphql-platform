using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Services.Tasks;

/// <summary>
/// A task's full details, including its labels, dependencies, dependents,
/// comments, and computed blockers, as returned by the structured (JSON)
/// output of <c>task show</c>.
/// </summary>
internal sealed record TaskDetailResult
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
    public required IReadOnlyList<string> Labels { get; init; }
    public required IReadOnlyList<string> Blockers { get; init; }
    public required IReadOnlyList<TaskDependencyDetail> Dependencies { get; init; }
    public required IReadOnlyList<TaskDependentDetail> Dependents { get; init; }
    public required IReadOnlyList<TaskComment> Comments { get; init; }
    public required IReadOnlyList<TakeoverReferenceResult> Takeovers { get; init; }
}
