using ChilliCream.Nitro.CommandLine.Tui.Input;

namespace ChilliCream.Nitro.CommandLine.Tui.Editing;

/// <summary>
/// The outcome of submitting a <see cref="TaskCreateForm"/> against the task
/// store.
/// </summary>
internal abstract record TaskCreateOutcome
{
    private TaskCreateOutcome()
    {
    }

    /// <summary>
    /// The task was created, carrying the id the store allocated for it.
    /// </summary>
    public sealed record Succeeded(string TaskId, string ToastText) : TaskCreateOutcome;

    /// <summary>
    /// The store rejected the write, carrying its <see cref="ExitException"/> message.
    /// </summary>
    public sealed record Failed(string ToastText) : TaskCreateOutcome;

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
