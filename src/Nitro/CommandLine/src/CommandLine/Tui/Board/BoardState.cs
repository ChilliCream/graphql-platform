using ChilliCream.Nitro.CommandLine.Services.Tasks;

namespace ChilliCream.Nitro.CommandLine.Tui.Board;

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
    /// The raw index of each column with at least one task, in display order.
    /// </summary>
    public IReadOnlyList<int> VisibleColumnIndices
        => [.. Enumerable.Range(0, Columns.Count).Where(i => Columns[i].Tasks.Count > 0)];

    /// <summary>
    /// The columns with at least one task, in display order.
    /// </summary>
    public IReadOnlyList<BoardColumnState> VisibleColumns
        => [.. VisibleColumnIndices.Select(i => Columns[i])];

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
    /// Moves focus by <paramref name="delta"/> positions among the visible columns,
    /// clamped to the first or last visible column. Does nothing when no column is visible.
    /// </summary>
    public void FocusAdjacentVisibleColumn(int delta)
    {
        var visibleIndices = VisibleColumnIndices;

        if (visibleIndices.Count == 0)
        {
            return;
        }

        var currentPosition = IndexOf(visibleIndices, FocusedColumnIndex);
        var position = currentPosition < 0
            ? 0
            : Math.Clamp(currentPosition + delta, 0, visibleIndices.Count - 1);

        FocusedColumnIndex = visibleIndices[position];
    }

    /// <summary>
    /// Reloads every column from the task store. Each column's selected task
    /// stays selected when it is still present in the reloaded list;
    /// otherwise the selected row is clamped to the new list's bounds. When the
    /// focused column no longer has a task, focus moves to the first visible column.
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

        var visibleIndices = VisibleColumnIndices;

        if (visibleIndices.Count > 0 && !visibleIndices.Contains(FocusedColumnIndex))
        {
            FocusedColumnIndex = visibleIndices[0];
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

    private static int IndexOf(IReadOnlyList<int> values, int value)
    {
        for (var i = 0; i < values.Count; i++)
        {
            if (values[i] == value)
            {
                return i;
            }
        }

        return -1;
    }
}
