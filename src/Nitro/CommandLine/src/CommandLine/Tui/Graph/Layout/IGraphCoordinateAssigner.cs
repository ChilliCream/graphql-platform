namespace ChilliCream.Nitro.CommandLine.Tui.Graph.Layout;

/// <summary>
/// Assigns cell-quantized positions to ordered vertices in each layer.
/// </summary>
internal interface IGraphCoordinateAssigner
{
    IReadOnlyDictionary<string, GraphLayoutNode> Assign(
        IReadOnlyList<List<LayoutVertex>> layers,
        int layerSpacing,
        int nodeSpacing);
}
