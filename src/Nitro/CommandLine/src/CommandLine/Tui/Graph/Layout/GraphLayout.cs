using ChilliCream.Nitro.CommandLine.Tui.Graph;

namespace ChilliCream.Nitro.CommandLine.Tui.Graph.Layout;

/// <summary>
/// Produces a deterministic, left-to-right, layered layout for a graph model.
/// </summary>
internal sealed class GraphLayout
{
    private readonly IGraphLayeringStrategy _layering;
    private readonly IGraphCoordinateAssigner _coordinates;
    private readonly GraphLayoutMetrics? _metrics;

    public GraphLayout(
        IGraphLayeringStrategy? layering = null,
        IGraphCoordinateAssigner? coordinates = null,
        GraphLayoutMetrics? metrics = null)
    {
        _layering = layering ?? new LongestPathLayering();
        _coordinates = coordinates ?? new OrderedCoordinateAssigner();
        _metrics = metrics;
    }

    public GraphLayoutResult Layout(
        GraphModel model,
        IReadOnlyDictionary<string, GraphNodeSize> measuredNodeSizes,
        GraphLayoutResult? previousLayout = null,
        int layerSpacing = 4,
        int nodeSpacing = 1)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(measuredNodeSizes);

        if (layerSpacing < 0)
        {
            throw ThrowHelper.LayerSpacingMustBeNonNegative();
        }

        if (nodeSpacing < 0)
        {
            throw ThrowHelper.NodeSpacingMustBeNonNegative();
        }

        var orderedNodes = model.Nodes.OrderBy(t => t.Id, StringComparer.Ordinal).ToArray();
        if (orderedNodes.Length == 0)
        {
            return new GraphLayoutResult([], [], 0, 0);
        }

        var arcs = GraphCycleGuard.Guard(model);
        var nodeIds = orderedNodes.Select(t => t.Id).ToArray();
        var layersById = _layering.AssignLayers(nodeIds, arcs, previousLayout);
        var layers = CreateVertices(orderedNodes, measuredNodeSizes, layersById);
        var segments = AddSegments(arcs, layers, layersById);
        Order(layers, previousLayout);
        MinimizeCrossings(layers, segments, _metrics);

        var positions = _coordinates.Assign(layers, layerSpacing, nodeSpacing);
        var nodes = layers
            .SelectMany(t => t)
            .Where(t => t.NodeId is not null)
            .Select(t => new GraphLayoutNode(
                t.NodeId!,
                positions[t].X,
                positions[t].Y,
                t.Size.Width,
                t.Size.Height,
                t.Layer,
                t.Order))
            .OrderBy(t => t.Id, StringComparer.Ordinal)
            .ToArray();
        var spans = segments
            .OrderBy(t => t.Arc.Edge.FromId, StringComparer.Ordinal)
            .ThenBy(t => t.Arc.Edge.ToId, StringComparer.Ordinal)
            .ThenBy(t => t.Arc.Edge.Kind)
            .ThenBy(t => t.Sequence)
            .Select(t => new GraphLayoutEdgeSpan(
                t.Arc.Edge,
                t.From.Layer,
                t.To.Layer,
                t.From.Order,
                t.To.Order,
                positions[t.From],
                positions[t.To],
                t.Arc.IsReversed))
            .ToArray();

        return new GraphLayoutResult(
            nodes,
            spans,
            CountCrossings(segments),
            arcs.Count(t => t.IsReversed));
    }

    private static List<List<LayoutVertex>> CreateVertices(
        IReadOnlyList<GraphNode> nodes,
        IReadOnlyDictionary<string, GraphNodeSize> measuredNodeSizes,
        IReadOnlyDictionary<string, int> layersById)
    {
        var layerCount = layersById.Values.Max() + 1;
        var layers = Enumerable.Range(0, layerCount).Select(_ => new List<LayoutVertex>()).ToList();

        foreach (var node in nodes)
        {
            var size = measuredNodeSizes.TryGetValue(node.Id, out var measured)
                ? measured.Normalize()
                : new GraphNodeSize(1, 1);
            var vertex = new LayoutVertex
            {
                Key = node.Id,
                NodeId = node.Id,
                Size = size,
                Layer = layersById[node.Id]
            };
            layers[vertex.Layer].Add(vertex);
        }

        return layers;
    }

    private static IReadOnlyList<LayoutSegment> AddSegments(
        IReadOnlyList<LayoutArc> arcs,
        IReadOnlyList<List<LayoutVertex>> layers,
        IReadOnlyDictionary<string, int> layersById)
    {
        var vertices = layers.SelectMany(t => t).Where(t => t.NodeId is not null)
            .ToDictionary(t => t.NodeId!, StringComparer.Ordinal);
        var segments = new List<LayoutSegment>();
        var sequence = 0;

        foreach (var arc in arcs)
        {
            var from = vertices[arc.FromId];
            var to = vertices[arc.ToId];
            var previous = from;
            for (var layer = layersById[arc.FromId] + 1; layer < layersById[arc.ToId]; layer++)
            {
                var dummy = new LayoutVertex
                {
                    Key = $"{arc.FromId}\u001f{arc.ToId}\u001f{layer:D8}",
                    Size = new GraphNodeSize(1, 1),
                    Layer = layer
                };
                layers[layer].Add(dummy);
                Add(previous, dummy, arc, sequence++, segments);
                previous = dummy;
            }

            Add(previous, to, arc, sequence++, segments);
        }

        return segments;
    }

    private static void Add(
        LayoutVertex from,
        LayoutVertex to,
        LayoutArc arc,
        int sequence,
        ICollection<LayoutSegment> segments)
    {
        var segment = new LayoutSegment(from, to, arc, sequence);
        from.Outgoing.Add(segment);
        to.Incoming.Add(segment);
        segments.Add(segment);
    }

    private static void Order(IReadOnlyList<List<LayoutVertex>> layers, GraphLayoutResult? previousLayout)
    {
        var previous = previousLayout?.Nodes.ToDictionary(t => t.Id, StringComparer.Ordinal)
            ?? new Dictionary<string, GraphLayoutNode>(StringComparer.Ordinal);
        foreach (var layer in layers)
        {
            layer.Sort((left, right) => CompareInitial(left, right, previous));
            SetOrders(layer);
        }
    }

    private static int CompareInitial(
        LayoutVertex left,
        LayoutVertex right,
        IReadOnlyDictionary<string, GraphLayoutNode> previous)
    {
        var leftPrevious = left.NodeId is not null && previous.TryGetValue(left.NodeId, out var leftNode)
            ? leftNode
            : null;
        var rightPrevious = right.NodeId is not null && previous.TryGetValue(right.NodeId, out var rightNode)
            ? rightNode
            : null;
        if (leftPrevious is not null && rightPrevious is not null)
        {
            var byOrder = leftPrevious.Order.CompareTo(rightPrevious.Order);
            if (byOrder != 0)
            {
                return byOrder;
            }
        }
        else if (leftPrevious is not null)
        {
            return -1;
        }
        else if (rightPrevious is not null)
        {
            return 1;
        }

        return string.CompareOrdinal(left.Key, right.Key);
    }

    private static void MinimizeCrossings(
        IReadOnlyList<List<LayoutVertex>> layers,
        IReadOnlyList<LayoutSegment> segments,
        GraphLayoutMetrics? metrics)
    {
        var best = layers.Select(t => t.ToArray()).ToArray();
        var bestCrossingCount = CountCrossings(segments);

        for (var iteration = 0; iteration < 8; iteration++)
        {
            if (iteration % 2 == 0)
            {
                for (var layer = 1; layer < layers.Count; layer++)
                {
                    BarycenterSort(layers[layer], incoming: true);
                }
            }
            else
            {
                for (var layer = layers.Count - 2; layer >= 0; layer--)
                {
                    BarycenterSort(layers[layer], incoming: false);
                }
            }

            Transpose(layers, segments, metrics);
            var crossingCount = CountCrossings(segments);
            if (crossingCount < bestCrossingCount)
            {
                bestCrossingCount = crossingCount;
                for (var layer = 0; layer < layers.Count; layer++)
                {
                    best[layer] = layers[layer].ToArray();
                }
            }
        }

        for (var layer = 0; layer < layers.Count; layer++)
        {
            layers[layer].Clear();
            layers[layer].AddRange(best[layer]);
            SetOrders(layers[layer]);
        }
    }

    private static void BarycenterSort(List<LayoutVertex> layer, bool incoming)
    {
        var ranked = layer
            .Select((vertex, index) => new
            {
                Vertex = vertex,
                Index = index,
                Barycenter = CalculateBarycenter(vertex, incoming)
            })
            .OrderBy(t => t.Barycenter)
            .ThenBy(t => t.Index)
            .ThenBy(t => t.Vertex.Key, StringComparer.Ordinal)
            .Select(t => t.Vertex)
            .ToArray();
        layer.Clear();
        layer.AddRange(ranked);
        SetOrders(layer);
    }

    private static double CalculateBarycenter(LayoutVertex vertex, bool incoming)
    {
        var neighbors = incoming ? vertex.Incoming.Select(t => t.From) : vertex.Outgoing.Select(t => t.To);
        var orders = neighbors.Select(t => t.Order).ToArray();
        return orders.Length == 0 ? vertex.Order : orders.Average();
    }

    private static void Transpose(
        IReadOnlyList<List<LayoutVertex>> layers,
        IReadOnlyList<LayoutSegment> segments,
        GraphLayoutMetrics? metrics)
    {
        var segmentsByLayer = segments
            .GroupBy(t => t.From.Layer)
            .ToDictionary(t => t.Key, t => (IReadOnlyList<LayoutSegment>)t.ToArray());
        var captureCandidateObservations = metrics?.CaptureCandidateObservations == true;

        for (var pass = 0; pass < 4; pass++)
        {
            var changed = false;
            foreach (var layer in layers)
            {
                for (var index = 0; index < layer.Count - 1; index++)
                {
                    var left = layer[index];
                    var right = layer[index + 1];
                    metrics?.RecordCandidate();
                    var before = CountIncidentCrossings(left, right, segmentsByLayer, metrics);
                    var fullBefore = captureCandidateObservations
                        ? CountCrossings(segments)
                        : 0;
                    (layer[index], layer[index + 1]) = (layer[index + 1], layer[index]);
                    left.Order = index + 1;
                    right.Order = index;
                    var after = CountIncidentCrossings(left, right, segmentsByLayer, metrics);
                    var fullAfter = captureCandidateObservations
                        ? CountCrossings(segments)
                        : 0;
                    var accepted = after < before;
                    if (captureCandidateObservations)
                    {
                        metrics!.RecordCandidateObservation(before, after, fullBefore, fullAfter, accepted);
                    }
                    if (accepted)
                    {
                        changed = true;
                    }
                    else
                    {
                        (layer[index], layer[index + 1]) = (layer[index + 1], layer[index]);
                        left.Order = index;
                        right.Order = index + 1;
                    }
                }
            }

            if (!changed)
            {
                return;
            }
        }
    }

    private static int CountCrossings(IReadOnlyList<LayoutSegment> segments)
    {
        var count = 0;
        foreach (var layer in segments.GroupBy(t => t.From.Layer))
        {
            var ordered = layer
                .OrderBy(t => t.From.Order)
                .ThenBy(t => t.Sequence)
                .ToArray();
            var tree = new FenwickTree(layer.Max(t => t.To.Order) + 1);
            var seen = 0;

            for (var start = 0; start < ordered.Length;)
            {
                var end = start + 1;
                while (end < ordered.Length && ordered[start].From.Order == ordered[end].From.Order)
                {
                    end++;
                }

                for (var index = start; index < end; index++)
                {
                    count += seen - tree.Sum(ordered[index].To.Order + 1);
                }

                for (var index = start; index < end; index++)
                {
                    tree.Add(ordered[index].To.Order, 1);
                    seen++;
                }

                start = end;
            }
        }

        return count;
    }

    private static int CountIncidentCrossings(
        LayoutVertex left,
        LayoutVertex right,
        IReadOnlyDictionary<int, IReadOnlyList<LayoutSegment>> segmentsByLayer,
        GraphLayoutMetrics? metrics)
    {
        var incident = left.Incoming
            .Concat(left.Outgoing)
            .Concat(right.Incoming)
            .Concat(right.Outgoing)
            .ToArray();
        var incidentSequences = incident.Select(t => t.Sequence).ToHashSet();
        var count = 0;

        foreach (var segment in incident)
        {
            foreach (var other in segmentsByLayer[segment.From.Layer])
            {
                metrics?.RecordIncidentComparison();
                if (segment.Sequence == other.Sequence
                    || (incidentSequences.Contains(other.Sequence) && other.Sequence < segment.Sequence))
                {
                    continue;
                }

                if (Crosses(segment, other))
                {
                    count++;
                }
            }
        }

        return count;
    }

    private static bool Crosses(LayoutSegment left, LayoutSegment right)
    {
        var from = left.From.Order.CompareTo(right.From.Order);
        var to = left.To.Order.CompareTo(right.To.Order);
        return from != 0 && to != 0 && from != to;
    }

    private static void SetOrders(IReadOnlyList<LayoutVertex> layer)
    {
        for (var index = 0; index < layer.Count; index++)
        {
            layer[index].Order = index;
        }
    }

    private sealed class FenwickTree
    {
        private readonly int[] _values;

        public FenwickTree(int length)
        {
            _values = new int[length + 1];
        }

        public void Add(int index, int value)
        {
            for (index++; index < _values.Length; index += index & -index)
            {
                _values[index] += value;
            }
        }

        public int Sum(int length)
        {
            var sum = 0;
            for (; length > 0; length -= length & -length)
            {
                sum += _values[length];
            }

            return sum;
        }
    }
}

/// <summary>
/// Captures crossing minimization operations for verification.
/// </summary>
internal sealed class GraphLayoutMetrics
{
    private readonly List<GraphLayoutCandidateObservation>? _candidateObservations;

    public GraphLayoutMetrics(bool captureCandidateObservations = false)
    {
        if (captureCandidateObservations)
        {
            _candidateObservations = [];
        }
    }

    public int CandidateCount { get; private set; }

    public int IncidentComparisonCount { get; private set; }

    public bool CaptureCandidateObservations => _candidateObservations is not null;

    public IReadOnlyList<GraphLayoutCandidateObservation> CandidateObservations => _candidateObservations
        ?? (IReadOnlyList<GraphLayoutCandidateObservation>)Array.Empty<GraphLayoutCandidateObservation>();

    public void RecordCandidate()
    {
        CandidateCount++;
    }

    public void RecordIncidentComparison()
    {
        IncidentComparisonCount++;
    }

    public void RecordCandidateObservation(
        int incidentBefore,
        int incidentAfter,
        int fullBefore,
        int fullAfter,
        bool accepted)
    {
        _candidateObservations?.Add(
            new GraphLayoutCandidateObservation(
                incidentBefore,
                incidentAfter,
                fullBefore,
                fullAfter,
                accepted));
    }
}

internal sealed record GraphLayoutCandidateObservation(
    int IncidentBefore,
    int IncidentAfter,
    int FullBefore,
    int FullAfter,
    bool Accepted);
