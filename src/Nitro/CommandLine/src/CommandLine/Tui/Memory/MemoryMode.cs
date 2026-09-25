using ChilliCream.Nitro.CommandLine.Services.Memory;
using ChilliCream.Nitro.CommandLine.Tui.Input;
using ChilliCream.Nitro.CommandLine.Tui.Shell;
using ChilliCream.Nitro.CommandLine.Tui.Widgets;
using ChilliCream.Nitro.CommandLine.Tui.Widgets.Form;
using Spectre.Console.Rendering;
using CursorDirection = ChilliCream.Nitro.CommandLine.Tui.Input.CursorDirection;

namespace ChilliCream.Nitro.CommandLine.Tui.Memory;

/// <summary>
/// Displays every curated memory and journal entry in the workspace as one read-only,
/// full-width table: kind, type, tags, age, and body columns. The board has no acting agent,
/// so promoting a journal entry or forgetting a curated memory is unavailable here; opening a
/// row is a no-op until the entry popover ships.
/// </summary>
internal sealed class MemoryMode : ITuiMode, IRawKeyCapturingMode
{
    private const int PanelChromeWidth = 4;
    private const int PanelChromeHeight = 2;
    private const int MaxIndicatorSettlePasses = 3;
    private const int HeaderLineCount = 4;

    private const string EmptyStateMessage = "No memory yet.";

    private readonly TimeProvider _timeProvider;
    private readonly MemoryState _state;
    private readonly Viewport _listViewport = new(0, 0);

    private MemorySearchForm? _searchForm;

    public MemoryMode(IMemoryStore store, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(store);

        _timeProvider = timeProvider ?? TimeProvider.System;
        _state = new MemoryState(new MemoryDataLoader(store));
    }

    /// <summary>
    /// The board's current live state: the loaded rows, kind filter, and selection.
    /// </summary>
    public MemoryState State => _state;

    /// <inheritdoc />
    public KeyMap? KeyMap => null;

    /// <inheritdoc />
    public bool IsInputCapturing => _searchForm is not null;

    /// <inheritdoc />
    public IReadOnlyList<KeyHint> CapturingHints => _searchForm is not null ? MemorySearchForm.Hints : [];

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
        TuiMessage.MoveCursor(CursorDirection.Up) => Move(-1),
        TuiMessage.MoveCursor(CursorDirection.Down) => Move(1),
        TuiMessage.MoveToEdge(var edge) => MoveToEdge(edge),
        // OpenSelected opens the entry popover from a later ticket; a no-op until then.
        TuiMessage.RefreshRequested => Refresh(),
        TuiMessage.CycleView(var delta) => CycleFilter(delta),
        TuiMessage.CopySelectedId => CopySelectedId(),
        TuiMessage.SearchRequested => OpenSearchForm(),
        _ => []
    };

    /// <summary>
    /// Handles one raw key while <see cref="IsInputCapturing"/> is true, routed here by
    /// the host instead of through the semantic <see cref="TuiMessage"/> dispatch.
    /// </summary>
    public IReadOnlyList<TuiMessage> HandleRawKey(ConsoleKeyInfo info)
        => _searchForm is not null ? HandleSearchFormKey(info) : [];

    /// <inheritdoc />
    public IRenderable Render(int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            return new Markup(string.Empty);
        }

        if (_searchForm is { } searchForm)
        {
            return searchForm.Render(width, height);
        }

        return RenderListPane(width, height);
    }

    private IReadOnlyList<TuiMessage> Move(int delta)
    {
        if (_state.Rows.Count > 0)
        {
            _state.SelectedRow = Math.Clamp(_state.SelectedRow + delta, 0, _state.Rows.Count - 1);
        }

        return [];
    }

    private IReadOnlyList<TuiMessage> MoveToEdge(EdgeTarget edge)
    {
        if (_state.Rows.Count > 0)
        {
            _state.SelectedRow = edge == EdgeTarget.Top ? 0 : _state.Rows.Count - 1;
        }

        return [];
    }

    private IReadOnlyList<TuiMessage> Refresh()
    {
        RefreshBlocking();
        return [];
    }

    private IReadOnlyList<TuiMessage> CycleFilter(int delta)
    {
        _state.CycleFilterAsync(delta, CancellationToken.None).GetAwaiter().GetResult();
        return [];
    }

    private IReadOnlyList<TuiMessage> CopySelectedId()
    {
        if (_state.SelectedItem is not { } item)
        {
            return [new TuiMessage.ShowToast("No item selected.", ToastStyle.Warn)];
        }

        return [new TuiMessage.ShowToast(item.Id, ToastStyle.Info)];
    }

    private IReadOnlyList<TuiMessage> OpenSearchForm()
    {
        _searchForm = new MemorySearchForm(_state.SearchText);
        return [];
    }

    private IReadOnlyList<TuiMessage> HandleSearchFormKey(ConsoleKeyInfo info)
    {
        var result = _searchForm!.HandleKey(info);

        return result switch
        {
            null => [],
            FormResult.Cancelled => CloseSearchForm(),
            FormResult.ButtonActivated { ButtonId: MemorySearchForm.CancelButtonId } => CloseSearchForm(),
            FormResult.Submitted => ApplySearch(),
            _ => []
        };
    }

    private IReadOnlyList<TuiMessage> CloseSearchForm()
    {
        _searchForm = null;
        return [];
    }

    private IReadOnlyList<TuiMessage> ApplySearch()
    {
        var text = _searchForm!.Text;
        _searchForm = null;
        _state.ApplySearchAsync(text, CancellationToken.None).GetAwaiter().GetResult();
        return [];
    }

    private IRenderable RenderListPane(int width, int height)
    {
        var safeWidth = Math.Max(1, width);
        var contentWidth = Math.Max(0, safeWidth - PanelChromeWidth);
        var interiorHeight = Math.Max(0, height - PanelChromeHeight);
        var now = _timeProvider.GetUtcNow();

        var lines = RenderListLines(contentWidth, interiorHeight, now);
        var header = BuildHeader(_state);
        var panel = ColumnPane.RenderWithHeader(header, lines, focused: true);
        panel.Width = safeWidth;
        panel.Height = Math.Max(1, height);

        return panel;
    }

    /// <summary>
    /// The panel header: "Memory (N)", "Curated (N)", or "Journal (N)" for the active kind
    /// filter, with " (filtered)" appended once a search is set.
    /// </summary>
    private static string BuildHeader(MemoryState state)
    {
        var label = state.Filter switch
        {
            MemoryCollectionFilter.Curated => "Curated",
            MemoryCollectionFilter.Journal => "Journal",
            _ => "Memory"
        };

        var suffix = state.SearchText.Length > 0 ? " (filtered)" : "";
        return $"{label} ({state.Rows.Count}){suffix}";
    }

    /// <summary>
    /// Renders the header block (a blank line, the header row, its rule, and a blank line),
    /// then the visible rows, padded with blank lines to <paramref name="interiorHeight"/>,
    /// with "N more above/below" indicators once the rows no longer fit. Column widths are
    /// computed from this call's visible slice and the header titles, so the header and rows
    /// always agree on where each column starts. Shows the empty-state message below the
    /// header block when there is no memory at all.
    /// </summary>
    private IReadOnlyList<string> RenderListLines(int contentWidth, int interiorHeight, DateTimeOffset now)
    {
        if (interiorHeight <= 0)
        {
            return [];
        }

        var headerLineCount = Math.Min(HeaderLineCount, interiorHeight);
        var rowsHeight = Math.Max(0, interiorHeight - headerLineCount);
        var lines = new List<string>(interiorHeight);

        if (_state.Rows.Count == 0)
        {
            var widths = MemoryRowBadge.ComputeWidths([], now);
            MemoryRowBadge.AddHeaderLines(lines, headerLineCount, contentWidth, widths);

            if (rowsHeight > 0)
            {
                lines.Add(DisplayWidth.Truncate(_state.LoadError ?? EmptyStateMessage, contentWidth));
            }

            PadTo(lines, interiorHeight);
            return lines;
        }

        var rows = _state.Rows;
        var reservedRows = 0;

        for (var pass = 0; pass < MaxIndicatorSettlePasses; pass++)
        {
            var windowHeight = Math.Max(0, rowsHeight - reservedRows);
            _listViewport.Update(rows.Count, windowHeight);
            _listViewport.EnsureVisible(_state.SelectedRow);

            var needed = (_listViewport.HiddenAbove > 0 ? 1 : 0) + (_listViewport.HiddenBelow > 0 ? 1 : 0);

            if (needed == reservedRows)
            {
                break;
            }

            reservedRows = needed;
        }

        var (start, visibleCount) = _listViewport.Slice();
        var visibleRows = new List<MemoryRow>(visibleCount);

        for (var i = 0; i < visibleCount; i++)
        {
            visibleRows.Add(rows[start + i]);
        }

        var rowWidths = MemoryRowBadge.ComputeWidths(visibleRows, now);
        MemoryRowBadge.AddHeaderLines(lines, headerLineCount, contentWidth, rowWidths);

        if (_listViewport.HiddenAbove > 0)
        {
            lines.Add(FormatIndicator(_listViewport.HiddenAbove, "above"));
        }

        for (var i = 0; i < visibleCount; i++)
        {
            var selected = start + i == _state.SelectedRow;
            lines.Add(MemoryRowBadge.Render(visibleRows[i], now, selected, contentWidth, rowWidths));
        }

        if (_listViewport.HiddenBelow > 0)
        {
            lines.Add(FormatIndicator(_listViewport.HiddenBelow, "below"));
        }

        PadTo(lines, interiorHeight);
        return lines;
    }

    private static void PadTo(List<string> lines, int height)
    {
        while (lines.Count < height)
        {
            lines.Add(string.Empty);
        }
    }

    private static string FormatIndicator(int hiddenCount, string direction) => $"  {hiddenCount} more {direction}";

    private void RefreshBlocking() => _state.RefreshAsync(CancellationToken.None).GetAwaiter().GetResult();
}
