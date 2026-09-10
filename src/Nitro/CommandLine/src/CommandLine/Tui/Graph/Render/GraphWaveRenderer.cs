using ChilliCream.Nitro.CommandLine.Tui.Graph.Layout;

namespace ChilliCream.Nitro.CommandLine.Tui.Graph.Render;

/// <summary>
/// Renders wave captions and non-destructive separators for graph layout layers.
/// </summary>
internal static class GraphWaveRenderer
{
    public static CellBuffer CreateHeader(GraphLayoutResult layout, int width)
    {
        var header = new CellBuffer(width, 1);
        var columns = GetColumns(layout);
        var style = GraphEdgeStyles.Dim(GraphEdgeStyles.Line);

        for (var index = 1; index < columns.Length; index++)
        {
            if (TryGetSeparatorX(columns[index - 1], columns[index], out var separatorX))
            {
                header.Set(separatorX, 0, '│', style);
            }
        }

        foreach (var column in columns)
        {
            var caption = $"wave {column.Layer + 1}";
            var captionX = column.X + Math.Max(0, (column.Width - caption.Length) / 2);
            for (var index = 0; index < caption.Length; index++)
            {
                header.Set(captionX + index, 0, caption[index], style);
            }
        }

        return header;
    }

    public static void ApplySeparators(CellBuffer buffer, GraphLayoutResult layout)
    {
        var columns = GetColumns(layout);
        var style = GraphEdgeStyles.Dim(GraphEdgeStyles.Line);

        for (var index = 1; index < columns.Length; index++)
        {
            if (!TryGetSeparatorX(columns[index - 1], columns[index], out var separatorX))
            {
                continue;
            }

            for (var y = 0; y < buffer.Height; y++)
            {
                buffer.SetIfEmpty(separatorX, y, '│', style);
            }
        }
    }

    private static LayerColumn[] GetColumns(GraphLayoutResult layout)
        => layout.Nodes
            .GroupBy(node => node.Layer)
            .OrderBy(group => group.Key)
            .Select(group =>
            {
                var x = group.Min(node => node.X);
                var right = group.Max(node => node.X + node.Width);
                return new LayerColumn(group.Key, x, right - x);
            })
            .ToArray();

    private static bool TryGetSeparatorX(LayerColumn left, LayerColumn right, out int separatorX)
    {
        var leftRight = left.X + left.Width;
        if (leftRight >= right.X)
        {
            separatorX = 0;
            return false;
        }

        separatorX = leftRight + ((right.X - leftRight) / 2);
        return true;
    }

    private readonly record struct LayerColumn(int Layer, int X, int Width);
}
