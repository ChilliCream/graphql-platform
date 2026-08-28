using ChilliCream.Nitro.CommandLine.Tui.Input;

namespace ChilliCream.Nitro.CommandLine.Tui.Mail;

/// <summary>
/// The outcome of one <see cref="MailLifecycleActions"/> write against the
/// mail store.
/// </summary>
internal abstract record MailActionOutcome
{
    private MailActionOutcome()
    {
    }

    /// <summary>
    /// The write succeeded.
    /// </summary>
    public sealed record Succeeded(MailAction Action, string ToastText) : MailActionOutcome;

    /// <summary>
    /// The store rejected the write with an <see cref="ExitException"/>.
    /// </summary>
    public sealed record Failed(MailAction Action, string ToastText) : MailActionOutcome;

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
