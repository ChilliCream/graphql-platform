namespace ChilliCream.Nitro.CommandLine.Tui.Graph.Render;

/// <summary>
/// A visible character and its terminal style at one canvas coordinate.
/// </summary>
internal readonly record struct CanvasCell(char Glyph, Style Style, CellOwners Owners)
{
    public static readonly CanvasCell Empty = new(' ', Style.Plain, default);
}
