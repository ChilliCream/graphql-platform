using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Tui.Theming;
using ChilliCream.Nitro.CommandLine.Tui.Tree;
using ChilliCream.Nitro.CommandLine.Tui.Widgets;
using Spectre.Console.Rendering;

namespace ChilliCream.Nitro.CommandLine.Tui.Graph.TreeView;

/// <summary>
/// Projects a workspace graph into a navigable parent-child tree. The tree
/// hides terminal tasks and adapts its initial epic expansion to graph size.
/// </summary>
internal sealed class GraphTreeView
{
    private const string RootId = "__graph_root__";

    private readonly Viewport _viewport = new(0, 0);
    private readonly HashSet<string> _collapsedEpicIds = new(StringComparer.Ordinal);
    private HashSet<string> _matchIds = new(StringComparer.Ordinal);
    private GraphModel _model;
    private IReadOnlyList<GraphTreeRow> _rows = [];
    private string? _selectedTaskId;

    /// <summary>
    /// Creates the tree projection for <paramref name="model"/>. A null
    /// collapsed-epic set selects the adaptive opening state.
    /// </summary>
    public GraphTreeView(GraphModel model, IReadOnlySet<string>? collapsedEpicIds = null)
    {
        ArgumentNullException.ThrowIfNull(model);

        _model = model;
        SetCollapsedEpics(collapsedEpicIds);
        RebuildRows();
        _selectedTaskId = _rows.FirstOrDefault(t => !t.IsRoot)?.TaskId;
        RebuildRows();
    }

    /// <summary>
    /// The visible hierarchy rows, including the virtual root as the first row.
    /// </summary>
    public IReadOnlyList<GraphTreeRow> Rows => _rows;

    /// <summary>
    /// The current task selection, or <see langword="null"/> when no task is visible.
    /// </summary>
    public string? SelectedTaskId => _selectedTaskId;

    /// <summary>
    /// Replaces the graph while retaining manual expansion choices that still
    /// name visible epics.
    /// </summary>
    public void SetModel(GraphModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        _model = model;
        var visibleEpicIds = VisibleNodes()
            .Where(t => t.IsEpic)
            .Select(t => t.Id)
            .ToHashSet(StringComparer.Ordinal);
        _collapsedEpicIds.IntersectWith(visibleEpicIds);
        RebuildRows();

        if (_selectedTaskId is not null && !_model.Nodes.Any(t => t.Id == _selectedTaskId))
        {
            _selectedTaskId = _rows.FirstOrDefault(t => !t.IsRoot)?.TaskId;
            RebuildRows();
        }
    }

    /// <summary>
    /// Marks task ids matched by a search without changing the hierarchy's
    /// expansion state.
    /// </summary>
    public void SetMatchIds(IEnumerable<string>? taskIds)
    {
        _matchIds = taskIds is null
            ? new HashSet<string>(StringComparer.Ordinal)
            : taskIds.ToHashSet(StringComparer.Ordinal);
        RebuildRows();
    }

    /// <summary>
    /// Selects a task without expanding any collapsed epic that contains it.
    /// </summary>
    public void SelectTask(string? taskId)
    {
        _selectedTaskId = taskId;
        RebuildRows();
    }

    /// <summary>
    /// Moves the cursor among visible task rows by <paramref name="delta"/>.
    /// </summary>
    public void MoveSelection(int delta)
    {
        var taskRows = _rows.Where(t => !t.IsRoot).ToArray();

        if (taskRows.Length == 0)
        {
            _selectedTaskId = null;
            return;
        }

        var currentIndex = Array.FindIndex(taskRows, t => t.TaskId == _selectedTaskId);
        var nextIndex = currentIndex < 0 ? 0 : Math.Clamp(currentIndex + delta, 0, taskRows.Length - 1);
        _selectedTaskId = taskRows[nextIndex].TaskId;
        RebuildRows();
    }

    /// <summary>
    /// Collapses the selected epic when it has visible hierarchy children.
    /// </summary>
    public void CollapseSelected()
    {
        var selected = _rows.FirstOrDefault(t => t.TaskId == _selectedTaskId);

        if (selected is { Task.IsEpic: true, HasChildren: true, IsExpanded: true })
        {
            _collapsedEpicIds.Add(selected.Task.Id);
            RebuildRows();
        }
    }

    /// <summary>
    /// Expands the selected collapsed epic.
    /// </summary>
    public void ExpandSelected()
    {
        var selected = _rows.FirstOrDefault(t => t.TaskId == _selectedTaskId);

        if (selected is { Task.IsEpic: true, HasChildren: true, IsExpanded: false })
        {
            _collapsedEpicIds.Remove(selected.Task.Id);
            RebuildRows();
        }
    }

    /// <summary>
    /// Renders the current viewport of tree rows at the requested dimensions.
    /// </summary>
    public IRenderable Render(int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            return new Markup(string.Empty);
        }

        _viewport.Update(_rows.Count, height);
        _viewport.EnsureVisible(IndexOfSelectedRow());
        var (start, count) = _viewport.Slice();
        var lines = new List<IRenderable>(count);

        for (var index = 0; index < count; index++)
        {
            lines.Add(new Markup(RenderRow(_rows[start + index], width)));
        }

        return new Rows(lines);
    }

    private void SetCollapsedEpics(IReadOnlySet<string>? collapsedEpicIds)
    {
        if (collapsedEpicIds is not null)
        {
            _collapsedEpicIds.UnionWith(collapsedEpicIds);
            return;
        }

        var visibleNodes = VisibleNodes();

        if (visibleNodes.Count > GraphReductionOptions.AdaptiveCollapseThreshold)
        {
            _collapsedEpicIds.UnionWith(visibleNodes.Where(t => t.IsEpic).Select(t => t.Id));
        }
    }

    private IReadOnlyList<GraphNode> VisibleNodes()
        => _model.Nodes.Where(t => !TaskStates.IsTerminal(t.Status)).ToArray();

    private void RebuildRows()
    {
        var visibleNodes = VisibleNodes();
        var visibleIds = visibleNodes.Select(t => t.Id).ToHashSet(StringComparer.Ordinal);
        var visibleEdges = _model.Edges
            .Where(t => visibleIds.Contains(t.FromId) && visibleIds.Contains(t.ToId))
            .ToArray();
        var hierarchy = GraphHierarchy.Build(new GraphModel(visibleNodes, visibleEdges));
        var rows = new List<GraphTreeRow>();
        var descendantsById = BuildDescendantMatchCounts(hierarchy);

        Flatten(hierarchy, 0, true, [], rows);

        var rowIds = rows.Where(t => t.TaskId is not null).Select(t => t.TaskId!).ToHashSet(StringComparer.Ordinal);
        var blockingEdges = visibleEdges
            .Where(t => t.Kind == GraphEdgeKind.Blocks && rowIds.Contains(t.FromId) && rowIds.Contains(t.ToId))
            .ToArray();
        _rows = rows.Select(row => AddDependencyState(row, blockingEdges)).ToArray();

        return;

        void Flatten(
            GraphHierarchyNode node,
            int depth,
            bool isLastChild,
            IReadOnlyList<bool> ancestorIsLastChild,
            List<GraphTreeRow> destination)
        {
            var isRoot = node.IsRoot;
            var task = node.Task;
            var hasChildren = node.Children.Count > 0;
            var isExpanded = isRoot || task is not { IsEpic: true } || !_collapsedEpicIds.Contains(task.Id);
            var containedMatchCount = task is { IsEpic: true } && !isExpanded
                ? descendantsById.GetValueOrDefault(task.Id)
                : 0;
            var connector = new TreeNodeRow
            {
                TaskId = task?.Id ?? RootId,
                Depth = depth,
                IsLastChild = isLastChild,
                AncestorIsLastChild = ancestorIsLastChild
            };

            destination.Add(new GraphTreeRow
            {
                Task = task,
                Connector = connector,
                HasChildren = hasChildren,
                IsExpanded = isExpanded,
                ContainedMatchCount = containedMatchCount,
                IsSelected = task?.Id == _selectedTaskId
            });

            if (!isExpanded)
            {
                return;
            }

            var childAncestors = depth == 0 ? [] : Append(ancestorIsLastChild, isLastChild);

            for (var index = 0; index < node.Children.Count; index++)
            {
                Flatten(
                    node.Children[index],
                    depth + 1,
                    index == node.Children.Count - 1,
                    childAncestors,
                    destination);
            }
        }
    }

    private Dictionary<string, int> BuildDescendantMatchCounts(GraphHierarchyNode root)
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        CountMatches(root);
        return counts;

        int CountMatches(GraphHierarchyNode node)
        {
            var count = node.Task is not null && _matchIds.Contains(node.Task.Id) ? 1 : 0;

            foreach (var child in node.Children)
            {
                count += CountMatches(child);
            }

            if (node.Task is not null)
            {
                counts[node.Task.Id] = count - (_matchIds.Contains(node.Task.Id) ? 1 : 0);
            }

            return count;
        }
    }

    private GraphTreeRow AddDependencyState(GraphTreeRow row, IReadOnlyList<GraphEdge> blockingEdges)
    {
        if (row.TaskId is not { } taskId)
        {
            return row;
        }

        var blockedByCount = blockingEdges.Count(t => t.ToId == taskId);
        var blocksCount = blockingEdges.Count(t => t.FromId == taskId);
        var related = _selectedTaskId is not null
            && taskId != _selectedTaskId
            && blockingEdges.Any(t =>
                (t.FromId == _selectedTaskId && t.ToId == taskId)
                || (t.ToId == _selectedTaskId && t.FromId == taskId));

        return row with
        {
            BlockedByCount = blockedByCount,
            BlocksCount = blocksCount,
            IsRelatedToSelection = related
        };
    }

    private int IndexOfSelectedRow()
    {
        for (var index = 0; index < _rows.Count; index++)
        {
            if (_rows[index].TaskId == _selectedTaskId)
            {
                return index;
            }
        }

        return 0;
    }

    private static IReadOnlyList<bool> Append(IReadOnlyList<bool> values, bool value)
    {
        var result = new bool[values.Count + 1];

        for (var index = 0; index < values.Count; index++)
        {
            result[index] = values[index];
        }

        result[^1] = value;
        return result;
    }

    private static string RenderRow(GraphTreeRow row, int width)
    {
        var text = row.IsRoot ? "Root" : RenderTask(row);
        var connector = row.Connector.BuildConnector();
        var line = Markup.Escape(connector) + text;

        if (row.IsSelected || row.IsRelatedToSelection)
        {
            line = Stylize(ThemeTokens.GetStyle("selection.highlight").ToMarkup(), line);
        }

        return TruncateMarkup(line, width);
    }

    private static string RenderTask(GraphTreeRow row)
    {
        var task = row.Task!;
        var fold = row.HasChildren ? (row.IsExpanded ? "▾ " : "▸ ") : "  ";
        var badges = $"  blocked by {row.BlockedByCount} / blocks {row.BlocksCount}";

        if (row.ContainedMatchCount > 0)
        {
            badges += $"  {row.ContainedMatchCount} hits";
        }

        return $"{Markup.Escape(fold)}{TaskGlyphs.StatusMarkup(task.Status)} {TaskGlyphs.TypeCodeMarkup(task.Type)} "
            + $"{Markup.Escape(task.Title)} {Stylize(ThemeTokens.GetStyle("footer.key").ToMarkup(), Markup.Escape(task.Id))}"
            + Markup.Escape(badges);
    }

    private static string Stylize(string styleMarkup, string content)
        => styleMarkup.Length == 0 ? content : $"[{styleMarkup}]{content}[/]";

    private static string TruncateMarkup(string markup, int width)
    {
        var plain = Markup.Remove(markup);

        if (plain.Length <= width)
        {
            return markup;
        }

        return Markup.Escape(width == 1 ? "…" : string.Concat(plain.AsSpan(0, width - 1), "…"));
    }
}
