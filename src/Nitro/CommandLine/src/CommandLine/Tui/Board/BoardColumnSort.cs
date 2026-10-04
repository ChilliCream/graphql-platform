namespace ChilliCream.Nitro.CommandLine.Tui.Board;

/// <summary>
/// How a column orders the tasks it loads.
/// </summary>
internal enum BoardColumnSort
{
    /// <summary>
    /// Priority ascending, then created date ascending, then id ascending,
    /// matching the list task command's default order.
    /// </summary>
    Default,

    /// <summary>
    /// Closed date descending (falling back to updated date), then id
    /// ascending.
    /// </summary>
    RecentFirst
}
