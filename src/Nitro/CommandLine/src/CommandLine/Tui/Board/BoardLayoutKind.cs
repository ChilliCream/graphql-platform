namespace ChilliCream.Nitro.CommandLine.Tui.Board;

/// <summary>
/// The arrangement a <see cref="BoardLayoutDecision"/> resolves to for one frame.
/// </summary>
internal enum BoardLayoutKind
{
    /// <summary>Every column is rendered side by side at equal width.</summary>
    Grid,

    /// <summary>Only the focused column is rendered, at the full content width.</summary>
    Maximized,

    /// <summary>
    /// Columns are stacked vertically, each sharing an equal slice of the
    /// available height. On frames too short to give every column a usable
    /// slice, the focused column expands instead and the rest collapse to a
    /// single title line.
    /// </summary>
    Stacked
}
