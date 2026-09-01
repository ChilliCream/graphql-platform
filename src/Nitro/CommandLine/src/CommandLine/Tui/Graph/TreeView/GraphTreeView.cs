using System.Text;
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
    private bool _hideClosed = true;

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
    /// Sets whether terminal tasks are excluded from this projection.
    /// </summary>
    public void SetHideClosed(bool hideClosed)
    {
        if (_hideClosed == hideClosed)
        {
            return;
        }

        _hideClosed = hideClosed;
        RebuildRows();
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
    public bool CollapseSelected()
    {
        var selected = _rows.FirstOrDefault(t => t.TaskId == _selectedTaskId);

        if (selected is { Task.IsEpic: true, HasChildren: true, IsExpanded: true })
        {
            _collapsedEpicIds.Add(selected.Task.Id);
            RebuildRows();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Expands the selected collapsed epic.
    /// </summary>
    public bool ExpandSelected()
    {
        var selected = _rows.FirstOrDefault(t => t.TaskId == _selectedTaskId);

        if (selected is { Task.IsEpic: true, HasChildren: true, IsExpanded: false })
        {
            _collapsedEpicIds.Remove(selected.Task.Id);
            RebuildRows();
            return true;
        }

        return false;
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
        => _hideClosed
            ? _model.Nodes.Where(t => !TaskStates.IsTerminal(t.Status)).ToArray()
            : _model.Nodes;

    private void RebuildRows()
    {
        var visibleNodes = VisibleNodes();
        var visibleIds = visibleNodes.Select(t => t.Id).ToHashSet(StringComparer.Ordinal);
        var visibleEdges = _model.Edges
            .Where(t => visibleIds.Contains(t.FromId) && visibleIds.Contains(t.ToId))
            .ToArray();
        var hierarchy = GraphHierarchy.Build(new GraphModel(visibleNodes, visibleEdges));
        var rows = new List<GraphTreeRow>();
        var descendantsById = BuildDescendantCounts(hierarchy, _matchIds);
        var blockingEdges = visibleEdges.Where(t => t.Kind == GraphEdgeKind.Blocks).ToArray();
        var selectedBlockerIds = new HashSet<string>(StringComparer.Ordinal);
        var selectedDependentIds = new HashSet<string>(StringComparer.Ordinal);

        if (_selectedTaskId is not null)
        {
            foreach (var edge in blockingEdges)
            {
                if (edge.ToId == _selectedTaskId)
                {
                    selectedBlockerIds.Add(edge.FromId);
                }

                if (edge.FromId == _selectedTaskId)
                {
                    selectedDependentIds.Add(edge.ToId);
                }
            }
        }

        var selectedRelationshipIds = new HashSet<string>(selectedBlockerIds, StringComparer.Ordinal);
        selectedRelationshipIds.UnionWith(selectedDependentIds);
        var descendantRelationshipCounts = BuildDescendantCounts(hierarchy, selectedRelationshipIds);

        Flatten(hierarchy, 0, true, [], rows);

        _rows = rows.Select(row => AddDependencyState(row, blockingEdges, selectedRelationshipIds)).ToArray();

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
            var containedRelationshipCount = task is { IsEpic: true } && !isExpanded
                ? descendantRelationshipCounts.GetValueOrDefault(task.Id)
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
                ContainedRelationshipCount = containedRelationshipCount,
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

    private static Dictionary<string, int> BuildDescendantCounts(
        GraphHierarchyNode root,
        IReadOnlySet<string> taskIds)
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        CountMatches(root);
        return counts;

        int CountMatches(GraphHierarchyNode node)
        {
            var count = node.Task is not null && taskIds.Contains(node.Task.Id) ? 1 : 0;

            foreach (var child in node.Children)
            {
                count += CountMatches(child);
            }

            if (node.Task is not null)
            {
                counts[node.Task.Id] = count - (taskIds.Contains(node.Task.Id) ? 1 : 0);
            }

            return count;
        }
    }

    private GraphTreeRow AddDependencyState(
        GraphTreeRow row,
        IReadOnlyList<GraphEdge> blockingEdges,
        IReadOnlySet<string> selectedRelationshipIds)
    {
        if (row.TaskId is not { } taskId)
        {
            return row;
        }

        var blockedByCount = blockingEdges.Count(t => t.ToId == taskId);
        var blocksCount = blockingEdges.Count(t => t.FromId == taskId);
        var related = selectedRelationshipIds.Contains(taskId) || row.ContainedRelationshipCount > 0;

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
        var connector = row.Connector.BuildConnector();
        var text = row.IsRoot ? "Root" : RenderTask(row, width - MeasureWidth(connector));
        var line = Markup.Escape(connector) + text;

        if (row.IsSelected || row.IsRelatedToSelection)
        {
            line = Stylize(ThemeTokens.GetStyle("selection.highlight").ToMarkup(), line);
        }

        return line;
    }

    private static string RenderTask(GraphTreeRow row, int width)
    {
        var task = row.Task!;
        var fold = row.HasChildren ? (row.IsExpanded ? "▾ " : "▸ ") : "  ";
        var badges = $"  blocked by {row.BlockedByCount} / blocks {row.BlocksCount}";

        if (row.ContainedMatchCount > 0)
        {
            badges += $"  {row.ContainedMatchCount} hits";
        }

        if (row.ContainedRelationshipCount > 0)
        {
            badges += $"  {row.ContainedRelationshipCount} related";
        }

        var prefix = $"{fold}{TaskGlyphs.Status(task.Status)} [{TaskGlyphs.TypeCode(task.Type)}] ";
        var suffix = $" {task.Id}{badges}";
        var title = Truncate(task.Title, width - MeasureWidth(prefix) - MeasureWidth(suffix));

        return $"{Markup.Escape(fold)}{TaskGlyphs.StatusMarkup(task.Status)} {TaskGlyphs.TypeCodeMarkup(task.Type)} "
            + $"{Markup.Escape(title)} {Stylize(ThemeTokens.GetStyle("footer.key").ToMarkup(), Markup.Escape(task.Id))}"
            + Markup.Escape(badges);
    }

    private static string Stylize(string styleMarkup, string content)
        => styleMarkup.Length == 0 ? content : $"[{styleMarkup}]{content}[/]";

    private static string Truncate(string value, int width)
    {
        if (width <= 0)
        {
            return string.Empty;
        }

        if (MeasureWidth(value) <= width)
        {
            return value;
        }

        if (width == 1)
        {
            return "…";
        }

        var budget = width - 1;
        var used = 0;
        var builder = new StringBuilder();

        foreach (var rune in value.EnumerateRunes())
        {
            var runeWidth = GetRuneWidth(rune);

            if (used + runeWidth > budget)
            {
                break;
            }

            builder.Append(rune);
            used += runeWidth;
        }

        return builder.Append('…').ToString();
    }

    private static int MeasureWidth(string value)
    {
        var width = 0;

        foreach (var rune in value.EnumerateRunes())
        {
            width += GetRuneWidth(rune);
        }

        return width;
    }

    private static int GetRuneWidth(Rune rune)
    {
        var value = rune.Value;

        return value switch
        {
            >= 0x1100 and <= 0x115F => 2,
            >= 0x2E80 and <= 0x303E => 2,
            >= 0x3041 and <= 0x33FF => 2,
            >= 0x3400 and <= 0x4DBF => 2,
            >= 0x4E00 and <= 0x9FFF => 2,
            >= 0xA000 and <= 0xA4CF => 2,
            >= 0xAC00 and <= 0xD7A3 => 2,
            >= 0xF900 and <= 0xFAFF => 2,
            >= 0xFE30 and <= 0xFE4F => 2,
            >= 0xFF00 and <= 0xFFE6 => 2,
            >= 0x1F1E6 and <= 0x1F1FF => 2,
            >= 0x1F200 and <= 0x1F2FF => 2,
            >= 0x1F300 and <= 0x1F5FF => 2,
            >= 0x1F600 and <= 0x1F64F => 2,
            >= 0x1F680 and <= 0x1F6FF => 2,
            >= 0x1F900 and <= 0x1F9FF => 2,
            >= 0x1FA70 and <= 0x1FAFF => 2,
            >= 0x20000 and <= 0x2FFFD => 2,
            >= 0x30000 and <= 0x3FFFD => 2,
            _ => 1
        };
    }
}
