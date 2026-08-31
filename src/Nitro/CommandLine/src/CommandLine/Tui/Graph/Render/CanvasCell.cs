using Spectre.Console;

namespace ChilliCream.Nitro.CommandLine.Tui.Graph.Render;

/// <summary>
/// A visible character and its terminal style at one canvas coordinate.
/// </summary>
internal readonly record struct CanvasCell(char Glyph, Style Style, IReadOnlyList<object> Owners)
{
    public static readonly CanvasCell Empty = new(' ', Style.Plain, []);
}

[Flags]
internal enum CanvasDirections
{
    None = 0,
    Up = 1,
    Right = 2,
    Down = 4,
    Left = 8
}
