using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tui.Input;
using ChilliCream.Nitro.CommandLine.Tui.Shell;
using ChilliCream.Nitro.CommandLine.Tui.Widgets;
using Spectre.Console.Rendering;
using CursorDirection = ChilliCream.Nitro.CommandLine.Tui.Input.CursorDirection;

namespace ChilliCream.Nitro.CommandLine.Tui.Agents;

/// <summary>
/// The agents <see cref="ITuiMode"/>: a single scrollable list of every
/// registered agent, read-only against <see cref="IAgentRegistry"/>. Modeled
/// on the board and mail list panes (see <c>Tui/Board</c> and
/// <c>Tui/Mail</c>): one bordered panel of rendered rows, a
/// <see cref="Viewport"/> tracking the visible window, and j/k/arrow
/// selection through the shared global key map (<see cref="KeyMap"/> is
/// null, same as <c>BoardMode</c>). Enter on a row is wired in a follow-up
/// detail bead; here <see cref="TuiMessage.OpenSelected"/> is a no-op.
/// </summary>
internal sealed class AgentsMode : ITuiMode
{
    /// <summary>
    /// Border and padding columns the panel spends on either side of its
    /// content.
    /// </summary>
    private const int PanelChromeWidth = 4;

    /// <summary>
    /// Border rows the panel spends above and below its content; the header
    /// is drawn on the top border row.
    /// </summary>
    private const int PanelChromeHeight = 2;

    /// <summary>
    /// The number of distinct above/below indicator combinations the
    /// viewport can settle on, bounding how many times reserving space for
    /// them needs to be recomputed.
    /// </summary>
    private const int MaxIndicatorSettlePasses = 3;

    private readonly TimeProvider _timeProvider;
    private readonly AgentsState _state;
    private readonly Viewport _viewport = new(0, 0);

    public AgentsMode(IAgentRegistry registry, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(registry);

        _timeProvider = timeProvider ?? TimeProvider.System;
        _state = new AgentsState(registry);
    }

    /// <summary>
    /// The mode's current live state: the loaded agents and selection.
    /// </summary>
    public AgentsState State => _state;

    /// <inheritdoc />
    public KeyMap? KeyMap => null;

    /// <inheritdoc />
    public void OnEnter() => RefreshBlocking();

    /// <inheritdoc />
    public void OnResize(int width, int height)
    {
        // Render(width, height) recomputes the viewport window from its
        // parameters on every frame, so there is no per-resize state to
        // update ahead of time.
    }

    /// <inheritdoc />
    public IReadOnlyList<TuiMessage> Handle(TuiMessage message) => message switch
    {
        TuiMessage.MoveCursor(CursorDirection.Up) => MoveSelection(-1),
        TuiMessage.MoveCursor(CursorDirection.Down) => MoveSelection(1),
        TuiMessage.MoveToEdge(EdgeTarget.Top) => MoveSelectionToEdge(top: true),
        TuiMessage.MoveToEdge(EdgeTarget.Bottom) => MoveSelectionToEdge(top: false),
        TuiMessage.RefreshRequested => Refresh(),
        TuiMessage.CopySelectedId => CopySelectedId(),
        // Row detail is a follow-up bead; selecting a row here does nothing.
        TuiMessage.OpenSelected => [],
        _ => []
    };

    /// <inheritdoc />
    public IRenderable Render(int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            return new Markup(string.Empty);
        }

        var safeWidth = Math.Max(1, width);
        var contentWidth = Math.Max(0, safeWidth - PanelChromeWidth);
        var interiorHeight = Math.Max(0, height - PanelChromeHeight);

        var lines = RenderLines(contentWidth, interiorHeight);
        var panel = ColumnPane.Render("Agents", _state.Agents.Count, lines, focused: true);
        panel.Width = safeWidth;
        panel.Height = Math.Max(1, height);

        return panel;
    }

    private IReadOnlyList<TuiMessage> MoveSelection(int delta)
    {
        if (_state.Agents.Count > 0)
        {
            _state.SelectedRow = Math.Clamp(_state.SelectedRow + delta, 0, _state.Agents.Count - 1);
        }

        return [];
    }

    private IReadOnlyList<TuiMessage> MoveSelectionToEdge(bool top)
    {
        if (_state.Agents.Count > 0)
        {
            _state.SelectedRow = top ? 0 : _state.Agents.Count - 1;
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
        var name = _state.SelectedAgent?.Name;

        return name is null
            ? [new TuiMessage.ShowToast("No agent selected.", ToastStyle.Warn)]
            : [new TuiMessage.ShowToast(name, ToastStyle.Info)];
    }

    /// <summary>
    /// Renders the visible rows: the scrolled agent badges, padded with
    /// blank lines so the panel reports a stable line count, with "N more
    /// above/below" indicators reserving their own rows once the agents no
    /// longer fit <paramref name="interiorHeight"/>.
    /// </summary>
    private IReadOnlyList<string> RenderLines(int contentWidth, int interiorHeight)
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
            _viewport.Update(agents.Count, windowHeight);
            _viewport.EnsureVisible(_state.SelectedRow);

            var needed = (_viewport.HiddenAbove > 0 ? 1 : 0) + (_viewport.HiddenBelow > 0 ? 1 : 0);

            if (needed == reservedRows)
            {
                break;
            }

            reservedRows = needed;
        }

        var (start, visibleCount) = _viewport.Slice();
        var lines = new List<string>(interiorHeight);
        var now = _timeProvider.GetUtcNow();

        if (_viewport.HiddenAbove > 0)
        {
            lines.Add(FormatIndicator(_viewport.HiddenAbove, "above"));
        }

        for (var i = 0; i < visibleCount; i++)
        {
            var agent = agents[start + i];
            var selected = start + i == _state.SelectedRow;
            lines.Add(AgentRowBadge.Render(agent, now, selected, contentWidth));
        }

        if (_viewport.HiddenBelow > 0)
        {
            lines.Add(FormatIndicator(_viewport.HiddenBelow, "below"));
        }

        while (lines.Count < interiorHeight)
        {
            lines.Add(string.Empty);
        }

        return lines;
    }

    private static string FormatIndicator(int hiddenCount, string direction) => $"  {hiddenCount} more {direction}";

    private void RefreshBlocking() => _state.RefreshAsync(CancellationToken.None).GetAwaiter().GetResult();
}
