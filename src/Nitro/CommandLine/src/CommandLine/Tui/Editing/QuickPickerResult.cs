namespace ChilliCream.Nitro.CommandLine.Tui.Editing;

/// <summary>
/// The outcome of a completed <see cref="QuickPicker"/> interaction.
/// </summary>
internal abstract record QuickPickerResult
{
    private QuickPickerResult()
    {
    }

    /// <summary>
    /// Enter was pressed: carries the id of the option that was selected.
    /// </summary>
    public sealed record Applied(string SelectedId) : QuickPickerResult;

    /// <summary>
    /// Escape was pressed; nothing should change.
    /// </summary>
    public sealed record Cancelled : QuickPickerResult;
}
