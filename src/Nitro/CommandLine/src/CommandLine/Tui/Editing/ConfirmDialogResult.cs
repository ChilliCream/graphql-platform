namespace ChilliCream.Nitro.CommandLine.Tui.Editing;

/// <summary>
/// The outcome of a completed <see cref="ConfirmDialog"/> interaction.
/// </summary>
internal abstract record ConfirmDialogResult
{
    private ConfirmDialogResult()
    {
    }

    /// <summary>
    /// The confirm button was activated, or Enter was pressed while the
    /// reason field had focus. Carries the reason text entered, or an empty
    /// string when left blank.
    /// </summary>
    public sealed record Confirmed(string Reason) : ConfirmDialogResult;

    /// <summary>
    /// The dialog was cancelled: Escape was pressed, or the cancel button was
    /// activated.
    /// </summary>
    public sealed record Cancelled : ConfirmDialogResult;
}
