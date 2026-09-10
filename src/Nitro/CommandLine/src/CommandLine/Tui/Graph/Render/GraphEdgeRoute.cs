using ChilliCream.Nitro.CommandLine.Tui.Graph.Layout;

namespace ChilliCream.Nitro.CommandLine.Tui.Graph.Render;

/// <summary>
/// One routed span and the cells occupied by its stroke.
/// </summary>
internal sealed record GraphEdgeRoute(GraphLayoutEdgeSpan Span, IReadOnlyList<GraphLayoutPoint> Points);
