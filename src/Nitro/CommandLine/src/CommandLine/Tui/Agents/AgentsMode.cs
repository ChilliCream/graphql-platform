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
/// Displays live participants beside the selected participant's details.
/// Enter focuses the detail pane; horizontal navigation toggles pane focus, and
/// vertical navigation moves the list selection or scrolls the focused detail pane.
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
    /// The maximum number of passes used to reserve viewport indicator rows.
    /// </summary>
    private const int MaxIndicatorSettlePasses = 3;

    /// <summary>
    /// The fraction of the frame width the list pane occupies; the detail pane takes the remainder.
    /// </summary>
    private const int ListWidthNumerator = 1;
    private const int ListWidthDenominator = 2;

    private readonly TimeProvider _timeProvider;
    private readonly AgentsState _state;
    private readonly AgentDetailModel _detailModel;
    private readonly AgentDetailView _detailView;
    private readonly Viewport _listViewport = new(0, 0);

    public AgentsMode(
        ITaskStore taskStore,
        IMailStore mailStore,
        IAgentSessionRegistry sessionRegistry,
        IClaudeSessionActivityReader activityReader,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(taskStore);
        ArgumentNullException.ThrowIfNull(mailStore);
        ArgumentNullException.ThrowIfNull(sessionRegistry);
        ArgumentNullException.ThrowIfNull(activityReader);

        _timeProvider = timeProvider ?? TimeProvider.System;
        _state = new AgentsState(sessionRegistry, activityReader);
        _detailModel = new AgentDetailModel(taskStore, mailStore);
        _detailView = new AgentDetailView(_detailModel, _timeProvider);
    }

    /// <summary>
    /// The mode's current live state: the loaded participants, selection,
    /// and pane focus.
    /// </summary>
    public AgentsState State => _state;

    /// <inheritdoc />
    public KeyMap? KeyMap => null;

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
    /// the newly selected participant) while the list has focus, or scrolls
    /// the detail body while the detail pane has focus.
    /// </summary>
    private IReadOnlyList<TuiMessage> MoveOrScroll(int delta)
    {
        if (_state.Focus == AgentsFocus.List)
        {
            if (_state.Rows.Count > 0)
            {
                _state.SelectedRow = Math.Clamp(_state.SelectedRow + delta, 0, _state.Rows.Count - 1);
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
            if (_state.Rows.Count > 0)
            {
                _state.SelectedRow = edge == EdgeTarget.Top ? 0 : _state.Rows.Count - 1;
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
    /// Left and Right both flip focus between the two panes.
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
        var sessionId = _state.SelectedParticipant?.Participant.Session.SessionId;

        return sessionId is null
            ? [new TuiMessage.ShowToast("No session selected.", ToastStyle.Warn)]
            : [new TuiMessage.ShowToast(sessionId, ToastStyle.Info)];
    }

    private IRenderable RenderListPane(int width, int height)
    {
        var focused = _state.Focus == AgentsFocus.List;
        var safeWidth = Math.Max(1, width);
        var contentWidth = Math.Max(0, safeWidth - PanelChromeWidth);
        var interiorHeight = Math.Max(0, height - PanelChromeHeight);

        var lines = RenderListLines(contentWidth, interiorHeight, focused);
        var panel = ColumnPane.Render("Agents", _state.Rows.Count, lines, focused);
        panel.Width = safeWidth;
        panel.Height = Math.Max(1, height);

        return panel;
    }

    private IRenderable RenderDetailPane(int width, int height)
        => _detailView.Render(width, height, _state.Focus == AgentsFocus.Detail);

    /// <summary>
    /// Renders the visible rows, padded with blank lines to <paramref name="interiorHeight"/>, with "N
    /// more above/below" indicators once the participants no longer fit. Column widths are computed
    /// from this call's visible slice.
    /// </summary>
    private IReadOnlyList<string> RenderListLines(int contentWidth, int interiorHeight, bool focused)
    {
        if (interiorHeight <= 0)
        {
            return [];
        }

        var rows = _state.Rows;
        var reservedRows = 0;

        for (var pass = 0; pass < MaxIndicatorSettlePasses; pass++)
        {
            var windowHeight = Math.Max(0, interiorHeight - reservedRows);
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
        var now = _timeProvider.GetUtcNow();
        var visibleRows = new List<AgentParticipantRow>(visibleCount);

        for (var i = 0; i < visibleCount; i++)
        {
            visibleRows.Add(rows[start + i]);
        }

        var widths = AgentRowBadge.ComputeWidths(visibleRows, now);
        var lines = new List<string>(interiorHeight);

        if (_listViewport.HiddenAbove > 0)
        {
            lines.Add(FormatIndicator(_listViewport.HiddenAbove, "above"));
        }

        for (var i = 0; i < visibleCount; i++)
        {
            var selected = focused && start + i == _state.SelectedRow;
            lines.Add(AgentRowBadge.Render(visibleRows[i], now, selected, contentWidth, widths));
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
        RefreshDetail();
    }

    /// <summary>
    /// Loads details when the selected participant differs from the loaded one.
    /// Does nothing when no participant is selected or its key is unchanged.
    /// </summary>
    private void ReloadDetailIfNeeded()
    {
        var selected = _state.SelectedParticipant;

        if (selected is null || _detailModel.CurrentKey == selected.Key)
        {
            return;
        }

        _detailModel.LoadAsync(selected.Participant, CancellationToken.None).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Reloads the detail pane for the currently selected participant, unconditionally, or clears it
    /// when nothing is selected.
    /// </summary>
    private void RefreshDetail()
    {
        var selected = _state.SelectedParticipant;

        if (selected is null)
        {
            _detailModel.Clear();
            return;
        }

        _detailModel.LoadAsync(selected.Participant, CancellationToken.None).GetAwaiter().GetResult();
    }
}
