using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Tui.Input;
using ChilliCream.Nitro.CommandLine.Tui.Shell;
using ChilliCream.Nitro.CommandLine.Tui.Widgets;
using Spectre.Console.Rendering;
using CursorDirection = ChilliCream.Nitro.CommandLine.Tui.Input.CursorDirection;

namespace ChilliCream.Nitro.CommandLine.Tui.Board;

/// <summary>
/// Displays and navigates a task board with grid, stacked, and maximized layouts.
/// </summary>
internal sealed class BoardMode : ITuiMode
{
    /// <summary>
    /// Border and padding columns a <see cref="ColumnPane"/> panel spends on
    /// either side of its content, at a given panel width.
    /// </summary>
    private const int PanelChromeWidth = 4;

    /// <summary>
    /// Border rows a <see cref="ColumnPane"/> panel spends above and below its
    /// content; the header is drawn on the top border row.
    /// </summary>
    private const int PanelChromeHeight = 2;

    /// <summary>
    /// The maximum number of passes used to reserve viewport indicator rows.
    /// </summary>
    private const int MaxIndicatorSettlePasses = 3;

    private readonly BoardDataLoader _loader;
    private readonly IReadOnlyList<BoardView> _views;

    private BoardState _state;
    private Viewport[] _viewports;
    private int _viewIndex;
    private bool _maximized;

    /// <summary>
    /// Creates a board using the first supplied view.
    /// A null or empty view list selects <see cref="BoardView.Default"/>.
    /// </summary>
    public BoardMode(BoardDataLoader loader, IReadOnlyList<BoardView>? views = null)
    {
        ArgumentNullException.ThrowIfNull(loader);

        _loader = loader;
        _views = views is { Count: > 0 } ? views : [BoardView.Default];
        _state = new BoardState(_views[0], _loader);
        _viewports = CreateViewports(_state.Columns.Count);
    }

    /// <summary>
    /// The board's current live state: columns, tasks, selection, and focus.
    /// </summary>
    public BoardState State => _state;

    /// <inheritdoc />
    public KeyMap? KeyMap => null;

    /// <inheritdoc />
    public string? SelectedTaskId => FocusedColumn()?.SelectedTaskId;

    /// <inheritdoc />
    public void SelectTask(string id)
    {
        var columns = _state.Columns;

        for (var i = 0; i < columns.Count; i++)
        {
            var row = IndexOfTask(columns[i].Tasks, id);

            if (row < 0)
            {
                continue;
            }

            _state.FocusColumn(i);
            columns[i].SelectedRow = row;
            return;
        }
    }

    /// <inheritdoc />
    public void OnEnter() => RefreshBlocking();

    /// <inheritdoc />
    public void OnResize(int width, int height)
    {
        // Layout and viewport state are recomputed from Render's parameters every frame.
    }

    /// <inheritdoc />
    public IReadOnlyList<TuiMessage> Handle(TuiMessage message) => message switch
    {
        TuiMessage.MoveCursor(CursorDirection.Left) => FocusColumn(-1),
        TuiMessage.MoveCursor(CursorDirection.Right) => FocusColumn(1),
        TuiMessage.MoveCursor(CursorDirection.Up) => MoveSelection(-1),
        TuiMessage.MoveCursor(CursorDirection.Down) => MoveSelection(1),
        TuiMessage.MoveToEdge(EdgeTarget.Top) => MoveSelectionToEdge(top: true),
        TuiMessage.MoveToEdge(EdgeTarget.Bottom) => MoveSelectionToEdge(top: false),
        TuiMessage.RefreshRequested => Refresh(),
        TuiMessage.CycleView(var delta) => CycleView(delta),
        TuiMessage.ToggleMaximize => ToggleMaximize(),
        // OpenSelected is handled by TuiShell before it reaches here: the shell switches to a
        // BoardDetailMode showing the selection.
        TuiMessage.CopySelectedId => CopySelectedId(),
        _ => []
    };

    /// <inheritdoc />
    public IRenderable Render(int width, int height)
    {
        if (_state.Columns.Count == 0 || width <= 0 || height <= 0)
        {
            return new Markup(string.Empty);
        }

        var visibleIndices = _state.VisibleColumnIndices;

        if (visibleIndices.Count == 0)
        {
            return RenderEmptyBoard(width, height);
        }

        var focusedPosition = Math.Max(0, IndexOf(visibleIndices, _state.FocusedColumnIndex));
        var decision = BoardLayout.Decide(width, height, visibleIndices.Count, focusedPosition, _maximized);

        return decision.Kind switch
        {
            BoardLayoutKind.Maximized => RenderMaximized(decision, visibleIndices, focusedPosition),
            BoardLayoutKind.Stacked => RenderStacked(decision, visibleIndices),
            _ => RenderGrid(decision, visibleIndices)
        };
    }

    private IRenderable RenderGrid(BoardLayoutDecision decision, IReadOnlyList<int> visibleIndices)
    {
        var columnLayouts = new Layout[visibleIndices.Count];

        for (var position = 0; position < visibleIndices.Count; position++)
        {
            var rawIndex = visibleIndices[position];
            var slot = decision.Columns[position];
            var focused = rawIndex == _state.FocusedColumnIndex;
            var panel = RenderColumnPanel(rawIndex, slot.Width, slot.Height, focused, headerSuffix: null);

            columnLayouts[position] = new Layout($"board-column-{rawIndex}", panel).Size(Math.Max(1, slot.Width));
        }

        return new Layout("board").SplitColumns(columnLayouts);
    }

    private IRenderable RenderMaximized(
        BoardLayoutDecision decision, IReadOnlyList<int> visibleIndices, int focusedPosition)
    {
        var rawIndex = visibleIndices[focusedPosition];
        var slot = decision.Columns[focusedPosition];
        var suffix = $"{focusedPosition + 1}/{visibleIndices.Count}";

        return RenderColumnPanel(rawIndex, slot.Width, slot.Height, focused: true, headerSuffix: suffix);
    }

    private IRenderable RenderStacked(BoardLayoutDecision decision, IReadOnlyList<int> visibleIndices)
    {
        var columns = _state.Columns;
        var rows = new List<IRenderable>(Math.Max(0, visibleIndices.Count * 2 - 1));

        for (var position = 0; position < visibleIndices.Count; position++)
        {
            if (position > 0)
            {
                // Insert one blank row between stacked column panels.
                rows.Add(new Text(string.Empty));
            }

            var rawIndex = visibleIndices[position];
            var slot = decision.Columns[position];

            var focused = rawIndex == _state.FocusedColumnIndex;

            rows.Add(slot.Expanded
                ? RenderColumnPanel(rawIndex, slot.Width, slot.Height, focused, headerSuffix: null)
                : new Markup(Markup.Escape($"{columns[rawIndex].Definition.Name} ({columns[rawIndex].Tasks.Count})")));
        }

        return new Rows(rows);
    }

    /// <summary>
    /// Renders the single full-width panel shown when every column is empty.
    /// </summary>
    private IRenderable RenderEmptyBoard(int width, int height)
    {
        var safeWidth = Math.Max(1, width);
        var interiorHeight = Math.Max(0, height - PanelChromeHeight);
        var lines = new List<string>(interiorHeight);

        if (interiorHeight > 0)
        {
            lines.Add("No tasks yet.");
        }

        while (lines.Count < interiorHeight)
        {
            lines.Add(string.Empty);
        }

        var panel = ColumnPane.RenderWithHeader($"{_state.View.Name} (0)", lines, focused: false);
        panel.Width = safeWidth;
        panel.Height = Math.Max(1, height);

        return panel;
    }

    /// <summary>
    /// Builds one column's bordered panel: its visible task lines, sized to
    /// <paramref name="columnWidth"/> and <paramref name="panelHeight"/>, with
    /// <paramref name="headerSuffix"/> appended to the column name when the
    /// column is the only one shown.
    /// </summary>
    private Panel RenderColumnPanel(int index, int columnWidth, int panelHeight, bool focused, string? headerSuffix)
    {
        var column = _state.Columns[index];
        var safeWidth = Math.Max(1, columnWidth);
        var contentWidth = Math.Max(0, safeWidth - PanelChromeWidth);
        var interiorHeight = Math.Max(0, panelHeight - PanelChromeHeight);

        var lines = RenderColumnLines(column, _viewports[index], contentWidth, interiorHeight, focused);
        var name = headerSuffix is null ? column.Definition.Name : $"{column.Definition.Name} - {headerSuffix}";
        var panel = ColumnPane.Render(name, column.Tasks.Count, lines, focused);

        // Panel header title inherits the same accent color as its border, per column.
        var borderStyle = column.Definition.ResolveBorderStyle(focused);
        panel.BorderStyle = borderStyle;
        panel.Header = panel.Header!.SetStyle(borderStyle);

        panel.Width = safeWidth;
        panel.Height = Math.Max(1, panelHeight);

        return panel;
    }

    private IReadOnlyList<TuiMessage> ToggleMaximize()
    {
        _maximized = !_maximized;
        return [];
    }

    private IReadOnlyList<TuiMessage> FocusColumn(int delta)
    {
        _state.FocusAdjacentVisibleColumn(delta);
        return [];
    }

    private IReadOnlyList<TuiMessage> MoveSelection(int delta)
    {
        var column = FocusedColumn();

        if (column is { Tasks.Count: > 0 })
        {
            column.SelectedRow = Math.Clamp(column.SelectedRow + delta, 0, column.Tasks.Count - 1);
        }

        return [];
    }

    private IReadOnlyList<TuiMessage> MoveSelectionToEdge(bool top)
    {
        var column = FocusedColumn();

        if (column is { Tasks.Count: > 0 })
        {
            column.SelectedRow = top ? 0 : column.Tasks.Count - 1;
        }

        return [];
    }

    private IReadOnlyList<TuiMessage> Refresh()
    {
        RefreshBlocking();
        return [];
    }

    private IReadOnlyList<TuiMessage> CycleView(int delta)
    {
        if (_views.Count <= 1)
        {
            return [];
        }

        _viewIndex = ((_viewIndex + delta) % _views.Count + _views.Count) % _views.Count;
        _state = new BoardState(_views[_viewIndex], _loader);
        _viewports = CreateViewports(_state.Columns.Count);
        RefreshBlocking();
        return [];
    }

    private IReadOnlyList<TuiMessage> CopySelectedId()
    {
        var id = FocusedColumn()?.SelectedTaskId;

        return id is null
            ? [new TuiMessage.ShowToast("No task selected.", ToastStyle.Warn)]
            : [new TuiMessage.ShowToast(id, ToastStyle.Info)];
    }

    private BoardColumnState? FocusedColumn()
    {
        var columns = _state.Columns;
        return _state.FocusedColumnIndex >= 0 && _state.FocusedColumnIndex < columns.Count
            ? columns[_state.FocusedColumnIndex]
            : null;
    }

    private void RefreshBlocking() => _state.RefreshAsync(CancellationToken.None).GetAwaiter().GetResult();

    private static Viewport[] CreateViewports(int columnCount)
    {
        var viewports = new Viewport[columnCount];

        for (var i = 0; i < columnCount; i++)
        {
            viewports[i] = new Viewport(0, 0);
        }

        return viewports;
    }

    /// <summary>
    /// Renders one column's task table: the fixed header block (a blank line, the header row,
    /// and its rule), then the visible rows, padded with blank lines to
    /// <paramref name="interiorHeight"/>, with "N more above/below" indicators once the
    /// column's tasks no longer fit. Column widths are computed from this call's visible
    /// slice and the header titles, so the header and rows always agree on where each column
    /// starts.
    /// </summary>
    private static IReadOnlyList<string> RenderColumnLines(
        BoardColumnState column,
        Viewport viewport,
        int contentWidth,
        int interiorHeight,
        bool focused)
    {
        if (interiorHeight <= 0)
        {
            return [];
        }

        var headerLineCount = Math.Min(
            BoardTaskRow.HeaderLineCount,
            column.Tasks.Count > 0 ? Math.Max(0, interiorHeight - 1) : interiorHeight);
        var rowsHeight = Math.Max(0, interiorHeight - headerLineCount);
        var reservedRows = 0;

        for (var pass = 0; pass < MaxIndicatorSettlePasses; pass++)
        {
            var windowHeight = Math.Max(0, rowsHeight - reservedRows);
            viewport.Update(column.Tasks.Count, windowHeight);
            viewport.EnsureVisible(column.SelectedRow);

            var needed = (viewport.HiddenAbove > 0 ? 1 : 0) + (viewport.HiddenBelow > 0 ? 1 : 0);

            if (needed == reservedRows)
            {
                break;
            }

            reservedRows = needed;
        }

        var (start, visibleCount) = viewport.Slice();
        var visibleTasks = new List<TaskItem>(visibleCount);

        for (var i = 0; i < visibleCount; i++)
        {
            visibleTasks.Add(column.Tasks[start + i]);
        }

        var widths = BoardTaskRow.ComputeWidths(visibleTasks);
        var lines = new List<string>(interiorHeight);
        BoardTaskRow.AddHeaderLines(lines, headerLineCount, contentWidth, widths);

        if (viewport.HiddenAbove > 0)
        {
            lines.Add(FormatIndicator(viewport.HiddenAbove, "above"));
        }

        for (var i = 0; i < visibleCount; i++)
        {
            var selected = focused && start + i == column.SelectedRow;
            lines.Add(BoardTaskRow.Render(visibleTasks[i], selected, contentWidth, widths));
        }

        if (viewport.HiddenBelow > 0)
        {
            lines.Add(FormatIndicator(viewport.HiddenBelow, "below"));
        }

        while (lines.Count < interiorHeight)
        {
            lines.Add(string.Empty);
        }

        return lines;
    }

    private static string FormatIndicator(int hiddenCount, string direction) => $"  {hiddenCount} more {direction}";

    private static int IndexOfTask(IReadOnlyList<TaskItem> tasks, string id)
    {
        for (var i = 0; i < tasks.Count; i++)
        {
            if (tasks[i].Id == id)
            {
                return i;
            }
        }

        return -1;
    }

    private static int IndexOf(IReadOnlyList<int> values, int value)
    {
        for (var i = 0; i < values.Count; i++)
        {
            if (values[i] == value)
            {
                return i;
            }
        }

        return -1;
    }
}
