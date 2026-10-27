namespace ChilliCream.Nitro.CommandLine.Tui.Board;

/// <summary>
/// The layout chosen for one frame: which <see cref="BoardLayoutKind"/> applies
/// and the slot each column renders into, indexed the same as the board's
/// column list.
/// </summary>
internal sealed class BoardLayoutDecision
{
    /// <summary>
    /// The arrangement this decision resolved to.
    /// </summary>
    public required BoardLayoutKind Kind { get; init; }

    /// <summary>
    /// Each column's computed slot, indexed the same as the board's column list.
    /// </summary>
    public required IReadOnlyList<BoardColumnLayout> Columns { get; init; }
}
