using ChilliCream.Nitro.CommandLine.Tui.Input;
using ChilliCream.Nitro.CommandLine.Tui.Shell;
using ChilliCream.Nitro.CommandLine.Tui.Widgets;
using Spectre.Console.Rendering;
using CursorDirection = ChilliCream.Nitro.CommandLine.Tui.Input.CursorDirection;

namespace ChilliCream.Nitro.CommandLine.Tui.Board;

/// <summary>
/// The kanban board <see cref="ITuiMode"/>: renders a board view as equal-width
/// columns and turns navigation, refresh, and selection intents into changes on
/// a <see cref="BoardState"/>.
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
    /// The number of distinct above/below indicator combinations a column's
    /// viewport can settle on, bounding how many times reserving space for
    /// them needs to be recomputed.
    /// </summary>
    private const int MaxIndicatorSettlePasses = 3;

    private readonly BoardDataLoader _loader;
    private readonly IReadOnlyList<BoardView> _views;

    private BoardState _state;
    private Viewport[] _viewports;
    private int _viewIndex;
    private bool _maximized;

    /// <summary>
    /// Creates the board mode over <paramref name="loader"/>, starting on the
    /// first of <paramref name="views"/>. Defaults to the single v1 built-in
    /// view when <paramref name="views"/> is not given.
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
    public void OnEnter() => RefreshBlocking();

    /// <inheritdoc />
    public void OnResize(int width, int height)
    {
        // Render(width, height) recomputes the layout decision and every
        // column's viewport window from its parameters on every frame, so
        // there is no per-resize state to update ahead of time.
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
        TuiMessage.OpenSelected => [new TuiMessage.ShowToast("Detail view not available yet.", ToastStyle.Info)],
        TuiMessage.CopySelectedId => CopySelectedId(),
        _ => []
    };

    /// <inheritdoc />
    public IRenderable Render(int width, int height)
    {
        var columns = _state.Columns;

        if (columns.Count == 0 || width <= 0 || height <= 0)
        {
            return new Markup(string.Empty);
        }

        var decision = BoardLayout.Decide(width, height, columns.Count, _state.FocusedColumnIndex, _maximized);

        return decision.Kind switch
        {
            BoardLayoutKind.Maximized => RenderMaximized(decision),
            BoardLayoutKind.Stacked => RenderStacked(decision),
            _ => RenderGrid(decision)
        };
    }

    private IRenderable RenderGrid(BoardLayoutDecision decision)
    {
        var columns = _state.Columns;
        var columnLayouts = new Layout[columns.Count];

        for (var i = 0; i < columns.Count; i++)
        {
            var slot = decision.Columns[i];
            var focused = i == _state.FocusedColumnIndex;
            var panel = RenderColumnPanel(i, slot.Width, slot.Height, focused, headerSuffix: null);

            columnLayouts[i] = new Layout($"board-column-{i}", panel).Size(Math.Max(1, slot.Width));
        }

        return new Layout("board").SplitColumns(columnLayouts);
    }

    private IRenderable RenderMaximized(BoardLayoutDecision decision)
    {
        var columns = _state.Columns;
        var focused = _state.FocusedColumnIndex;
        var slot = decision.Columns[focused];
        var suffix = $"{focused + 1}/{columns.Count}";

        return RenderColumnPanel(focused, slot.Width, slot.Height, focused: true, headerSuffix: suffix);
    }

    private IRenderable RenderStacked(BoardLayoutDecision decision)
    {
        var columns = _state.Columns;
        var rows = new List<IRenderable>(columns.Count);

        for (var i = 0; i < columns.Count; i++)
        {
            var slot = decision.Columns[i];

            rows.Add(slot.Expanded
                ? RenderColumnPanel(i, slot.Width, slot.Height, focused: i == _state.FocusedColumnIndex, headerSuffix: null)
                : new Markup($"{columns[i].Definition.Name} ({columns[i].Tasks.Count})"));
        }

        return new Rows(rows);
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
        panel.Width = safeWidth;

        return panel;
    }

    private IReadOnlyList<TuiMessage> ToggleMaximize()
    {
        _maximized = !_maximized;
        return [];
    }

    private IReadOnlyList<TuiMessage> FocusColumn(int delta)
    {
        _state.FocusColumn(_state.FocusedColumnIndex + delta);
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
            // Nothing to switch to in v1's single built-in view; the intent is
            // still accepted so the wiring is exercised once a second view lands.
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
    /// Renders one column's visible rows: the scrolled task badges, padded
    /// with blank lines so every column reports the same line count, with
    /// "N more above/below" indicators reserving their own rows once the
    /// column's tasks no longer fit <paramref name="interiorHeight"/>.
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

        var reservedRows = 0;

        for (var pass = 0; pass < MaxIndicatorSettlePasses; pass++)
        {
            var windowHeight = Math.Max(0, interiorHeight - reservedRows);
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
        var lines = new List<string>(interiorHeight);

        if (viewport.HiddenAbove > 0)
        {
            lines.Add(FormatIndicator(viewport.HiddenAbove, "above"));
        }

        for (var i = 0; i < visibleCount; i++)
        {
            var task = column.Tasks[start + i];
            var selected = focused && start + i == column.SelectedRow;
            lines.Add(TaskBadge.Render(
                task.Id, task.Title, task.Status, task.Priority, task.Type, selected, contentWidth));
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
}
