using Spectre.Console;
using Spectre.Console.Rendering;

namespace ChilliCream.Nitro.CommandLine.Tui.Graph.Render;

/// <summary>
/// Produces the graph canvas metrics line displayed below a rendered graph.
/// </summary>
internal static class GraphRenderFooter
{
    public static string CreateText(GraphRenderResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return $"nodes: {result.Layout.Nodes.Count}  edges: {result.Routes.Count}  grid: {result.Buffer.Width} x {result.Buffer.Height}  crossings: {result.Layout.CrossingCount}  reversed: {result.Layout.ReversedEdgeCount}";
    }

    public static IRenderable Render(GraphRenderResult result) => new Text(CreateText(result));
}
