using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Tui.Input;
using ChilliCream.Nitro.CommandLine.Tui.Widgets.Form;

namespace ChilliCream.Nitro.CommandLine.Tui.Editing;

/// <summary>
/// The lifecycle write a <see cref="TaskLifecycleOutcome"/> resulted from.
/// </summary>
internal enum TaskLifecycleAction
{
    Close,
    Reopen,
    Delete
}

/// <summary>
/// The outcome of one <see cref="TaskLifecycleActions"/> write against the
/// task store.
/// </summary>
internal abstract record TaskLifecycleOutcome
{
    private TaskLifecycleOutcome()
    {
    }

    /// <summary>
    /// The write succeeded, carrying the task's state as returned by the
    /// store.
    /// </summary>
    public sealed record Succeeded(TaskLifecycleAction Action, TaskItem Task, string ToastText) : TaskLifecycleOutcome
    {
        /// <summary>
        /// Whether a detail view showing this task should pop back to its
        /// originating list: true once the task is deleted, since it no
        /// longer exists to display.
        /// </summary>
        public bool ShouldPopDetail => Action == TaskLifecycleAction.Delete;
    }

    /// <summary>
    /// The write was rejected: either the store threw <see cref="ExitException"/>,
    /// or the action was gated (see <see cref="TaskLifecycleActions.CanReopen"/>)
    /// before ever reaching the store.
    /// </summary>
    public sealed record Failed(TaskLifecycleAction Action, string ToastText) : TaskLifecycleOutcome;

    /// <summary>
    /// The shell toast this outcome should show: success styled for
    /// <see cref="Succeeded"/>, error styled for <see cref="Failed"/>.
    /// </summary>
    public TuiMessage.ShowToast ToShowToast() => this switch
    {
        Succeeded succeeded => new TuiMessage.ShowToast(succeeded.ToastText, ToastStyle.Success),
        Failed failed => new TuiMessage.ShowToast(failed.ToastText, ToastStyle.Error),
        _ => throw new NotSupportedException()
    };
}

/// <summary>
/// Close, reopen, and delete actions for the selected task: builds the
/// confirmation dialog for each action, and applies the confirmed action to
/// the task store.
/// </summary>
internal static class TaskLifecycleActions
{
    /// <summary>
    /// Builds the confirmation dialog for closing <paramref name="task"/>.
    /// </summary>
    public static ConfirmDialog CreateCloseDialog(TaskItem task)
        => new(
            $"Close task '{task.Id}': \"{task.Title}\"? Dependents blocked on it are released.",
            "Close");

    /// <summary>
    /// Builds the confirmation dialog for reopening <paramref name="task"/>.
    /// Only meaningful when <see cref="CanReopen"/> is true.
    /// </summary>
    public static ConfirmDialog CreateReopenDialog(TaskItem task)
        => new($"Reopen task '{task.Id}': \"{task.Title}\"?", "Reopen");

    /// <summary>
    /// Builds the confirmation dialog for deleting <paramref name="task"/>.
    /// </summary>
    public static ConfirmDialog CreateDeleteDialog(TaskItem task)
        => new(
            $"Delete task '{task.Id}': \"{task.Title}\"? This tombstones the task.",
            "Delete",
            ButtonKind.Danger);

    /// <summary>
    /// Whether reopening should be offered for <paramref name="task"/>: only a
    /// closed or archived task can be reopened.
    /// </summary>
    public static bool CanReopen(TaskItem task) => task.Status is TaskStates.Closed or TaskStates.Archived;

    /// <summary>
    /// Closes <paramref name="task"/> with <paramref name="reason"/>.
    /// </summary>
    public static async Task<TaskLifecycleOutcome> CloseAsync(
        ITaskStore store,
        TaskItem task,
        string reason,
        string actor,
        CancellationToken cancellationToken)
    {
        try
        {
            var closed = await store.CloseTaskAsync([task.Id], reason, actor, cancellationToken);
            return new TaskLifecycleOutcome.Succeeded(
                TaskLifecycleAction.Close, closed[0], $"Closed task '{task.Id}'.");
        }
        catch (ExitException ex)
        {
            return new TaskLifecycleOutcome.Failed(TaskLifecycleAction.Close, ex.Message);
        }
    }

    /// <summary>
    /// Reopens <paramref name="task"/> with <paramref name="reason"/>. Fails
    /// without writing to the store when <see cref="CanReopen"/> is false.
    /// </summary>
    public static async Task<TaskLifecycleOutcome> ReopenAsync(
        ITaskStore store,
        TaskItem task,
        string reason,
        string actor,
        CancellationToken cancellationToken)
    {
        if (!CanReopen(task))
        {
            return new TaskLifecycleOutcome.Failed(
                TaskLifecycleAction.Reopen, $"Task '{task.Id}' is not closed.");
        }

        try
        {
            var reopened = await store.ReopenTaskAsync(task.Id, reason, actor, cancellationToken);
            return new TaskLifecycleOutcome.Succeeded(
                TaskLifecycleAction.Reopen, reopened, $"Reopened task '{task.Id}'.");
        }
        catch (ExitException ex)
        {
            return new TaskLifecycleOutcome.Failed(TaskLifecycleAction.Reopen, ex.Message);
        }
    }

    /// <summary>
    /// Deletes (tombstones) <paramref name="task"/> with <paramref name="reason"/>.
    /// </summary>
    public static async Task<TaskLifecycleOutcome> DeleteAsync(
        ITaskStore store,
        TaskItem task,
        string reason,
        string actor,
        CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await store.DeleteTaskAsync(task.Id, reason, actor, cancellationToken);
            return new TaskLifecycleOutcome.Succeeded(
                TaskLifecycleAction.Delete, deleted, $"Deleted task '{task.Id}'.");
        }
        catch (ExitException ex)
        {
            return new TaskLifecycleOutcome.Failed(TaskLifecycleAction.Delete, ex.Message);
        }
    }
}
