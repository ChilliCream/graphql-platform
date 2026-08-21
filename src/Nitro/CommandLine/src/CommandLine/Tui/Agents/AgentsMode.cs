using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tui.Input;
using ChilliCream.Nitro.CommandLine.Tui.Shell;
using ChilliCream.Nitro.CommandLine.Tui.Widgets;
using Spectre.Console.Rendering;
using CursorDirection = ChilliCream.Nitro.CommandLine.Tui.Input.CursorDirection;

namespace ChilliCream.Nitro.CommandLine.Tui.Agents;

/// <summary>
/// The agents <see cref="ITuiMode"/>: a list pane of every registered agent
/// next to a detail pane for the selected agent, modeled on the mail
/// board's own list/detail split (see <see cref="AgentsFocus"/> and
/// <c>MailMode</c>/<c>MailFocus</c>). Moving the list selection reloads the
/// detail pane (identity, assigned tasks, sent mail) through the same
/// <see cref="AgentDetailModel"/>/<see cref="AgentDetailView"/> pair the
/// former pushed <c>AgentDetailMode</c> used; that pushed full-screen mode
/// is gone; there is nothing left for it to do once the detail is always
/// visible next to the list. Enter and h/l/Left/Right toggle which pane
/// holds focus, exactly like the mail board: List focus moves the list
/// selection with j/k, Detail focus scrolls the detail body instead.
/// </summary>
internal sealed class AgentsMode : ITuiMode
{
    /// <summary>
    /// Border and padding columns the list pane's panel spends on either
    /// side of its content.
    /// </summary>
    private const int PanelChromeWidth = 4;

    /// <summary>
    /// Border rows the list pane's panel spends above and below its
    /// content; the header is drawn on the top border row.
    /// </summary>
    private const int PanelChromeHeight = 2;

    /// <summary>
    /// The number of distinct above/below indicator combinations the
    /// list's viewport can settle on, bounding how many times reserving
    /// space for them needs to be recomputed.
    /// </summary>
    private const int MaxIndicatorSettlePasses = 3;

    /// <summary>
    /// The fraction of the frame width the list pane occupies; the detail
    /// pane takes the remainder. Wider than the mail board's own split
    /// since an agent row spends its width on four columns (name, role,
    /// two ages) rather than one truncated subject line.
    /// </summary>
    private const int ListWidthNumerator = 1;
    private const int ListWidthDenominator = 2;

    private readonly TimeProvider _timeProvider;
    private readonly AgentsState _state;
    private readonly AgentDetailModel _detailModel;
    private readonly AgentDetailView _detailView;
    private readonly Viewport _listViewport = new(0, 0);

    public AgentsMode(
        IAgentRegistry registry,
        ITaskStore taskStore,
        IMailStore mailStore,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(taskStore);
        ArgumentNullException.ThrowIfNull(mailStore);

        _timeProvider = timeProvider ?? TimeProvider.System;
        _state = new AgentsState(registry);
        _detailModel = new AgentDetailModel(registry, taskStore, mailStore);
        _detailView = new AgentDetailView(_detailModel, _timeProvider);
    }

    /// <summary>
    /// The mode's current live state: the loaded agents, selection, and
    /// pane focus.
    /// </summary>
    public AgentsState State => _state;

    /// <inheritdoc />
    public KeyMap? KeyMap => null;

    /// <inheritdoc />
    public void OnEnter() => RefreshBlocking();

    /// <inheritdoc />
    public void OnResize(int width, int height)
    {
        // Render(width, height) recomputes the layout and every pane's
        // viewport window from its parameters on every frame, so there is
        // no per-resize state to update ahead of time.
    }

    /// <inheritdoc />
    public IReadOnlyList<TuiMessage> Handle(TuiMessage message) => message switch
    {
        TuiMessage.MoveCursor(CursorDirection.Up) => MoveOrScroll(-1),
        TuiMessage.MoveCursor(CursorDirection.Down) => MoveOrScroll(1),
        TuiMessage.MoveCursor(CursorDirection.Left) => TogglePane(),
        TuiMessage.MoveCursor(CursorDirection.Right) => TogglePane(),
        TuiMessage.MoveToEdge(var edge) => MoveOrScrollToEdge(edge),
        TuiMessage.OpenSelected => FocusDetail(),
        TuiMessage.RefreshRequested => Refresh(),
        TuiMessage.CopySelectedId => CopySelectedId(),
        _ => []
    };

    /// <inheritdoc />
    public IRenderable Render(int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            return new Markup(string.Empty);
        }

        var listWidth = Math.Max(1, width * ListWidthNumerator / ListWidthDenominator);
        var detailWidth = Math.Max(1, width - listWidth);

        return new Layout("agents").SplitColumns(
            new Layout("list", RenderListPane(listWidth, height)).Size(listWidth),
            new Layout("detail", RenderDetailPane(detailWidth, height)));
    }

    /// <summary>
    /// Up/Down moves the list selection (and reloads the detail pane for
    /// the newly selected agent) while the list has focus, or scrolls the
    /// detail body while the detail pane has focus.
    /// </summary>
    private IReadOnlyList<TuiMessage> MoveOrScroll(int delta)
    {
        if (_state.Focus == AgentsFocus.List)
        {
            if (_state.Agents.Count > 0)
            {
                _state.SelectedRow = Math.Clamp(_state.SelectedRow + delta, 0, _state.Agents.Count - 1);
                ReloadDetailIfNeeded();
            }
        }
        else if (delta > 0)
        {
            _detailView.ScrollDown();
        }
        else
        {
            _detailView.ScrollUp();
        }

        return [];
    }

    private IReadOnlyList<TuiMessage> MoveOrScrollToEdge(EdgeTarget edge)
    {
        if (_state.Focus == AgentsFocus.List)
        {
            if (_state.Agents.Count > 0)
            {
                _state.SelectedRow = edge == EdgeTarget.Top ? 0 : _state.Agents.Count - 1;
                ReloadDetailIfNeeded();
            }
        }
        else if (edge == EdgeTarget.Top)
        {
            _detailView.ScrollToTop();
        }
        else
        {
            _detailView.ScrollToBottom();
        }

        return [];
    }

    /// <summary>
    /// Left and Right both flip focus between the two panes: with only two
    /// panes, direction carries no extra meaning, matching the mail board.
    /// </summary>
    private IReadOnlyList<TuiMessage> TogglePane()
    {
        _state.Focus = _state.Focus == AgentsFocus.List ? AgentsFocus.Detail : AgentsFocus.List;
        return [];
    }

    private IReadOnlyList<TuiMessage> FocusDetail()
    {
        _state.Focus = AgentsFocus.Detail;
        return [];
    }

    private IReadOnlyList<TuiMessage> Refresh()
    {
        RefreshBlocking();
        return [];
    }

    private IReadOnlyList<TuiMessage> CopySelectedId()
    {
        var name = _state.SelectedAgent?.Name;

        return name is null
            ? [new TuiMessage.ShowToast("No agent selected.", ToastStyle.Warn)]
            : [new TuiMessage.ShowToast(name, ToastStyle.Info)];
    }

    private IRenderable RenderListPane(int width, int height)
    {
        var focused = _state.Focus == AgentsFocus.List;
        var safeWidth = Math.Max(1, width);
        var contentWidth = Math.Max(0, safeWidth - PanelChromeWidth);
        var interiorHeight = Math.Max(0, height - PanelChromeHeight);

        var lines = RenderListLines(contentWidth, interiorHeight, focused);
        var panel = ColumnPane.Render("Agents", _state.Agents.Count, lines, focused);
        panel.Width = safeWidth;
        panel.Height = Math.Max(1, height);

        return panel;
    }

    private IRenderable RenderDetailPane(int width, int height)
        => _detailView.Render(width, height, _state.Focus == AgentsFocus.Detail);

    /// <summary>
    /// Renders the visible rows: the scrolled agent badges, padded with
    /// blank lines so the panel reports a stable line count, with "N more
    /// above/below" indicators reserving their own rows once the agents no
    /// longer fit <paramref name="interiorHeight"/>. Column widths are
    /// computed from this call's visible slice, so they track whichever
    /// rows are actually on screen as the list scrolls.
    /// </summary>
    private IReadOnlyList<string> RenderListLines(int contentWidth, int interiorHeight, bool focused)
    {
        if (interiorHeight <= 0)
        {
            return [];
        }

        var agents = _state.Agents;
        var reservedRows = 0;

        for (var pass = 0; pass < MaxIndicatorSettlePasses; pass++)
        {
            var windowHeight = Math.Max(0, interiorHeight - reservedRows);
            _listViewport.Update(agents.Count, windowHeight);
            _listViewport.EnsureVisible(_state.SelectedRow);

            var needed = (_listViewport.HiddenAbove > 0 ? 1 : 0) + (_listViewport.HiddenBelow > 0 ? 1 : 0);

            if (needed == reservedRows)
            {
                break;
            }

            reservedRows = needed;
        }

        var (start, visibleCount) = _listViewport.Slice();
        var now = _timeProvider.GetUtcNow();
        var visibleAgents = new List<AgentRecord>(visibleCount);

        for (var i = 0; i < visibleCount; i++)
        {
            visibleAgents.Add(agents[start + i]);
        }

        var widths = AgentRowBadge.ComputeWidths(visibleAgents, now);
        var lines = new List<string>(interiorHeight);

        if (_listViewport.HiddenAbove > 0)
        {
            lines.Add(FormatIndicator(_listViewport.HiddenAbove, "above"));
        }

        for (var i = 0; i < visibleCount; i++)
        {
            var selected = focused && start + i == _state.SelectedRow;
            lines.Add(AgentRowBadge.Render(visibleAgents[i], now, selected, contentWidth, widths));
        }

        if (_listViewport.HiddenBelow > 0)
        {
            lines.Add(FormatIndicator(_listViewport.HiddenBelow, "below"));
        }

        while (lines.Count < interiorHeight)
        {
            lines.Add(string.Empty);
        }

        return lines;
    }

    private static string FormatIndicator(int hiddenCount, string direction) => $"  {hiddenCount} more {direction}";

    private void RefreshBlocking()
    {
        _state.RefreshAsync(CancellationToken.None).GetAwaiter().GetResult();
        ReloadDetailIfNeeded();
    }

    /// <summary>
    /// Reloads the detail pane through <see cref="AgentDetailModel"/> when
    /// the selected agent differs from whichever agent it last loaded. A
    /// no-op when the selection hasn't actually changed (for example a
    /// clamped move at the list's edge), so scrolling doesn't re-issue the
    /// tasks/mail queries on every keypress.
    /// </summary>
    private void ReloadDetailIfNeeded()
    {
        var selectedName = _state.SelectedAgent?.Name;

        if (selectedName is null || selectedName == _detailModel.CurrentAgentName)
        {
            return;
        }

        _detailModel.LoadAsync(selectedName, CancellationToken.None).GetAwaiter().GetResult();
    }
}
