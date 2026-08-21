using ChilliCream.Nitro.CommandLine.Services.Tasks;

namespace ChilliCream.Nitro.CommandLine.Tui.Board;

/// <summary>
/// One board column's live data: its definition, the tasks currently loaded
/// for it, and which row is selected.
/// </summary>
internal sealed class BoardColumnState(ColumnDefinition definition)
{
    /// <summary>
    /// The column's declarative filter and sort.
    /// </summary>
    public ColumnDefinition Definition { get; } = definition;

    /// <summary>
    /// The tasks currently loaded for this column, in display order.
    /// </summary>
    public IReadOnlyList<TaskItem> Tasks { get; internal set; } = [];

    /// <summary>
    /// The index of the selected row within <see cref="Tasks"/>.
    /// </summary>
    public int SelectedRow { get; internal set; }

    /// <summary>
    /// The id of the task at <see cref="SelectedRow"/>, or null when the
    /// column is empty or the row is out of range.
    /// </summary>
    public string? SelectedTaskId
        => SelectedRow >= 0 && SelectedRow < Tasks.Count ? Tasks[SelectedRow].Id : null;
}

/// <summary>
/// The live state of a board: per-column task lists and selection, plus
/// which column has focus.
/// </summary>
internal sealed class BoardState
{
    private readonly BoardDataLoader _loader;

    public BoardState(BoardView view, BoardDataLoader loader)
    {
        View = view;
        _loader = loader;
        Columns = [.. view.Columns.Select(column => new BoardColumnState(column))];
    }

    /// <summary>
    /// The board layout this state was built from.
    /// </summary>
    public BoardView View { get; }

    /// <summary>
    /// The board's columns, in display order.
    /// </summary>
    public IReadOnlyList<BoardColumnState> Columns { get; }

    /// <summary>
    /// The index of the column that has focus.
    /// </summary>
    public int FocusedColumnIndex { get; private set; }

    /// <summary>
    /// Moves focus to the given column index, clamped to the valid range.
    /// </summary>
    public void FocusColumn(int index)
    {
        FocusedColumnIndex = Columns.Count == 0 ? 0 : Math.Clamp(index, 0, Columns.Count - 1);
    }

    /// <summary>
    /// Reloads every column from the task store. Each column's selected task
    /// stays selected when it is still present in the reloaded list;
    /// otherwise the selected row is clamped to the new list's bounds.
    /// </summary>
    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        foreach (var column in Columns)
        {
            var selectedTaskId = column.SelectedTaskId;

            var tasks = await _loader.LoadColumnAsync(column.Definition, cancellationToken);
            column.Tasks = tasks;

            var preservedIndex = selectedTaskId is null ? -1 : IndexOf(tasks, selectedTaskId);

            column.SelectedRow = preservedIndex >= 0
                ? preservedIndex
                : Math.Clamp(column.SelectedRow, 0, Math.Max(0, tasks.Count - 1));
        }
    }

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
