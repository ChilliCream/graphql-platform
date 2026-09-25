using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Memory;
using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tui.Input;
using ChilliCream.Nitro.CommandLine.Tui.Shell;
using ChilliCream.Nitro.CommandLine.Tui.Widgets;
using ChilliCream.Nitro.CommandLine.Tui.Widgets.Form;
using Spectre.Console.Rendering;
using CursorDirection = ChilliCream.Nitro.CommandLine.Tui.Input.CursorDirection;

namespace ChilliCream.Nitro.CommandLine.Tui.Agents;

/// <summary>
/// Displays every non-deleted agent as one full-width table: a presence bubble and name,
/// role, harness, and started/last-seen ages. State and ages are recomputed from
/// <see cref="TimeProvider.GetUtcNow"/> on every render, so bubbles and ages change without
/// a database event. Delete and delete-offline are requested here but confirmed and applied
/// by the hosting shell.
/// </summary>
internal sealed class AgentsMode : ITuiMode, IRawKeyCapturingMode
{
    private const int PanelChromeWidth = 4;
    private const int PanelChromeHeight = 2;
    private const int MaxIndicatorSettlePasses = 3;
    private const int HeaderLineCount = TableRenderer.TopBlockLineCount;

    private const string EmptyStateMessage =
        "No agents yet. Start a harness with Nitro hooks installed, or run nitro agent login.";

    private readonly IAgentStore _agentStore;
    private readonly TimeProvider _timeProvider;
    private readonly AgentsState _state;
    private readonly Viewport _listViewport = new(0, 0);

    private AgentSearchForm? _searchForm;
    private readonly IMailStore _mailStore;
    private readonly ITaskStore _taskStore;
    private readonly IMemoryStore _memoryStore;

    public AgentsMode(
        IAgentStore agentStore,
        IMailStore mailStore,
        ITaskStore taskStore,
        IMemoryStore memoryStore,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(agentStore);
        ArgumentNullException.ThrowIfNull(mailStore);
        ArgumentNullException.ThrowIfNull(taskStore);
        ArgumentNullException.ThrowIfNull(memoryStore);

        _agentStore = agentStore;
        _mailStore = mailStore;
        _taskStore = taskStore;
        _memoryStore = memoryStore;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _state = new AgentsState(agentStore, _timeProvider);
        KeyMap = AgentsKeyMap.CreateDefault(() => _state.SelectedAgent?.Name);
    }

    /// <summary>
    /// The mode's current live state: the loaded rows, search filter, and selection.
    /// </summary>
    public AgentsState State => _state;

    /// <inheritdoc />
    public KeyMap? KeyMap { get; }

    /// <inheritdoc />
    public bool IsInputCapturing => _searchForm is not null;

    /// <inheritdoc />
    public IReadOnlyList<KeyHint> CapturingHints => _searchForm is not null ? AgentSearchForm.Hints : [];

    /// <summary>
    /// The Agents tab hosts its own d/D/search bindings and hides the global tab's
    /// unrelated zoom, edit, back, and quit hints so its footer lists only its own keys.
    /// </summary>
    public IReadOnlyCollection<KeyHint> SuppressedGlobalHints { get; } =
    [
        new KeyHint("z", "zoom"),
        new KeyHint("e", "edit"),
        new KeyHint("esc", "back"),
        new KeyHint("q", "quit")
    ];

    /// <inheritdoc />
    public void OnEnter() => RefreshBlocking();

    /// <inheritdoc />
    public void OnResize(int width, int height)
    {
        // Layout and viewport state are recomputed from Render's parameters every frame.
    }

    /// <summary>
    /// Counts the agents currently resolving to <see cref="AgentState.Offline"/>, for the
    /// shell's delete-offline confirmation.
    /// </summary>
    public int CountOfflineAgents() => _state.CountOffline(_timeProvider.GetUtcNow());

    /// <summary>
    /// Recomputes state and ages as of now, re-sorting when they changed and preserving the
    /// selected agent by name. Returns whether anything the render depends on changed since
    /// the last tick or render.
    /// </summary>
    public bool Tick() => _state.Resettle(_timeProvider.GetUtcNow());

    /// <inheritdoc />
    public IReadOnlyList<TuiMessage> Handle(TuiMessage message) => message switch
    {
        TuiMessage.MoveCursor(CursorDirection.Up) => Move(-1),
        TuiMessage.MoveCursor(CursorDirection.Down) => Move(1),
        TuiMessage.MoveToEdge(var edge) => MoveToEdge(edge),
        // Reached only when TryCreatePopover found no selected agent to open: the shell falls
        // back to dispatching OpenSelected here for the no-selection toast.
        TuiMessage.OpenSelected => OpenSelectedFallback(),
        TuiMessage.RefreshRequested => Refresh(),
        TuiMessage.CopySelectedId => CopySelectedId(),
        TuiMessage.SearchRequested => OpenSearchForm(),
        _ => []
    };

    /// <inheritdoc />
    public IPopover? TryCreatePopover()
    {
        if (_state.SelectedAgent is not { } agent)
        {
            return null;
        }

        return new AgentPopoverModel(agent.Name, _agentStore, _mailStore, _taskStore, _memoryStore, _timeProvider);
    }

    /// <inheritdoc />
    public IReadOnlyList<TuiMessage> HandlePopoverRequest(PopoverResult.Request request) => request.Payload switch
    {
        AgentPopoverRequest.DeleteRequested deleteRequested =>
            [new TuiMessage.DeleteAgentRequested(deleteRequested.Name)],
        AgentPopoverRequest.CopyRequested copyRequested =>
            [BuildCopyIdToast(copyRequested.Name, copyRequested.SessionId)],
        AgentPopoverRequest.CopyItemRequested copyItemRequested =>
            [new TuiMessage.ShowToast(copyItemRequested.Id, ToastStyle.Info)],
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

    /// <summary>
    /// Reports the no-selection toast the shell shows when <see cref="TryCreatePopover"/>
    /// found no agent to open; a no-op when an agent is selected (the popover already opened).
    /// </summary>
    private IReadOnlyList<TuiMessage> OpenSelectedFallback() =>
        _state.SelectedAgent is null
            ? [new TuiMessage.ShowToast("No agent selected.", ToastStyle.Warn)]
            : [];

    private IReadOnlyList<TuiMessage> CopySelectedId()
    {
        if (_state.SelectedAgent is not { } agent)
        {
            return [new TuiMessage.ShowToast("No agent selected.", ToastStyle.Warn)];
        }

        return [BuildCopyIdToast(agent.Name, agent.SessionId)];
    }

    /// <summary>
    /// Builds the toast for copying an agent's session id: the id itself, or a login-only
    /// hint when <paramref name="sessionId"/> is null or empty.
    /// </summary>
    public static TuiMessage.ShowToast BuildCopyIdToast(string name, string? sessionId) =>
        sessionId is { Length: > 0 }
            ? new TuiMessage.ShowToast(sessionId, ToastStyle.Info)
            : new TuiMessage.ShowToast($"'{name}' is login-only and has no session.", ToastStyle.Info);

    private IReadOnlyList<TuiMessage> OpenSearchForm()
    {
        _searchForm = new AgentSearchForm(_state.SearchText);
        return [];
    }

    private IReadOnlyList<TuiMessage> HandleSearchFormKey(ConsoleKeyInfo info)
    {
        var result = _searchForm!.HandleKey(info);

        return result switch
        {
            null => [],
            FormResult.Cancelled => CloseSearchForm(),
            FormResult.ButtonActivated { ButtonId: AgentSearchForm.CancelButtonId } => CloseSearchForm(),
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

    private IRenderable RenderListPane(int width, int height)
    {
        var safeWidth = Math.Max(1, width);
        var contentWidth = Math.Max(0, safeWidth - PanelChromeWidth);
        var interiorHeight = Math.Max(0, height - PanelChromeHeight);
        var now = _timeProvider.GetUtcNow();

        var lines = RenderListLines(contentWidth, interiorHeight, now);
        var header = $"Agents ({_state.CountOnline(now)} online / {_state.TotalCount})";
        var panel = ColumnPane.RenderWithHeader(header, lines, focused: true);
        panel.Width = safeWidth;
        panel.Height = Math.Max(1, height);

        return panel;
    }

    /// <summary>
    /// Renders the header block (a blank line, the header row, and its rule), then the visible
    /// rows, padded with blank lines to <paramref name="interiorHeight"/>, with "N more
    /// above/below" indicators once the rows no longer fit. Column widths are computed from
    /// this call's visible slice and the header titles, so the header and rows always agree on
    /// where each column starts. Shows the empty-state message below the header block when
    /// there are no agents at all.
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
            var widths = AgentRowBadge.ComputeWidths([], now);
            AddHeaderLines(lines, headerLineCount, contentWidth, widths);

            if (rowsHeight > 0)
            {
                lines.Add(DisplayWidth.Truncate(EmptyStateMessage, contentWidth));
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
        var visibleRows = new List<AgentRow>(visibleCount);

        for (var i = 0; i < visibleCount; i++)
        {
            visibleRows.Add(rows[start + i]);
        }

        var rowWidths = AgentRowBadge.ComputeWidths(visibleRows, now);
        AddHeaderLines(lines, headerLineCount, contentWidth, rowWidths);

        if (_listViewport.HiddenAbove > 0)
        {
            lines.Add(FormatIndicator(_listViewport.HiddenAbove, "above"));
        }

        for (var i = 0; i < visibleCount; i++)
        {
            var selected = start + i == _state.SelectedRow;
            lines.Add(AgentRowBadge.Render(visibleRows[i], now, selected, contentWidth, rowWidths));
        }

        if (_listViewport.HiddenBelow > 0)
        {
            lines.Add(FormatIndicator(_listViewport.HiddenBelow, "below"));
        }

        PadTo(lines, interiorHeight);
        return lines;
    }

    /// <summary>
    /// Appends the header block to <paramref name="lines"/>: a blank line, the header title
    /// row, and its rule, up to <paramref name="headerLineCount"/> of
    /// <see cref="HeaderLineCount"/> (later lines are dropped first when the interior is too
    /// short to hold all of them).
    /// </summary>
    private static void AddHeaderLines(
        List<string> lines, int headerLineCount, int contentWidth, AgentRowBadge.Widths widths) =>
        AgentRowBadge.AddHeaderLines(lines, headerLineCount, contentWidth, widths);

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
