using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Tui.Input;
using CursorDirection = ChilliCream.Nitro.CommandLine.Tui.Input.CursorDirection;

namespace ChilliCream.Nitro.CommandLine.Tui.Details;

/// <summary>
/// The loaded state behind a task detail view: the task and its related
/// data, a cursor over its combined dependency and blocks rows, and a
/// navigation stack for drilling into related tasks and returning.
/// </summary>
internal sealed class TaskDetailModel
{
    private readonly ITaskStore _store;
    private readonly Stack<string> _backStack = new();

    public TaskDetailModel(ITaskStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    /// <summary>
    /// The currently loaded task, or null when the last load target does not
    /// exist.
    /// </summary>
    public TaskItem? Task { get; private set; }

    /// <summary>
    /// The id last passed to <see cref="LoadAsync"/>, even when it did not
    /// resolve to a task.
    /// </summary>
    public string? CurrentTaskId { get; private set; }

    /// <summary>
    /// The loaded task's labels, ordered by label.
    /// </summary>
    public IReadOnlyList<string> Labels { get; private set; } = [];

    /// <summary>
    /// The loaded task's comments, ordered by created_at then id.
    /// </summary>
    public IReadOnlyList<TaskComment> Comments { get; private set; } = [];

    /// <summary>
    /// The descriptions of the tasks currently blocking the loaded task, from
    /// the store's computed blocked set.
    /// </summary>
    public IReadOnlyList<string> BlockedBy { get; private set; } = [];

    /// <summary>
    /// The loaded task's outgoing dependencies followed by its incoming
    /// blocks, in that order. <see cref="SelectedRowIndex"/> indexes into
    /// this list.
    /// </summary>
    public IReadOnlyList<TaskDetailRow> Rows { get; private set; } = [];

    /// <summary>
    /// The index of the selected row within <see cref="Rows"/>.
    /// </summary>
    public int SelectedRowIndex { get; private set; }

    /// <summary>
    /// Whether <see cref="GoBackAsync"/> has a task to return to.
    /// </summary>
    public bool CanGoBack => _backStack.Count > 0;

    /// <summary>
    /// Loads the task with the given id and its related data, without
    /// affecting the navigation stack. Resets the row cursor. Returns whether
    /// the task exists.
    /// </summary>
    public async Task<bool> LoadAsync(string taskId, CancellationToken cancellationToken)
    {
        CurrentTaskId = taskId;

        var task = await _store.GetTaskAsync(taskId, cancellationToken).ConfigureAwait(false);

        if (task is null)
        {
            Task = null;
            Labels = [];
            Comments = [];
            BlockedBy = [];
            Rows = [];
            SelectedRowIndex = 0;
            return false;
        }

        var labels = await _store.GetLabelsAsync(taskId, cancellationToken).ConfigureAwait(false);
        var dependencies = await _store.GetDependenciesAsync(taskId, cancellationToken).ConfigureAwait(false);
        var blocks = await _store.GetDependentsAsync(taskId, cancellationToken).ConfigureAwait(false);
        var comments = await _store.GetCommentsAsync(taskId, cancellationToken).ConfigureAwait(false);
        var blocked = await _store.ComputeBlockedAsync(cancellationToken).ConfigureAwait(false);

        Task = task;
        Labels = labels;
        Comments = comments;
        BlockedBy = blocked.TryGetValue(taskId, out var blockers) ? blockers : [];
        Rows = BuildRows(dependencies, blocks);
        SelectedRowIndex = 0;
        return true;
    }

    /// <summary>
    /// Moves <see cref="SelectedRowIndex"/> one step in <paramref name="direction"/>,
    /// clamped to <see cref="Rows"/>. Directions other than up and down have
    /// no effect.
    /// </summary>
    public void MoveRowCursor(CursorDirection direction)
    {
        if (Rows.Count == 0)
        {
            return;
        }

        SelectedRowIndex = direction switch
        {
            CursorDirection.Down => Math.Min(Rows.Count - 1, SelectedRowIndex + 1),
            CursorDirection.Up => Math.Max(0, SelectedRowIndex - 1),
            _ => SelectedRowIndex
        };
    }

    /// <summary>
    /// Loads the task targeted by the selected row, pushing the current
    /// task's id onto the navigation stack. Has no effect, and returns false,
    /// when no row is selected or the target task no longer exists.
    /// </summary>
    public async Task<bool> OpenSelectedRowAsync(CancellationToken cancellationToken)
    {
        if (Task is not { } current)
        {
            return false;
        }

        if (SelectedRowIndex < 0 || SelectedRowIndex >= Rows.Count)
        {
            return false;
        }

        var row = Rows[SelectedRowIndex];

        if (row.Status is null)
        {
            return false;
        }

        if (!await LoadAsync(row.TargetId, cancellationToken).ConfigureAwait(false))
        {
            return false;
        }

        _backStack.Push(current.Id);
        return true;
    }

    /// <summary>
    /// Pops the navigation stack and loads the popped task id. Has no
    /// effect, and returns false, when the stack is empty.
    /// </summary>
    public async Task<bool> GoBackAsync(CancellationToken cancellationToken)
    {
        if (_backStack.Count == 0)
        {
            return false;
        }

        var previousId = _backStack.Pop();
        await LoadAsync(previousId, cancellationToken).ConfigureAwait(false);
        return true;
    }

    private static IReadOnlyList<TaskDetailRow> BuildRows(
        IReadOnlyList<TaskDependencyDetail> dependencies,
        IReadOnlyList<TaskDependentDetail> blocks)
    {
        var rows = new List<TaskDetailRow>(dependencies.Count + blocks.Count);
        var index = 0;

        foreach (var dependency in dependencies)
        {
            rows.Add(new TaskDetailRow(
                index++,
                TaskDetailRowKind.Dependency,
                dependency.Type,
                dependency.DependsOnId,
                dependency.Status,
                dependency.Title));
        }

        foreach (var block in blocks)
        {
            rows.Add(new TaskDetailRow(
                index++,
                TaskDetailRowKind.Blocks,
                block.Type,
                block.TaskId,
                block.Status,
                block.Title));
        }

        return rows;
    }
}
