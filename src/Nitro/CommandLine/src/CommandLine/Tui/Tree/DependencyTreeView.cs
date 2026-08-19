using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Tui.Input;
using ChilliCream.Nitro.CommandLine.Tui.Shell;
using ChilliCream.Nitro.CommandLine.Tui.Theming;
using ChilliCream.Nitro.CommandLine.Tui.Widgets;
using Spectre.Console.Rendering;
using CursorDirection = ChilliCream.Nitro.CommandLine.Tui.Input.CursorDirection;

namespace ChilliCream.Nitro.CommandLine.Tui.Tree;

/// <summary>
/// The dependency tree explorer <see cref="ITuiMode"/>: renders a task's
/// dependency graph as a windowed, connector-drawn tree, and lets the cursor
/// refocus the tree on any node while remembering the path back.
/// </summary>
/// <remarks>
/// Enter and f both refocus the tree on the cursor node, and Enter is
/// already reachable through the global key table via
/// <see cref="TuiMessage.OpenSelected"/>; <see cref="KeyMap"/> maps f to the
/// same message. m toggles the edge mode, d toggles the direction, and u
/// pops the breadcrumb stack, via <see cref="TuiMessage.ToggleTreeEdgeMode"/>,
/// <see cref="TuiMessage.ToggleTreeDirection"/>, and
/// <see cref="TuiMessage.NavigateTreeBack"/>. Entering the tree on a task
/// selected in another mode, and leaving it, are shell-level concerns: the
/// shell drives <see cref="EnterOnTask"/> directly the same way
/// <c>SearchMode.HandleQueryKey</c> is driven directly for gestures outside
/// the closed <see cref="TuiMessage"/> set.
/// </remarks>
internal sealed class DependencyTreeView : ITuiMode
{
    private readonly ITaskStore _store;
    private readonly Stack<string> _breadcrumbs = new();
    private readonly Viewport _viewport = new(0, 0);

    private Dictionary<string, TaskItem> _tasksById = new();
    private IReadOnlyList<TaskDependency> _edges = [];
    private IReadOnlyList<TreeNodeRow> _rows = [];
    private string _rootId;
    private TreeEdgeMode _edgeMode = TreeEdgeMode.Blocking;
    private TreeDirection _direction = TreeDirection.Up;
    private int _cursorIndex;

    public DependencyTreeView(ITaskStore store, string rootId)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _rootId = rootId ?? throw new ArgumentNullException(nameof(rootId));
    }

    /// <summary>
    /// The id of the tree's current root.
    /// </summary>
    public string RootId => _rootId;

    /// <summary>
    /// Which edges the tree currently follows.
    /// </summary>
    public TreeEdgeMode EdgeMode => _edgeMode;

    /// <summary>
    /// Which way the tree currently traverses edges from its root.
    /// </summary>
    public TreeDirection Direction => _direction;

    /// <summary>
    /// The rows of the tree currently loaded, in display order.
    /// </summary>
    public IReadOnlyList<TreeNodeRow> Rows => _rows;

    /// <summary>
    /// The id of the task at the cursor, or null when the tree is empty.
    /// Detail navigation hooks in through this.
    /// </summary>
    public string? SelectedTaskId
        => _cursorIndex >= 0 && _cursorIndex < _rows.Count ? _rows[_cursorIndex].TaskId : null;

    /// <summary>
    /// Whether <see cref="NavigateBack"/> has a previous root to return to.
    /// </summary>
    public bool CanNavigateBack => _breadcrumbs.Count > 0;

    /// <summary>
    /// f mirrors the global Enter binding, refocusing the tree on the cursor
    /// node. m toggles the edge mode, d toggles the direction, and u pops the
    /// breadcrumb stack.
    /// </summary>
    public KeyMap? KeyMap { get; } = new KeyMap(
    [
        new KeyBinding(
            new KeyChord(ConsoleKey.F, ConsoleModifiers.None, 'f'),
            () => new TuiMessage.OpenSelected()),
        new KeyBinding(
            new KeyChord(ConsoleKey.M, ConsoleModifiers.None, 'm'),
            () => new TuiMessage.ToggleTreeEdgeMode()),
        new KeyBinding(
            new KeyChord(ConsoleKey.D, ConsoleModifiers.None, 'd'),
            () => new TuiMessage.ToggleTreeDirection()),
        new KeyBinding(
            new KeyChord(ConsoleKey.U, ConsoleModifiers.None, 'u'),
            () => new TuiMessage.NavigateTreeBack())
    ]);

    public void OnEnter() => RefreshBlocking();

    public void OnResize(int width, int height)
    {
    }

    public IReadOnlyList<TuiMessage> Handle(TuiMessage message)
    {
        switch (message)
        {
            case TuiMessage.MoveCursor(CursorDirection.Down):
                MoveCursor(1);
                return [];

            case TuiMessage.MoveCursor(CursorDirection.Up):
                MoveCursor(-1);
                return [];

            case TuiMessage.MoveToEdge(var edge):
                MoveToEdge(edge);
                return [];

            case TuiMessage.OpenSelected:
                Refocus();
                return [];

            case TuiMessage.RefreshRequested:
                RefreshBlocking();
                return [];

            case TuiMessage.ToggleTreeEdgeMode:
                ToggleEdgeMode();
                return [];

            case TuiMessage.ToggleTreeDirection:
                ToggleDirection();
                return [];

            case TuiMessage.NavigateTreeBack:
                NavigateBack();
                return [];

            default:
                return [];
        }
    }

    /// <summary>
    /// Resets the tree to a new root and entry state: clears the breadcrumb
    /// stack, resets the cursor, and reloads the graph from the store.
    /// </summary>
    public void EnterOnTask(string taskId)
    {
        ArgumentNullException.ThrowIfNull(taskId);

        _rootId = taskId;
        _breadcrumbs.Clear();
        _cursorIndex = 0;
        RefreshBlocking();
    }

    /// <summary>
    /// Pushes the current root onto the breadcrumb stack and rebuilds the
    /// tree rooted at the cursor node. Has no effect when the tree is empty
    /// or the cursor is already on the root.
    /// </summary>
    public void Refocus()
    {
        if (SelectedTaskId is not { } targetId || targetId == _rootId)
        {
            return;
        }

        _breadcrumbs.Push(_rootId);
        _rootId = targetId;
        _cursorIndex = 0;
        Rebuild();
    }

    /// <summary>
    /// Pops the breadcrumb stack and rebuilds the tree rooted at the popped
    /// task id. Returns whether the stack had an entry to pop.
    /// </summary>
    public bool NavigateBack()
    {
        if (_breadcrumbs.Count == 0)
        {
            return false;
        }

        _rootId = _breadcrumbs.Pop();
        _cursorIndex = 0;
        Rebuild();
        return true;
    }

    /// <summary>
    /// Toggles between the blocking-dependency tree and the parent-child
    /// tree, keeping the cursor on the same task when it is still present.
    /// </summary>
    public void ToggleEdgeMode()
    {
        _edgeMode = _edgeMode == TreeEdgeMode.Blocking ? TreeEdgeMode.ParentChild : TreeEdgeMode.Blocking;
        Rebuild();
    }

    /// <summary>
    /// Toggles between showing what the root depends on and what depends on
    /// the root, keeping the cursor on the same task when it is still
    /// present.
    /// </summary>
    public void ToggleDirection()
    {
        _direction = _direction == TreeDirection.Up ? TreeDirection.Down : TreeDirection.Up;
        Rebuild();
    }

    public IRenderable Render(int width, int height)
    {
        var innerWidth = Math.Max(1, width - 4);
        var innerHeight = Math.Max(1, height - 2);
        var lines = RenderLines(innerWidth, innerHeight);

        return new Panel(lines.Count == 0 ? new Markup(string.Empty) : new Rows(lines))
        {
            Header = new PanelHeader(BuildTitle(innerWidth)),
            Border = BoxBorder.Rounded,
            BorderStyle = ThemeTokens.GetStyle("board.column.border.focused")
        };
    }

    private void MoveCursor(int delta)
    {
        if (_rows.Count == 0)
        {
            return;
        }

        _cursorIndex = Math.Clamp(_cursorIndex + delta, 0, _rows.Count - 1);
        _viewport.EnsureVisible(_cursorIndex);
    }

    private void MoveToEdge(EdgeTarget edge)
    {
        if (_rows.Count == 0)
        {
            return;
        }

        _cursorIndex = edge == EdgeTarget.Top ? 0 : _rows.Count - 1;
        _viewport.EnsureVisible(_cursorIndex);
    }

    private void RefreshBlocking()
    {
        var tasks = _store.QueryTasksAsync(new TaskFilter { IncludeAll = true }, CancellationToken.None)
            .GetAwaiter().GetResult();
        _tasksById = tasks.ToDictionary(t => t.Id);
        _edges = _store.GetDependencyEdgesAsync(CancellationToken.None).GetAwaiter().GetResult();
        Rebuild();
    }

    private void Rebuild()
    {
        var selectedTaskId = SelectedTaskId;

        _rows = TreeBuilder.Build(_rootId, _edges, _edgeMode, _direction);

        var preservedIndex = selectedTaskId is null ? -1 : IndexOf(selectedTaskId);
        _cursorIndex = preservedIndex >= 0 ? preservedIndex : 0;

        _viewport.Update(_rows.Count, _viewport.WindowHeight);
        _viewport.EnsureVisible(_cursorIndex);
    }

    private int IndexOf(string taskId)
    {
        for (var i = 0; i < _rows.Count; i++)
        {
            if (_rows[i].TaskId == taskId)
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// Builds the panel header: the breadcrumb of ids the tree has been
    /// refocused through, followed by the edge mode and direction. Degrades
    /// deterministically as <paramref name="maxWidth"/> shrinks, middle-
    /// truncating the id chain first and, if that alone still does not fit,
    /// dropping the edge mode and then the direction, so the header never
    /// depends on the panel's own ellipsis truncation.
    /// </summary>
    private string BuildTitle(int maxWidth)
    {
        var trail = _breadcrumbs.Reverse().Append(_rootId);
        var trailText = string.Join(" > ", trail);
        var edgeLabel = _edgeMode == TreeEdgeMode.Blocking ? "blocking" : "parent-child";
        var directionLabel = _direction == TreeDirection.Up ? "depends on" : "depended on by";

        const string separator = " · ";
        var suffixFull = separator + edgeLabel + separator + directionLabel;
        var suffixDirectionOnly = separator + directionLabel;

        string title;

        if (maxWidth >= suffixFull.Length)
        {
            title = MiddleTruncate(trailText, maxWidth - suffixFull.Length) + suffixFull;
        }
        else if (maxWidth >= suffixDirectionOnly.Length)
        {
            title = MiddleTruncate(trailText, maxWidth - suffixDirectionOnly.Length) + suffixDirectionOnly;
        }
        else
        {
            title = MiddleTruncate(trailText, maxWidth);
        }

        return Markup.Escape(title);
    }

    /// <summary>
    /// Truncates <paramref name="text"/> to at most <paramref name="maxLength"/>
    /// characters, replacing the middle with a single ellipsis so both the
    /// start and the end of an id chain stay legible.
    /// </summary>
    private static string MiddleTruncate(string text, int maxLength)
    {
        if (maxLength <= 0)
        {
            return string.Empty;
        }

        if (text.Length <= maxLength)
        {
            return text;
        }

        if (maxLength == 1)
        {
            return "…";
        }

        var keep = maxLength - 1;
        var head = (keep + 1) / 2;
        var tail = keep - head;

        return tail > 0
            ? text[..head] + "…" + text[(text.Length - tail)..]
            : text[..head] + "…";
    }

    private IReadOnlyList<IRenderable> RenderLines(int width, int height)
    {
        if (height <= 0)
        {
            return [];
        }

        var reservedRows = 0;

        for (var pass = 0; pass < 3; pass++)
        {
            var windowHeight = Math.Max(0, height - reservedRows);
            _viewport.Update(_rows.Count, windowHeight);
            _viewport.EnsureVisible(_cursorIndex);

            var needed = (_viewport.HiddenAbove > 0 ? 1 : 0) + (_viewport.HiddenBelow > 0 ? 1 : 0);

            if (needed == reservedRows)
            {
                break;
            }

            reservedRows = needed;
        }

        var (start, count) = _viewport.Slice();
        var lines = new List<IRenderable>(count + 2);

        if (_viewport.HiddenAbove > 0)
        {
            lines.Add(new Markup(Markup.Escape($"  {_viewport.HiddenAbove} more above")));
        }

        for (var i = start; i < start + count; i++)
        {
            lines.Add(new Markup(RenderRow(_rows[i], selected: i == _cursorIndex, width)));
        }

        if (_viewport.HiddenBelow > 0)
        {
            lines.Add(new Markup(Markup.Escape($"  {_viewport.HiddenBelow} more below")));
        }

        return lines;
    }

    private string RenderRow(TreeNodeRow row, bool selected, int width)
    {
        var connector = row.BuildConnector();
        var suffix = row.IsCycle ? " (cycle)" : "";
        var badgeWidth = Math.Max(0, width - connector.Length - suffix.Length);

        var task = _tasksById.GetValueOrDefault(row.TaskId);
        var title = task?.Title ?? "";
        var status = task?.Status ?? "unknown";
        var priority = task?.Priority ?? TaskPriorities.Medium;
        var type = task?.Type ?? TaskTypes.Task;

        var badge = TaskBadge.Render(row.TaskId, title, status, priority, type, selected, badgeWidth);

        return Markup.Escape(connector) + badge + Markup.Escape(suffix);
    }
}
