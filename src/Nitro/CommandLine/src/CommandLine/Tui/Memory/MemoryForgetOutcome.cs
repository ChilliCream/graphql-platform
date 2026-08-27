using ChilliCream.Nitro.CommandLine.Tui.Input;

namespace ChilliCream.Nitro.CommandLine.Tui.Memory;

/// <summary>
/// The outcome of one <see cref="MemoryLifecycleActions.ForgetAsync"/> write
/// against the memory store.
/// </summary>
internal abstract record MemoryForgetOutcome
{
    private MemoryForgetOutcome()
    {
    }

    /// <summary>
    /// The delete succeeded.
    /// </summary>
    public sealed record Succeeded(string ToastText) : MemoryForgetOutcome;

    /// <summary>
    /// The store rejected the write with an <see cref="ExitException"/>.
    /// </summary>
    public sealed record Failed(string ToastText) : MemoryForgetOutcome;

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
