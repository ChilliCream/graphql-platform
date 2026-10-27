using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Tui.Input;

namespace ChilliCream.Nitro.CommandLine.Tui.Editing;

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
