namespace ChilliCream.Nitro.CommandLine.Tui.Graph.Layout;

/// <summary>
/// The measured cell dimensions of a node before it is placed on the graph canvas.
/// </summary>
internal readonly record struct GraphNodeSize(int Width, int Height)
{
    public GraphNodeSize Normalize() => new(Math.Max(1, Width), Math.Max(1, Height));
}
