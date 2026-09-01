using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Tui.Graph.CanvasView;
using ChilliCream.Nitro.CommandLine.Tui.Graph.TreeView;
using ChilliCream.Nitro.CommandLine.Tui.Input;
using ChilliCream.Nitro.CommandLine.Tui.Shell;
using ChilliCream.Nitro.CommandLine.Tui.Widgets.Form;
using Spectre.Console.Rendering;
using CursorDirection = ChilliCream.Nitro.CommandLine.Tui.Input.CursorDirection;

namespace ChilliCream.Nitro.CommandLine.Tui.Graph;

/// <summary>
/// Hosts the tree and canvas projections of the workspace task graph with a
/// shared selection, visibility state, and epic collapse state.
/// </summary>
internal sealed class GraphMode : ITuiMode, IRawKeyCapturingMode
{
    private static readonly GraphModel s_emptyModel = new([], []);
    private static readonly HashSet<string> s_emptySet = new(StringComparer.Ordinal);

    private readonly GraphDataLoader _loader;
    private readonly HashSet<string> _collapsedEpicIds = new(StringComparer.Ordinal);

    private GraphModel _sourceModel = s_emptyModel;
    private GraphModel _filteredTreeModel = s_emptyModel;
    private GraphModel _canvasModel = s_emptyModel;
    private GraphTreeView _treeView = new(s_emptyModel, s_emptySet);
    private readonly GraphCanvasView _canvasView = new(s_emptyModel);
    private string? _selectedTaskId;
    private readonly HashSet<string> _labels = new(StringComparer.Ordinal);
    private readonly HashSet<string> _epicIds = new(StringComparer.Ordinal);
    private readonly HashSet<string> _matchIds = new(StringComparer.Ordinal);
    private readonly HashSet<string> _directMatchIds = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _containedMatchCounts = new(StringComparer.Ordinal);
    private readonly List<string> _cycleMatchIds = [];
    private LineEditor? _searchEditor;
    private GraphFilterForm? _filterForm;
    private int _searchMatchIndex = -1;
    private bool _hasLoaded;
    private bool _treeInitialized;
    private bool _showCanvas;
    private bool _hideClosed = true;
    private GraphSearchProjectionContext _searchContext = new(s_emptyModel, s_emptyModel);

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

    /// <summary>
    /// The number of projection contexts created for the current mode lifetime.
    /// </summary>
    internal int SearchContextBuildCount { get; private set; }

    /// <inheritdoc />
    public bool IsInputCapturing => _searchEditor is not null || _filterForm is not null;

    /// <inheritdoc />
    public IReadOnlyList<KeyHint> CapturingHints => _filterForm is not null
        ? GraphFilterForm.Hints
        : _searchEditor is not null
            ? [new KeyHint("enter", "next hit"), new KeyHint("esc", "close")]
            : [];

    /// <inheritdoc />
    public string? FooterStatus
    {
        get
        {
            if (_searchEditor is null && _labels.Count == 0 && _epicIds.Count == 0)
            {
                return null;
            }

            var search = _searchEditor is null ? null : $"Search: {_searchEditor.Text} ({_matchIds.Count} hits)";
            var filters = _labels.Count == 0 && _epicIds.Count == 0 ? null : FilterNotice();
            return string.Join("; ", new[] { search, filters }.Where(t => t is not null));
        }
    }

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

            case TuiMessage.FocusSearchRequested:
                OpenSearch();
                break;

            case TuiMessage.FilterGraphRequested:
                OpenFilterForm();
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
    public IReadOnlyList<TuiMessage> HandleRawKey(ConsoleKeyInfo info)
    {
        if (_filterForm is not null)
        {
            return HandleFilterKey(info);
        }

        if (_searchEditor is null)
        {
            return [];
        }

        if (info.Key == ConsoleKey.Escape)
        {
            _searchEditor = null;
            _matchIds.Clear();
            _searchMatchIndex = -1;
            ApplyMatchIds();
            return [];
        }

        if (info.Key == ConsoleKey.Enter)
        {
            JumpToNextMatch();
            return [];
        }

        if (_searchEditor.HandleKey(info))
        {
            UpdateMatches();
        }

        return [];
    }

    /// <inheritdoc />
    public IRenderable Render(int width, int height)
    {
        if (_filterForm is not null)
        {
            return _filterForm.Render(width, height);
        }

        return RenderProjection(width, height);
    }

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

        var sourceEpicIds = _sourceModel.Nodes
            .Where(t => t.IsEpic)
            .Select(t => t.Id)
            .ToHashSet(StringComparer.Ordinal);

        if (!_hasLoaded)
        {
            var visibleNodeCount = _sourceModel.Nodes.Count(IsVisible);
            if (visibleNodeCount > GraphReductionOptions.AdaptiveCollapseThreshold)
            {
                _collapsedEpicIds.UnionWith(sourceEpicIds);
            }

            _hasLoaded = true;
        }
        else
        {
            _collapsedEpicIds.IntersectWith(sourceEpicIds);
        }

        RecreateTree();
        var reduced = Reduce();
        _canvasModel = reduced;
        _canvasView.SetModel(reduced);
        RebuildSearchContext();
        SelectVisibleTask(ResolveVisibleSelection(requestedSelection, reduced));
        UpdateMatches();
    }

    private void ToggleProjection()
    {
        SynchronizeSelectionFromActiveProjection();
        _showCanvas = !_showCanvas;

        if (_showCanvas)
        {
            _selectedTaskId = ResolveVisibleSelection(_selectedTaskId, _canvasModel);
            _canvasView.SelectTask(_selectedTaskId);
        }
        else
        {
            _treeView.SelectTask(_selectedTaskId);
        }
    }

    private void ToggleClosed()
    {
        _hideClosed = !_hideClosed;
        RecreateTree();
        var reduced = Reduce();
        _canvasModel = reduced;
        _canvasView.SetModel(reduced);
        RebuildSearchContext();
        SelectVisibleTask(ResolveVisibleSelection(_selectedTaskId, reduced));
        ApplyMatchIds();
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
            return;
        }

        switch (direction)
        {
            case CursorDirection.Up:
                _treeView.MoveSelection(-1);
                _selectedTaskId = _treeView.SelectedTaskId;
                _canvasView.SelectTask(_selectedTaskId);
                break;

            case CursorDirection.Down:
                _treeView.MoveSelection(1);
                _selectedTaskId = _treeView.SelectedTaskId;
                _canvasView.SelectTask(_selectedTaskId);
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
        if (_showCanvas)
        {
            CollapseCanvasSelected();
            return;
        }

        if (_treeView.CollapseSelected())
        {
            var epicId = _treeView.SelectedTaskId!;
            _collapsedEpicIds.Add(epicId);
            UpdateCanvasModel(epicId);
        }
    }

    private void ExpandSelected()
    {
        if (_showCanvas)
        {
            ExpandCanvasSelected();
            return;
        }

        if (_treeView.ExpandSelected())
        {
            var epicId = _treeView.SelectedTaskId!;
            _collapsedEpicIds.Remove(epicId);
            UpdateCanvasModel(epicId);
        }
    }

    private void CollapseAll()
    {
        _collapsedEpicIds.UnionWith(_filteredTreeModel.Nodes.Where(t => t.IsEpic).Select(t => t.Id));
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
        _filteredTreeModel = FilterTreeModel();
        if (_treeInitialized)
        {
            _treeView.SetModel(_filteredTreeModel, _collapsedEpicIds);
        }
        else
        {
            _treeView = new GraphTreeView(_filteredTreeModel, _collapsedEpicIds);
            _treeInitialized = true;
        }

        _treeView.SetHideClosed(false);
        _treeView.SetMatchIds(_matchIds);
    }

    private void UpdateCanvasModel(string? requestedSelection)
    {
        var reduced = Reduce();
        _canvasModel = reduced;
        _canvasView.SetModel(reduced);
        RebuildSearchContext();
        ApplyMatchIds();
        SelectVisibleTask(ResolveVisibleSelection(requestedSelection, reduced));
    }

    private GraphModel Reduce()
        => GraphReducer.Reduce(
            _sourceModel,
            new GraphReductionOptions
            {
                HideClosed = _hideClosed,
                Labels = _labels,
                EpicIds = _epicIds,
                CollapsedEpicIds = _collapsedEpicIds
            });

    private GraphModel FilterTreeModel()
        => GraphReducer.Filter(
            _sourceModel,
            new GraphReductionOptions { Labels = _labels, EpicIds = _epicIds, HideClosed = _hideClosed });

    private bool IsVisible(GraphNode node)
        => !_hideClosed || !TaskStates.IsTerminal(node.Status);

    private string? ResolveVisibleSelection(string? requestedSelection)
        => ResolveVisibleSelection(requestedSelection, _canvasModel);

    private string? ResolveVisibleSelection(string? requestedSelection, GraphModel reduced)
    {
        if (requestedSelection is not null && reduced.Nodes.Any(t => t.Id == requestedSelection))
        {
            return requestedSelection;
        }

        if (requestedSelection is not null)
        {
            var representativeId = FindCollapsedRepresentative(requestedSelection, reduced);
            if (reduced.Nodes.Any(t => t.Id == representativeId))
            {
                return representativeId;
            }
        }

        return reduced.Nodes.FirstOrDefault()?.Id;
    }

    private string FindCollapsedRepresentative(string id, GraphModel reduced)
    {
        if (ReferenceEquals(reduced, _canvasModel))
        {
            return _searchContext.ResolveRepresentative(id);
        }

        var parentByChild = GraphParentMap.Build(VisibleModel());
        var reducedIds = reduced.Nodes.Select(t => t.Id).ToHashSet(StringComparer.Ordinal);
        var current = id;
        var visited = new HashSet<string>(StringComparer.Ordinal);

        while (true)
        {
            if (reducedIds.Contains(current))
            {
                return current;
            }

            if (!visited.Add(current) || !parentByChild.TryGetValue(current, out var parentId))
            {
                break;
            }

            current = parentId;
        }

        return id;
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

    private void CollapseCanvasSelected()
    {
        var epic = FindContainingEpic(_selectedTaskId);

        if (epic is null)
        {
            return;
        }

        _treeView.SelectTask(epic.Id);

        if (_treeView.CollapseSelected())
        {
            _collapsedEpicIds.Add(epic.Id);
            UpdateCanvasModel(epic.Id);
        }
    }

    private void ExpandCanvasSelected()
    {
        if (_selectedTaskId is not { } epicId
            || !_collapsedEpicIds.Contains(epicId)
            || _sourceModel.Nodes.FirstOrDefault(t => t.Id == epicId) is not { IsEpic: true })
        {
            return;
        }

        _treeView.SelectTask(epicId);

        if (_treeView.ExpandSelected())
        {
            _collapsedEpicIds.Remove(epicId);
            UpdateCanvasModel(epicId);
        }
    }

    private GraphNode? FindContainingEpic(string? taskId)
    {
        if (taskId is null)
        {
            return null;
        }

        var visibleModel = VisibleModel();
        var nodesById = visibleModel.Nodes.ToDictionary(t => t.Id, StringComparer.Ordinal);

        if (!nodesById.TryGetValue(taskId, out var current))
        {
            return null;
        }

        if (current.IsEpic)
        {
            return current;
        }

        var parentByChild = GraphParentMap.Build(visibleModel);
        var visited = new HashSet<string>(StringComparer.Ordinal);

        while (visited.Add(current.Id) && parentByChild.TryGetValue(current.Id, out var parentId))
        {
            current = nodesById[parentId];

            if (current.IsEpic)
            {
                return current;
            }
        }

        return null;
    }

    private GraphModel VisibleModel()
        => GraphReducer.Filter(
            _sourceModel,
            new GraphReductionOptions { HideClosed = _hideClosed, Labels = _labels, EpicIds = _epicIds });

    private void RebuildSearchContext()
    {
        _searchContext = new GraphSearchProjectionContext(_filteredTreeModel, _canvasModel);
        SearchContextBuildCount++;
    }

    private IRenderable RenderProjection(int width, int height)
        => _showCanvas ? _canvasView.Render(width, height) : _treeView.Render(width, height);

    private void OpenSearch()
    {
        _searchEditor ??= new LineEditor();
        UpdateMatches();
    }

    private void UpdateMatches()
    {
        _matchIds.Clear();
        _directMatchIds.Clear();
        _containedMatchCounts.Clear();
        _cycleMatchIds.Clear();
        var seenCycleIds = new HashSet<string>(StringComparer.Ordinal);

        if (_searchEditor is { Text.Length: > 0 } editor)
        {
            foreach (var node in _searchContext.VisibleNodes)
            {
                if (node.Title.Contains(editor.Text, StringComparison.OrdinalIgnoreCase))
                {
                    _matchIds.Add(node.Id);
                    var representativeId = _searchContext.ResolveRepresentative(node.Id);

                    if (representativeId == node.Id)
                    {
                        _directMatchIds.Add(node.Id);
                    }
                    else if (_searchContext.ContainsReducedId(representativeId))
                    {
                        _containedMatchCounts[representativeId] =
                            _containedMatchCounts.GetValueOrDefault(representativeId) + 1;
                    }

                    if (_searchContext.ContainsReducedId(representativeId) && seenCycleIds.Add(representativeId))
                    {
                        _cycleMatchIds.Add(representativeId);
                    }
                }
            }
        }

        _searchMatchIndex = -1;
        ApplyMatchIds();
    }

    private void ApplyMatchIds()
    {
        _treeView.SetMatchIds(_matchIds);
        _canvasView.SetMatchIds(_directMatchIds, _containedMatchCounts);
    }

    private void JumpToNextMatch()
    {
        if (_cycleMatchIds.Count == 0)
        {
            return;
        }

        _searchMatchIndex = (_searchMatchIndex + 1) % _cycleMatchIds.Count;
        SelectVisibleTask(_cycleMatchIds[_searchMatchIndex]);
    }

    private void OpenFilterForm() => _filterForm = new GraphFilterForm(_labels, _epicIds);

    private IReadOnlyList<TuiMessage> HandleFilterKey(ConsoleKeyInfo info)
    {
        var result = _filterForm!.HandleKey(info);

        switch (result)
        {
            case null:
                return [];

            case FormResult.Cancelled:
            case FormResult.ButtonActivated { ButtonId: GraphFilterForm.CancelButtonId }:
                _filterForm = null;
                return [];

            case FormResult.ButtonActivated { ButtonId: GraphFilterForm.ClearButtonId }:
                _filterForm = null;
                ApplyFilters(s_emptySet, s_emptySet);
                return [];

            case FormResult.Submitted:
            case FormResult.ButtonActivated { ButtonId: GraphFilterForm.ApplyButtonId }:
                var labels = _filterForm.Labels;
                var epicIds = _filterForm.EpicIds;
                _filterForm = null;
                ApplyFilters(labels, epicIds);
                return [];

            default:
                return [];
        }
    }

    private void ApplyFilters(IReadOnlySet<string> labels, IReadOnlySet<string> epicIds)
    {
        var requestedSelection = _selectedTaskId;
        _labels.Clear();
        _labels.UnionWith(labels);
        _epicIds.Clear();
        _epicIds.UnionWith(epicIds);
        RecreateTree();
        UpdateCanvasModel(requestedSelection);
        UpdateMatches();
    }

    private string FilterNotice()
    {
        var labels = _labels.Count == 0 ? null : $"labels: {string.Join(", ", _labels.Order(StringComparer.Ordinal))}";
        var epics = _epicIds.Count == 0 ? null : $"epics: {string.Join(", ", _epicIds.Order(StringComparer.Ordinal))}";
        return $"Filters active ({string.Join("; ", new[] { labels, epics }.Where(t => t is not null))})";
    }
}
