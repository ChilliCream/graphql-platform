namespace ChilliCream.Nitro.CommandLine.Tui.Board;

/// <summary>
/// Cycling and display helpers for <see cref="BoardOrientation"/>.
/// </summary>
internal static class BoardOrientationExtensions
{
    /// <summary>
    /// The orientation after <paramref name="orientation"/> in the cycle
    /// Auto, SideBySide, Stacked, back to Auto.
    /// </summary>
    public static BoardOrientation Next(this BoardOrientation orientation) => orientation switch
    {
        BoardOrientation.Auto => BoardOrientation.SideBySide,
        BoardOrientation.SideBySide => BoardOrientation.Stacked,
        _ => BoardOrientation.Auto
    };

    /// <summary>
    /// The lowercase label shown to the user for <paramref name="orientation"/>.
    /// </summary>
    public static string ToLabel(this BoardOrientation orientation) => orientation switch
    {
        BoardOrientation.SideBySide => "side by side",
        BoardOrientation.Stacked => "stacked",
        _ => "auto"
    };
}
