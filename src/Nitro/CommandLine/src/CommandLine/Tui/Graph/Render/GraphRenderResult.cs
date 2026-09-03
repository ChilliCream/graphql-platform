using ChilliCream.Nitro.CommandLine.Tui.Graph.Layout;

namespace ChilliCream.Nitro.CommandLine.Tui.Graph.Render;

/// <summary>
/// The routed graph canvas and its per-span geometry.
/// </summary>
internal sealed record GraphRenderResult(CellBuffer Buffer, IReadOnlyList<GraphEdgeRoute> Routes, GraphLayoutResult Layout)
{
    public int RenderedEdgeCount { get; init; }

    public CanvasViewport Viewport => new(0, 0, Buffer.Width, Buffer.Height);
}
