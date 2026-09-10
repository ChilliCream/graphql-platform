namespace ChilliCream.Nitro.CommandLine.Tui.Graph.Layout;

/// <summary>
/// Places ordered nodes on integer cell coordinates while preserving their layer order.
/// </summary>
internal sealed class OrderedCoordinateAssigner : IGraphCoordinateAssigner
{
    public IReadOnlyDictionary<LayoutVertex, GraphLayoutPoint> Assign(
        IReadOnlyList<List<LayoutVertex>> layers,
        int layerSpacing,
        int nodeSpacing)
    {
        var layerX = 0;
        var positions = new Dictionary<LayoutVertex, int>();
        var widths = new int[layers.Count];

        for (var layerIndex = 0; layerIndex < layers.Count; layerIndex++)
        {
            var layer = layers[layerIndex];
            var y = 0;
            foreach (var vertex in layer)
            {
                positions[vertex] = y;
                y += vertex.Size.Height + nodeSpacing;
            }

            widths[layerIndex] = layer.Count == 0 ? 0 : layer.Max(t => t.Size.Width);
        }

        for (var round = 0; round < 4; round++)
        {
            var start = round % 2 == 0 ? 1 : layers.Count - 2;
            var end = round % 2 == 0 ? layers.Count : -1;
            var step = round % 2 == 0 ? 1 : -1;

            for (var layerIndex = start; layerIndex != end; layerIndex += step)
            {
                Align(layers[layerIndex], positions, nodeSpacing, round % 2 == 0);
            }
        }

        var result = new Dictionary<LayoutVertex, GraphLayoutPoint>();
        for (var layerIndex = 0; layerIndex < layers.Count; layerIndex++)
        {
            var layer = layers[layerIndex];
            foreach (var vertex in layer)
            {
                result[vertex] = new GraphLayoutPoint(layerX, positions[vertex]);
            }

            layerX += widths[layerIndex] + layerSpacing;
        }

        return result;
    }

    private static void Align(
        IReadOnlyList<LayoutVertex> layer,
        Dictionary<LayoutVertex, int> positions,
        int spacing,
        bool useIncoming)
    {
        for (var index = 0; index < layer.Count; index++)
        {
            var vertex = layer[index];
            var neighbors = useIncoming ? vertex.Incoming : vertex.Outgoing;
            if (neighbors.Count == 0)
            {
                continue;
            }

            var centers = neighbors
                .Select(t => useIncoming ? t.From : t.To)
                .Select(t => positions[t] + (t.Size.Height - 1) / 2)
                .Order()
                .ToArray();
            var desired = centers[(centers.Length - 1) / 2] - (vertex.Size.Height - 1) / 2;
            var lowerBound = index == 0 ? 0 : positions[layer[index - 1]] + layer[index - 1].Size.Height + spacing;
            positions[vertex] = Math.Max(lowerBound, desired);
        }

        for (var index = 1; index < layer.Count; index++)
        {
            var previous = layer[index - 1];
            var vertex = layer[index];
            positions[vertex] = Math.Max(positions[vertex], positions[previous] + previous.Size.Height + spacing);
        }
    }
}
