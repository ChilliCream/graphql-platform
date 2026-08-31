using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Tui.Graph.CanvasView;
using ChilliCream.Nitro.CommandLine.Tui.Graph.TreeView;
using ChilliCream.Nitro.CommandLine.Tui.Input;
using ChilliCream.Nitro.CommandLine.Tui.Shell;
using Spectre.Console.Rendering;
using CursorDirection = ChilliCream.Nitro.CommandLine.Tui.Input.CursorDirection;

namespace ChilliCream.Nitro.CommandLine.Tui.Graph;

/// <summary>
/// Hosts the tree and canvas projections of the workspace task graph with a
/// shared selection, visibility state, and epic collapse state.
/// </summary>
internal sealed class GraphMode : ITuiMode
{
    private static readonly GraphModel s_emptyModel = new([], []);
    private static readonly HashSet<string> s_emptySet = new(StringComparer.Ordinal);

    private readonly GraphDataLoader _loader;
    private readonly HashSet<string> _collapsedEpicIds = new(StringComparer.Ordinal);

    private GraphModel _sourceModel = s_emptyModel;
    private GraphTreeView _treeView = new(s_emptyModel, s_emptySet);
    private readonly GraphCanvasView _canvasView = new(s_emptyModel);
    private string? _selectedTaskId;
    private bool _hasLoaded;
    private bool _showCanvas;
    private bool _hideClosed = true;

    public GraphMode(GraphDataLoader loader)
    {
        ArgumentNullException.ThrowIfNull(loader);
        _loader = loader;
    }

    /// <inheritdoc />
    public KeyMap? KeyMap { get; } = GraphKeyMap.CreateDefault();

    /// <inheritdoc />
    public string? SelectedTaskId => _selectedTaskId;

    /// <summary>
    /// Whether the canvas projection is active instead of the opening tree projection.
    /// </summary>
    public bool IsCanvasActive => _showCanvas;

    /// <summary>
    /// Whether terminal tasks are currently hidden from both projections.
    /// </summary>
    public bool HideClosed => _hideClosed;

    /// <summary>
    /// The tree projection retained by this mode.
    /// </summary>
    internal GraphTreeView TreeView => _treeView;

    /// <summary>
    /// The canvas projection retained by this mode.
    /// </summary>
    internal GraphCanvasView CanvasView => _canvasView;

    /// <summary>
    /// The epic ids currently represented as collapsed super-nodes.
    /// </summary>
    internal IReadOnlySet<string> CollapsedEpicIds => _collapsedEpicIds;

    /// <inheritdoc />
    public void OnEnter() => RefreshBlocking();

    /// <inheritdoc />
    public void OnResize(int width, int height)
    {
    }

    /// <inheritdoc />
    public IReadOnlyList<TuiMessage> Handle(TuiMessage message)
    {
        switch (message)
        {
            case TuiMessage.RefreshRequested:
                RefreshBlocking();
                break;

            case TuiMessage.MoveCursor(var direction):
                Move(direction);
                break;

            case TuiMessage.ToggleGraphProjection:
                ToggleProjection();
                break;

            case TuiMessage.ToggleGraphCompact:
                _canvasView.ToggleCompact();
                break;

            case TuiMessage.ToggleGraphParentChild:
                _canvasView.ToggleParentChild();
                break;

            case TuiMessage.ToggleGraphClosed:
                ToggleClosed();
                break;

            case TuiMessage.CollapseSelectedGraphEpic:
                CollapseSelected();
                break;

            case TuiMessage.ExpandSelectedGraphEpic:
                ExpandSelected();
                break;

            case TuiMessage.CollapseAllGraphEpics:
                CollapseAll();
                break;

            case TuiMessage.ExpandAllGraphEpics:
                ExpandAll();
                break;

            case TuiMessage.CopySelectedId:
                return _selectedTaskId is null
                    ? [new TuiMessage.ShowToast("No task selected.", ToastStyle.Warn)]
                    : [new TuiMessage.ShowToast(_selectedTaskId, ToastStyle.Info)];
        }

        return [];
    }

    /// <inheritdoc />
    public IRenderable Render(int width, int height)
        => _showCanvas
            ? _canvasView.Render(width, height)
            : _treeView.Render(width, height);

    /// <inheritdoc />
    public void SelectTask(string id)
    {
        ArgumentNullException.ThrowIfNull(id);

        if (!_hasLoaded)
        {
            _selectedTaskId = id;
            _treeView.SelectTask(id);
            _canvasView.SelectTask(id);
            return;
        }

        SelectVisibleTask(ResolveVisibleSelection(id));
    }

    private void RefreshBlocking()
    {
        var requestedSelection = _selectedTaskId;
        _sourceModel = _loader.LoadAsync(CancellationToken.None).GetAwaiter().GetResult();

        var visibleEpicIds = _sourceModel.Nodes
            .Where(IsVisible)
            .Where(t => t.IsEpic)
            .Select(t => t.Id)
            .ToHashSet(StringComparer.Ordinal);

        if (!_hasLoaded)
        {
            var visibleNodeCount = _sourceModel.Nodes.Count(IsVisible);
            if (visibleNodeCount > GraphReductionOptions.AdaptiveCollapseThreshold)
            {
                _collapsedEpicIds.UnionWith(visibleEpicIds);
            }

            _treeView = new GraphTreeView(_sourceModel, _collapsedEpicIds);
            _treeView.SetHideClosed(_hideClosed);
            _hasLoaded = true;
        }
        else
        {
            _collapsedEpicIds.IntersectWith(visibleEpicIds);
            _treeView.SetModel(_sourceModel);
        }

        var reduced = Reduce();
        _canvasView.SetModel(reduced);
        SelectVisibleTask(ResolveVisibleSelection(requestedSelection, reduced));
    }

    private void ToggleProjection()
    {
        SynchronizeSelectionFromActiveProjection();
        _showCanvas = !_showCanvas;
        SelectVisibleTask(_selectedTaskId);
    }

    private void ToggleClosed()
    {
        _hideClosed = !_hideClosed;
        _treeView.SetHideClosed(_hideClosed);
        var reduced = Reduce();
        _canvasView.SetModel(reduced);
        SelectVisibleTask(ResolveVisibleSelection(_selectedTaskId, reduced));
    }

    private void Move(CursorDirection direction)
    {
        if (_showCanvas)
        {
            _ = direction switch
            {
                CursorDirection.Left => _canvasView.MoveLeft(),
                CursorDirection.Right => _canvasView.MoveRight(),
                CursorDirection.Up => _canvasView.MoveUp(),
                CursorDirection.Down => _canvasView.MoveDown(),
                _ => false
            };
            _selectedTaskId = _canvasView.SelectedTaskId;
            _treeView.SelectTask(_selectedTaskId);
            return;
        }

        switch (direction)
        {
            case CursorDirection.Up:
                _treeView.MoveSelection(-1);
                SelectVisibleTask(_treeView.SelectedTaskId);
                break;

            case CursorDirection.Down:
                _treeView.MoveSelection(1);
                SelectVisibleTask(_treeView.SelectedTaskId);
                break;

            case CursorDirection.Left:
                CollapseSelected();
                break;

            case CursorDirection.Right:
                ExpandSelected();
                break;
        }
    }

    private void CollapseSelected()
    {
        if (SelectedEpic() is not { } epic)
        {
            return;
        }

        _collapsedEpicIds.Add(epic.Id);
        _treeView.SelectTask(epic.Id);
        _treeView.CollapseSelected();
        UpdateCanvasModel(epic.Id);
    }

    private void ExpandSelected()
    {
        if (SelectedEpic() is not { } epic)
        {
            return;
        }

        _collapsedEpicIds.Remove(epic.Id);
        _treeView.SelectTask(epic.Id);
        _treeView.ExpandSelected();
        UpdateCanvasModel(epic.Id);
    }

    private void CollapseAll()
    {
        _collapsedEpicIds.Clear();
        _collapsedEpicIds.UnionWith(_sourceModel.Nodes.Where(IsVisible).Where(t => t.IsEpic).Select(t => t.Id));
        RecreateTree();
        UpdateCanvasModel(_selectedTaskId);
    }

    private void ExpandAll()
    {
        _collapsedEpicIds.Clear();
        RecreateTree();
        UpdateCanvasModel(_selectedTaskId);
    }

    private void RecreateTree()
    {
        _treeView = new GraphTreeView(_sourceModel, _collapsedEpicIds);
        _treeView.SetHideClosed(_hideClosed);
    }

    private void UpdateCanvasModel(string? requestedSelection)
    {
        var reduced = Reduce();
        _canvasView.SetModel(reduced);
        SelectVisibleTask(ResolveVisibleSelection(requestedSelection, reduced));
    }

    private GraphModel Reduce()
        => GraphReducer.Reduce(
            _sourceModel,
            new GraphReductionOptions
            {
                HideClosed = _hideClosed,
                CollapsedEpicIds = _collapsedEpicIds
            });

    private GraphNode? SelectedEpic()
        => _selectedTaskId is null
            ? null
            : _sourceModel.Nodes.FirstOrDefault(t => t.Id == _selectedTaskId && t.IsEpic);

    private bool IsVisible(GraphNode node)
        => !_hideClosed || !TaskStates.IsTerminal(node.Status);

    private string? ResolveVisibleSelection(string? requestedSelection)
        => ResolveVisibleSelection(requestedSelection, Reduce());

    private string? ResolveVisibleSelection(string? requestedSelection, GraphModel reduced)
    {
        if (requestedSelection is not null && reduced.Nodes.Any(t => t.Id == requestedSelection))
        {
            return requestedSelection;
        }

        if (requestedSelection is not null)
        {
            var representativeId = FindCollapsedRepresentative(requestedSelection);
            if (reduced.Nodes.Any(t => t.Id == representativeId))
            {
                return representativeId;
            }
        }

        return reduced.Nodes.FirstOrDefault()?.Id;
    }

    private string FindCollapsedRepresentative(string id)
    {
        var parentByChild = _sourceModel.Edges
            .Where(t => t.Kind == GraphEdgeKind.ParentChild)
            .OrderBy(t => t.FromId, StringComparer.Ordinal)
            .ThenBy(t => t.ToId, StringComparer.Ordinal)
            .GroupBy(t => t.ToId, StringComparer.Ordinal)
            .ToDictionary(t => t.Key, t => t.First().FromId, StringComparer.Ordinal);
        var current = id;
        var representative = id;
        var visited = new HashSet<string>(StringComparer.Ordinal);

        while (visited.Add(current) && parentByChild.TryGetValue(current, out var parentId))
        {
            current = parentId;
            if (_collapsedEpicIds.Contains(current))
            {
                representative = current;
            }
        }

        return representative;
    }

    private void SynchronizeSelectionFromActiveProjection()
    {
        _selectedTaskId = _showCanvas
            ? _canvasView.SelectedTaskId
            : _treeView.SelectedTaskId;
    }

    private void SelectVisibleTask(string? taskId)
    {
        _selectedTaskId = taskId;
        _treeView.SelectTask(taskId);
        _canvasView.SelectTask(taskId);
    }
}
