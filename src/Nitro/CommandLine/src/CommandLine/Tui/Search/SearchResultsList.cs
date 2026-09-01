using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Tui.Input;
using ChilliCream.Nitro.CommandLine.Tui.Widgets;
using Spectre.Console.Rendering;
using CursorDirection = ChilliCream.Nitro.CommandLine.Tui.Input.CursorDirection;

namespace ChilliCream.Nitro.CommandLine.Tui.Search;

/// <summary>
/// The live results of a search mode query: the matching tasks, the visible
/// window over them, and which row is selected. Renders each row via
/// <see cref="TaskBadge"/> and a footer line reporting the total result
/// count.
/// </summary>
internal sealed class SearchResultsList
{
    private readonly Viewport _viewport = new(0, 0);

    /// <summary>
    /// The tasks currently loaded, in display order.
    /// </summary>
    public IReadOnlyList<TaskItem> Tasks { get; private set; } = [];

    /// <summary>
    /// The index of the selected row within <see cref="Tasks"/>.
    /// </summary>
    public int SelectedIndex { get; private set; }

    /// <summary>
    /// The id of the task at <see cref="SelectedIndex"/>, or null when the
    /// list is empty.
    /// </summary>
    public string? SelectedTaskId
        => SelectedIndex >= 0 && SelectedIndex < Tasks.Count ? Tasks[SelectedIndex].Id : null;

    /// <summary>
    /// Replaces the result set. The previously selected task stays selected
    /// when it is still present in <paramref name="tasks"/>; otherwise the
    /// selected index clamps to the new list's bounds.
    /// </summary>
    public void SetTasks(IReadOnlyList<TaskItem> tasks)
    {
        var selectedTaskId = SelectedTaskId;
        Tasks = tasks;

        var preservedIndex = selectedTaskId is null ? -1 : IndexOf(tasks, selectedTaskId);
        SelectedIndex = preservedIndex >= 0
            ? preservedIndex
            : Math.Clamp(SelectedIndex, 0, Math.Max(0, tasks.Count - 1));

        _viewport.Update(tasks.Count, _viewport.WindowHeight);
        _viewport.EnsureVisible(SelectedIndex);
    }

    /// <summary>
    /// Moves the selected row one step in <paramref name="direction"/>,
    /// clamped to the list bounds. Directions other than up and down have no
    /// effect.
    /// </summary>
    public void MoveCursor(CursorDirection direction)
    {
        if (Tasks.Count == 0)
        {
            return;
        }

        SelectedIndex = direction switch
        {
            CursorDirection.Down => Math.Min(Tasks.Count - 1, SelectedIndex + 1),
            CursorDirection.Up => Math.Max(0, SelectedIndex - 1),
            _ => SelectedIndex
        };

        _viewport.EnsureVisible(SelectedIndex);
    }

    /// <summary>
    /// Moves the selected row to the given edge of the list.
    /// </summary>
    public void MoveToEdge(EdgeTarget edge)
    {
        if (Tasks.Count == 0)
        {
            return;
        }

        SelectedIndex = edge == EdgeTarget.Top ? 0 : Tasks.Count - 1;
        _viewport.EnsureVisible(SelectedIndex);
    }

    /// <summary>
    /// Renders the visible window of task rows, followed by a footer line
    /// reporting the total result count.
    /// </summary>
    public IRenderable Render(int width, int height, bool focused)
    {
        var listHeight = Math.Max(0, height - 1);
        _viewport.Update(Tasks.Count, listHeight);
        _viewport.EnsureVisible(SelectedIndex);

        var (start, count) = _viewport.Slice();
        var lines = new List<IRenderable>(count + 1);

        for (var i = start; i < start + count; i++)
        {
            var task = Tasks[i];
            var line = TaskBadge.Render(
                task.Id,
                task.Title,
                task.Status,
                task.Priority,
                task.Type,
                selected: focused && i == SelectedIndex,
                maxWidth: width);
            lines.Add(new Markup(line));
        }

        lines.Add(new Markup(Markup.Escape(FooterText())));

        return new Rows(lines);
    }

    private string FooterText() => $"{Tasks.Count} task(s)";

    private static int IndexOf(IReadOnlyList<TaskItem> tasks, string taskId)
    {
        for (var i = 0; i < tasks.Count; i++)
        {
            if (tasks[i].Id == taskId)
            {
                return i;
            }
        }

        return -1;
    }
}
