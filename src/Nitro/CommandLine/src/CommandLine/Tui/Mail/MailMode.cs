using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tui.Editing;
using ChilliCream.Nitro.CommandLine.Tui.Input;
using ChilliCream.Nitro.CommandLine.Tui.Shell;
using ChilliCream.Nitro.CommandLine.Tui.Widgets;
using ChilliCream.Nitro.CommandLine.Tui.Widgets.Form;
using Spectre.Console.Rendering;
using CursorDirection = ChilliCream.Nitro.CommandLine.Tui.Input.CursorDirection;

namespace ChilliCream.Nitro.CommandLine.Tui.Mail;

/// <summary>
/// Displays every thread in the workspace as one read-only, full-width table: subject,
/// from, to, message count, and last-activity columns. The board has no acting agent, so
/// every mail write (compose, reply, read/unread, archive) is unavailable here; opening a
/// thread is a no-op until the thread popover ships.
/// </summary>
internal sealed class MailMode : ITuiMode, IRawKeyCapturingMode
{
    private const int PanelChromeWidth = 4;
    private const int PanelChromeHeight = 2;
    private const int MaxIndicatorSettlePasses = 3;
    private const int HeaderLineCount = 4;

    private const string EmptyStateMessage = "No mail yet.";

    /// <summary>
    /// The <see cref="QuickPickerOption.Id"/> for the agent filter picker's "all agents"
    /// entry, which clears <see cref="MailState.AgentFilter"/> rather than naming an agent.
    /// </summary>
    private const string AllAgentsOptionId = "";

    private readonly IAgentStore _agentStore;
    private readonly TimeProvider _timeProvider;
    private readonly MailState _state;
    private readonly Viewport _listViewport = new(0, 0);

    private MailSearchForm? _searchForm;
    private QuickPicker? _agentPicker;

    public MailMode(IMailStore store, IAgentStore agentStore, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(agentStore);

        _agentStore = agentStore;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _state = new MailState(new MailDataLoader(store));
    }

    /// <summary>
    /// The board's current live state: the loaded threads, filters, and selection.
    /// </summary>
    public MailState State => _state;

    /// <inheritdoc />
    public KeyMap? KeyMap => null;

    /// <inheritdoc />
    public bool IsInputCapturing => _searchForm is not null || _agentPicker is not null;

    /// <inheritdoc />
    public IReadOnlyList<KeyHint> CapturingHints
        => _searchForm is not null ? MailSearchForm.Hints
        : _agentPicker is not null ? QuickPicker.Hints
        : [];

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
        // OpenSelected opens the thread popover from a later ticket; a no-op until then.
        TuiMessage.RefreshRequested => Refresh(),
        TuiMessage.CopySelectedId => CopySelectedId(),
        TuiMessage.SearchRequested => OpenSearchForm(),
        TuiMessage.AgentFilterPickerRequested => OpenAgentFilterPicker(),
        _ => []
    };

    /// <summary>
    /// Handles one raw key while <see cref="IsInputCapturing"/> is true, routed here by
    /// the host instead of through the semantic <see cref="TuiMessage"/> dispatch.
    /// </summary>
    public IReadOnlyList<TuiMessage> HandleRawKey(ConsoleKeyInfo info)
        => _searchForm is not null ? HandleSearchFormKey(info)
        : _agentPicker is not null ? HandleAgentPickerKey(info)
        : [];

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

        if (_agentPicker is { } agentPicker)
        {
            return agentPicker.Render(width, height);
        }

        return RenderListPane(width, height);
    }

    private IReadOnlyList<TuiMessage> Move(int delta)
    {
        if (_state.Threads.Count > 0)
        {
            _state.SelectedRow = Math.Clamp(_state.SelectedRow + delta, 0, _state.Threads.Count - 1);
        }

        return [];
    }

    private IReadOnlyList<TuiMessage> MoveToEdge(EdgeTarget edge)
    {
        if (_state.Threads.Count > 0)
        {
            _state.SelectedRow = edge == EdgeTarget.Top ? 0 : _state.Threads.Count - 1;
        }

        return [];
    }

    private IReadOnlyList<TuiMessage> Refresh()
    {
        RefreshBlocking();
        return [];
    }

    private IReadOnlyList<TuiMessage> CopySelectedId()
    {
        if (_state.SelectedThread is not { } thread)
        {
            return [new TuiMessage.ShowToast("No thread selected.", ToastStyle.Warn)];
        }

        return [new TuiMessage.ShowToast(thread.ThreadId, ToastStyle.Info)];
    }

    private IReadOnlyList<TuiMessage> OpenSearchForm()
    {
        _searchForm = new MailSearchForm(_state.SearchText);
        return [];
    }

    private IReadOnlyList<TuiMessage> HandleSearchFormKey(ConsoleKeyInfo info)
    {
        var result = _searchForm!.HandleKey(info);

        return result switch
        {
            null => [],
            FormResult.Cancelled => CloseSearchForm(),
            FormResult.ButtonActivated { ButtonId: MailSearchForm.CancelButtonId } => CloseSearchForm(),
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
        _state.ApplySearch(text);
        return [];
    }

    /// <summary>
    /// Opens the agent filter picker with the current filter selected and an entry to
    /// clear it.
    /// </summary>
    private IReadOnlyList<TuiMessage> OpenAgentFilterPicker()
    {
        var agents = _agentStore.ListAsync(CancellationToken.None)
            .GetAwaiter()
            .GetResult()
            .OrderBy(a => a.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        _agentPicker = BuildAgentPicker(agents, _state.AgentFilter);
        return [];
    }

    private static QuickPicker BuildAgentPicker(IReadOnlyList<AgentRow> agents, string? selectedAgent)
    {
        var options = new List<QuickPickerOption> { new(AllAgentsOptionId, "All agents") };
        options.AddRange(agents.Select(a => new QuickPickerOption(a.Name, FormatAgentOptionMarkup(a))));

        return new QuickPicker("Filter by agent", options, selectedAgent ?? AllAgentsOptionId);
    }

    /// <summary>
    /// An agent picker row's markup: the name, plus its
    /// <see cref="AgentRow.Harness"/> display name in dim parentheses when non-empty.
    /// </summary>
    private static string FormatAgentOptionMarkup(AgentRow agent)
    {
        var name = Markup.Escape(agent.Name);
        return agent.Harness is not { Length: > 0 } harness
            ? name
            : $"{name} [dim]({Markup.Escape(AgentHarnessDisplay.Name(harness))})[/]";
    }

    private IReadOnlyList<TuiMessage> HandleAgentPickerKey(ConsoleKeyInfo info)
    {
        var result = _agentPicker!.HandleKey(info);

        return result switch
        {
            null => [],
            QuickPickerResult.Cancelled => CancelAgentPicker(),
            QuickPickerResult.Applied applied => ApplyAgentFilter(applied.SelectedId),
            _ => []
        };
    }

    private IReadOnlyList<TuiMessage> CancelAgentPicker()
    {
        _agentPicker = null;
        return [];
    }

    private IReadOnlyList<TuiMessage> ApplyAgentFilter(string selectedId)
    {
        _agentPicker = null;

        var agent = selectedId == AllAgentsOptionId ? null : selectedId;
        _state.SelectAgentFilterAsync(agent, CancellationToken.None).GetAwaiter().GetResult();

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
    /// The panel header: "Mail (N)", or "Mail: &lt;agent&gt; (N)" when the agent filter
    /// picker has narrowed the threads to one agent.
    /// </summary>
    private static string BuildHeader(MailState state) => state.AgentFilter is { } agent
        ? $"Mail: {agent} ({state.TotalCount})"
        : $"Mail ({state.TotalCount})";

    /// <summary>
    /// Renders the header block (a blank line, the header row, its rule, and a blank line),
    /// then the visible rows, padded with blank lines to <paramref name="interiorHeight"/>,
    /// with "N more above/below" indicators once the rows no longer fit. Column widths are
    /// computed from this call's visible slice and the header titles, so the header and rows
    /// always agree on where each column starts. Shows the empty-state message below the
    /// header block when there is no mail at all.
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

        if (_state.TotalCount == 0)
        {
            var widths = MailTable.ComputeWidths([], now);
            MailTable.AddHeaderLines(lines, headerLineCount, contentWidth, widths);

            if (rowsHeight > 0)
            {
                lines.Add(DisplayWidth.Truncate(EmptyStateMessage, contentWidth));
            }

            PadTo(lines, interiorHeight);
            return lines;
        }

        var threads = _state.Threads;
        var reservedRows = 0;

        for (var pass = 0; pass < MaxIndicatorSettlePasses; pass++)
        {
            var windowHeight = Math.Max(0, rowsHeight - reservedRows);
            _listViewport.Update(threads.Count, windowHeight);
            _listViewport.EnsureVisible(_state.SelectedRow);

            var needed = (_listViewport.HiddenAbove > 0 ? 1 : 0) + (_listViewport.HiddenBelow > 0 ? 1 : 0);

            if (needed == reservedRows)
            {
                break;
            }

            reservedRows = needed;
        }

        var (start, visibleCount) = _listViewport.Slice();
        var visibleThreads = new List<MailThreadSummary>(visibleCount);

        for (var i = 0; i < visibleCount; i++)
        {
            visibleThreads.Add(threads[start + i]);
        }

        var rowWidths = MailTable.ComputeWidths(visibleThreads, now);
        MailTable.AddHeaderLines(lines, headerLineCount, contentWidth, rowWidths);

        if (_listViewport.HiddenAbove > 0)
        {
            lines.Add(FormatIndicator(_listViewport.HiddenAbove, "above"));
        }

        for (var i = 0; i < visibleCount; i++)
        {
            var selected = start + i == _state.SelectedRow;
            lines.Add(MailTable.Render(visibleThreads[i], now, selected, contentWidth, rowWidths));
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
