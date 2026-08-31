using ChilliCream.Nitro.CommandLine.Tui.Graph.Layout;
using ChilliCream.Nitro.CommandLine.Tui.Graph.Render;
using ChilliCream.Nitro.CommandLine.Tui.Theming;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace ChilliCream.Nitro.CommandLine.Tui.Graph.CanvasView;

/// <summary>
/// Projects a graph model into a selectable, viewport-bound two-dimensional canvas.
/// </summary>
internal sealed class GraphCanvasView
{
    private readonly GraphLayout _layoutEngine = new();
    private readonly GraphEdgeRouter _edgeRouter = new();
    private GraphModel _model;
    private GraphLayoutResult _layout = new([], [], 0, 0);
    private string? _selectedTaskId;
    private string? _cycleOriginId;
    private HorizontalDirection? _cycleDirection;

    public GraphCanvasView(GraphModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        _model = model;
        RebuildLayout();
    }

    /// <summary>
    /// The selected graph task, or <see langword="null"/> when the graph is empty.
    /// </summary>
    public string? SelectedTaskId => _selectedTaskId;

    /// <summary>
    /// Whether nodes render as compact single-line entries.
    /// </summary>
    public bool IsCompact { get; private set; }

    /// <summary>
    /// Whether parent-child relationships are included in the edge overlay.
    /// </summary>
    public bool IncludeParentChild { get; private set; }

    /// <summary>
    /// The current deterministic graph layout.
    /// </summary>
    public GraphLayoutResult Layout => _layout;

    /// <summary>
    /// The canvas window rendered most recently.
    /// </summary>
    public CanvasViewport Viewport { get; private set; }

    /// <summary>
    /// Replaces the graph and retains a selection that still exists.
    /// </summary>
    public void SetModel(GraphModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        _model = model;
        RebuildLayout();
    }

    /// <summary>
    /// Changes the selected task, or clears selection for a null id.
    /// </summary>
    public void SelectTask(string? taskId)
    {
        _selectedTaskId = taskId;
        ClearCycle();
    }

    /// <summary>
    /// Switches between boxed and compact node rendering.
    /// </summary>
    public void ToggleCompact()
    {
        IsCompact = !IsCompact;
        RebuildLayout();
    }

    /// <summary>
    /// Switches the parent-child edge overlay.
    /// </summary>
    public void ToggleParentChild() => IncludeParentChild = !IncludeParentChild;

    /// <summary>
    /// Selects a blocking prerequisite, cycling deterministically among alternatives.
    /// </summary>
    public bool MoveLeft() => MoveHorizontal(HorizontalDirection.Left);

    /// <summary>
    /// Selects a dependent, cycling deterministically among alternatives.
    /// </summary>
    public bool MoveRight() => MoveHorizontal(HorizontalDirection.Right);

    /// <summary>
    /// Selects the preceding task in the current layout layer.
    /// </summary>
    public bool MoveUp() => MoveVertical(-1);

    /// <summary>
    /// Selects the following task in the current layout layer.
    /// </summary>
    public bool MoveDown() => MoveVertical(1);

    /// <summary>
    /// Builds the styled graph buffer with the selected task and its incident edges highlighted.
    /// </summary>
    public GraphRenderResult CreateRenderResult()
    {
        var selectedTaskId = _selectedTaskId;
        var result = _edgeRouter.Route(
            _layout,
            new GraphEdgeRenderOptions
            {
                IncludeParentChild = IncludeParentChild,
                StyleOverride = edge => selectedTaskId is not null
                    && (edge.FromId == selectedTaskId || edge.ToId == selectedTaskId)
                        ? ThemeTokens.GetStyle("selection.highlight")
                        : null
            });
        var nodesById = _model.Nodes.ToDictionary(t => t.Id, StringComparer.Ordinal);

        foreach (var layoutNode in _layout.Nodes)
        {
            if (nodesById.TryGetValue(layoutNode.Id, out var node))
            {
                GraphCanvasNodeRenderer.Render(
                    result.Buffer,
                    layoutNode,
                    node,
                    IsCompact,
                    layoutNode.Id == selectedTaskId);
            }
        }

        return result;
    }

    /// <summary>
    /// Renders a viewport that always contains the selected node.
    /// </summary>
    public IRenderable Render(int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            return new Markup(string.Empty);
        }

        var result = CreateRenderResult();
        var noticeHeight = _model.IsReduced ? 1 : 0;
        var canvasHeight = Math.Max(0, height - noticeHeight);
        Viewport = CreateViewport(result.Buffer, width, canvasHeight);
        var canvas = result.Buffer.Render(Viewport);

        if (!_model.IsReduced)
        {
            return canvas;
        }

        var notice = $"Graph reduced: {_model.HiddenNodeCount} nodes hidden";
        return new Rows(new Markup($"[{ThemeTokens.GetStyle("toast.warn.border").ToMarkup()}]{Markup.Escape(notice)}[/]"), canvas);
    }

    private void RebuildLayout()
    {
        var sizes = _model.Nodes.ToDictionary(
            node => node.Id,
            node => GraphCanvasNodeRenderer.Measure(node, IsCompact),
            StringComparer.Ordinal);
        _layout = _layoutEngine.Layout(_model, sizes, _layout);

        if (_selectedTaskId is null || _layout.FindNode(_selectedTaskId) is null)
        {
            _selectedTaskId = _layout.Nodes.FirstOrDefault()?.Id;
        }

        ClearCycle();
    }

    private bool MoveHorizontal(HorizontalDirection direction)
    {
        if (_selectedTaskId is null)
        {
            return false;
        }

        var originId = _cycleDirection == direction && _cycleOriginId is not null
            ? _cycleOriginId
            : _selectedTaskId;
        var candidates = GetHorizontalCandidates(originId, direction);

        if (candidates.Count == 0)
        {
            ClearCycle();
            return false;
        }

        var currentIndex = _cycleDirection == direction && _cycleOriginId == originId
            ? IndexOf(candidates, _selectedTaskId)
            : -1;
        _selectedTaskId = candidates[(currentIndex + 1) % candidates.Count];
        _cycleOriginId = originId;
        _cycleDirection = direction;
        return true;
    }

    private IReadOnlyList<string> GetHorizontalCandidates(string originId, HorizontalDirection direction)
    {
        var origin = _layout.FindNode(originId);
        if (origin is null)
        {
            return [];
        }

        var ids = _model.Edges
            .Where(edge => edge.Kind == GraphEdgeKind.Blocks)
            .Where(edge => direction == HorizontalDirection.Left ? edge.ToId == originId : edge.FromId == originId)
            .Select(edge => direction == HorizontalDirection.Left ? edge.FromId : edge.ToId)
            .Distinct(StringComparer.Ordinal)
            .Select(id => _layout.FindNode(id))
            .Where(node => node is not null)
            .Cast<GraphLayoutNode>()
            .OrderBy(node => Math.Abs(node.Layer - origin.Layer))
            .ThenBy(node => Math.Abs(node.Y - origin.Y))
            .ThenBy(node => node.Order)
            .ThenBy(node => node.Id, StringComparer.Ordinal)
            .Select(node => node.Id)
            .ToArray();

        return ids;
    }

    private bool MoveVertical(int delta)
    {
        if (_selectedTaskId is null)
        {
            return false;
        }

        var selected = _layout.FindNode(_selectedTaskId);
        if (selected is null)
        {
            return false;
        }

        var layer = _layout.Nodes
            .Where(node => node.Layer == selected.Layer)
            .OrderBy(node => node.Order)
            .ThenBy(node => node.Id, StringComparer.Ordinal)
            .ToArray();
        var index = Array.FindIndex(layer, node => node.Id == _selectedTaskId);
        var nextIndex = Math.Clamp(index + delta, 0, layer.Length - 1);
        ClearCycle();

        if (nextIndex == index)
        {
            return false;
        }

        _selectedTaskId = layer[nextIndex].Id;
        return true;
    }

    private CanvasViewport CreateViewport(CellBuffer buffer, int width, int height)
    {
        var viewportWidth = Math.Min(width, buffer.Width);
        var viewportHeight = Math.Min(height, buffer.Height);
        var selected = _selectedTaskId is null ? null : _layout.FindNode(_selectedTaskId);

        if (selected is null || viewportWidth == 0 || viewportHeight == 0)
        {
            return new CanvasViewport(0, 0, viewportWidth, viewportHeight);
        }

        var maxX = Math.Max(0, buffer.Width - viewportWidth);
        var maxY = Math.Max(0, buffer.Height - viewportHeight);
        var x = Math.Clamp(selected.X + (selected.Width / 2) - (viewportWidth / 2), 0, maxX);
        var y = Math.Clamp(selected.Y + (selected.Height / 2) - (viewportHeight / 2), 0, maxY);
        return new CanvasViewport(x, y, viewportWidth, viewportHeight);
    }

    private void ClearCycle()
    {
        _cycleOriginId = null;
        _cycleDirection = null;
    }

    private static int IndexOf(IReadOnlyList<string> values, string? value)
    {
        for (var index = 0; index < values.Count; index++)
        {
            if (values[index] == value)
            {
                return index;
            }
        }

        return -1;
    }

    private enum HorizontalDirection
    {
        Left,
        Right
    }
}
