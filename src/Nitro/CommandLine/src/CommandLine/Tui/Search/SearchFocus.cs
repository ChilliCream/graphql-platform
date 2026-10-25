namespace ChilliCream.Nitro.CommandLine.Tui.Search;

/// <summary>
/// Which of search mode's three panes currently holds focus.
/// </summary>
internal enum SearchFocus
{
    /// <summary>
    /// The query input line at the top of the mode.
    /// </summary>
    Input,

    /// <summary>
    /// The results list.
    /// </summary>
    List,

    /// <summary>
    /// The detail panel for the selected task.
    /// </summary>
    Detail
}
