using ChilliCream.Nitro.CommandLine.Tui.Input;

namespace ChilliCream.Nitro.CommandLine.Tui.Memory;

/// <summary>
/// The outcome of submitting a <see cref="MemoryPromoteForm"/> against the
/// memory store.
/// </summary>
internal abstract record MemoryPromoteOutcome
{
    private MemoryPromoteOutcome()
    {
    }

    /// <summary>
    /// The promotion succeeded, carrying whether it had already happened
    /// from an earlier, idempotent promotion of the same journal entry.
    /// </summary>
    public sealed record Succeeded(string CuratedId, bool AlreadyPromoted, string ToastText) : MemoryPromoteOutcome;

    /// <summary>
    /// The store rejected the write, carrying its <see cref="ExitException"/>
    /// message: this is how an invalid type surfaces.
    /// </summary>
    public sealed record Failed(string ToastText) : MemoryPromoteOutcome;

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
