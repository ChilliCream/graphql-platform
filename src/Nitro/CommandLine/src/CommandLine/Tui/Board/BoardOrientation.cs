namespace ChilliCream.Nitro.CommandLine.Tui.Board;

/// <summary>
/// How the board arranges its columns when no column is maximized.
/// </summary>
internal enum BoardOrientation
{
    /// <summary>Columns sit side by side while each gets enough width, and stack otherwise.</summary>
    Auto,

    /// <summary>Columns always sit side by side, however narrow they become.</summary>
    SideBySide,

    /// <summary>Columns are always stacked vertically.</summary>
    Stacked
}
