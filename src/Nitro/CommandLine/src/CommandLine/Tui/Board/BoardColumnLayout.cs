namespace ChilliCream.Nitro.CommandLine.Tui.Board;

/// <summary>
/// One column's computed slot within a <see cref="BoardLayoutDecision"/>.
/// </summary>
/// <param name="Width">The column's width in cells.</param>
/// <param name="Height">The column's height in cells.</param>
/// <param name="Expanded">
/// Whether the column renders its full panel, as opposed to a collapsed
/// title line.
/// </param>
internal readonly record struct BoardColumnLayout(int Width, int Height, bool Expanded);
