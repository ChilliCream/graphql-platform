namespace ChilliCream.Nitro.CommandLine.Tui.Widgets.Form;

/// <summary>
/// The outcome of a completed <see cref="Form"/> interaction.
/// </summary>
internal abstract record FormResult
{
    private FormResult()
    {
    }

    /// <summary>
    /// The primary button was activated while every field passed validation.
    /// </summary>
    public sealed record Submitted(IReadOnlyDictionary<string, FormValue> Values) : FormResult;

    /// <summary>
    /// The form was cancelled: Escape was pressed while no field consumed it.
    /// </summary>
    public sealed record Cancelled : FormResult;

    /// <summary>
    /// A non-primary button was activated. The <see cref="Form"/> does not
    /// interpret the id; the host decides what it means.
    /// </summary>
    public sealed record ButtonActivated(string ButtonId) : FormResult;
}
