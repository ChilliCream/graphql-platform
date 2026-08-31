using ChilliCream.Nitro.CommandLine.Services.Tasks;

namespace ChilliCream.Nitro.CommandLine.Tui.Graph;

/// <summary>
/// A task represented in the workspace graph. A collapsed epic retains the
/// number of task nodes it represents in <see cref="HiddenChildCount"/>.
/// </summary>
internal sealed record GraphNode
{
    public required string Id { get; init; }

    public required string Title { get; init; }

    public required string Status { get; init; }

    public required string Type { get; init; }

    public required int Priority { get; init; }

    public string? Assignee { get; init; }

    public IReadOnlyList<string> Labels { get; init; } = [];

    public int HiddenChildCount { get; init; }

    public bool IsEpic => Type == TaskTypes.Epic;

    public static GraphNode FromTask(TaskItem task, IReadOnlyList<string> labels)
        => new()
        {
            Id = task.Id,
            Title = task.Title,
            Status = task.Status,
            Type = task.Type,
            Priority = task.Priority,
            Assignee = task.Assignee,
            Labels = labels.Order(StringComparer.Ordinal).ToArray()
        };
}
