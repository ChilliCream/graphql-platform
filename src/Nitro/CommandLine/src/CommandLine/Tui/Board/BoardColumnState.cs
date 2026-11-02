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
