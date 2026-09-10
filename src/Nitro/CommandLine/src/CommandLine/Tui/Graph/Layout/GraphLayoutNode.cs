namespace ChilliCream.Nitro.CommandLine.Tui.Graph.Layout;

/// <summary>
/// The resolved canvas position and layer order of one graph node.
/// </summary>
internal sealed record GraphLayoutNode(
    string Id,
    int X,
    int Y,
    int Width,
    int Height,
    int Layer,
    int Order);
