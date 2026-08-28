using ChilliCream.Nitro.CommandLine.Tui.Input;

namespace ChilliCream.Nitro.CommandLine.Tui.Editing;

/// <summary>
/// The outcome of submitting a <see cref="TaskEditorForm"/> against the task
/// store.
/// </summary>
internal abstract record TaskEditorOutcome
{
    private TaskEditorOutcome()
    {
    }

    /// <summary>
    /// Every field that differed from the snapshot was written. Carries the
    /// changed store field names, empty when no field differed.
    /// </summary>
    public sealed record Succeeded(IReadOnlyList<string> ChangedFields, string ToastText) : TaskEditorOutcome;

    /// <summary>
    /// The store rejected a write, carrying its <see cref="ExitException"/> message.
    /// </summary>
    public sealed record Failed(string ToastText) : TaskEditorOutcome;

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
