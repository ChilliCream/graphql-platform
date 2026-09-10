using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Tui.Board;

namespace ChilliCream.Nitro.CommandLine.Tui.Graph;

/// <summary>
/// A task represented in the workspace graph. A collapsed epic retains the
/// number of task nodes it represents in <see cref="HiddenChildCount"/>.
/// </summary>
internal sealed record GraphNode
{
    public required string Id { get; init; }

    public required string Title { get; init; }

    /// <summary>
    /// The task's raw status. Drives the status glyph character
    /// (<see cref="ChilliCream.Nitro.CommandLine.Tui.Widgets.TaskGlyphs.Status"/>)
    /// and terminal dimming; not the border/glyph color, see
    /// <see cref="BoardStatus"/>.
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// The board column status <see cref="TaskBoardStatus.Resolve"/> resolved
    /// for this task at load time: blocked, deferred, ready, in_progress, or
    /// closed. Drives the node's border and glyph color so the graph speaks
    /// the same status color language as the Board, even though its raw
    /// <see cref="Status"/> may just say "open".
    /// </summary>
    public required string BoardStatus { get; init; }

    public required string Type { get; init; }

    public required int Priority { get; init; }

    public string? Assignee { get; init; }

    public IReadOnlyList<string> Labels { get; init; } = [];

    public int HiddenChildCount { get; init; }

    public bool IsEpic => Type == TaskTypes.Epic;

    public static GraphNode FromTask(TaskItem task, IReadOnlyList<string> labels, string boardStatus)
        => new()
        {
            Id = task.Id,
            Title = task.Title,
            Status = task.Status,
            BoardStatus = boardStatus,
            Type = task.Type,
            Priority = task.Priority,
            Assignee = task.Assignee,
            Labels = labels.Order(StringComparer.Ordinal).ToArray()
        };
}
