using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Tui.Input;
using ChilliCream.Nitro.CommandLine.Tui.Shell;
using ChilliCream.Nitro.CommandLine.Tui.Widgets;
using Spectre.Console.Rendering;
using CursorDirection = ChilliCream.Nitro.CommandLine.Tui.Input.CursorDirection;

namespace ChilliCream.Nitro.CommandLine.Tui.Mail;

/// <summary>
/// The mail board <see cref="ITuiMode"/>: a message list pane next to a
/// detail pane for the selected message, with a key toggling the detail
/// pane between one message and its whole thread. Read-only: no gesture
/// this mode handles marks a message read, archives it, or writes
/// anything back to the store.
/// </summary>
internal sealed class MailMode : ITuiMode
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
    /// pane takes the remainder.
    /// </summary>
    private const int ListWidthNumerator = 2;
    private const int ListWidthDenominator = 5;

    private readonly MailState _state;
    private readonly MailDetailView _detailView = new();
    private readonly TimeProvider _timeProvider;
    private readonly Viewport _listViewport = new(0, 0);

    public MailMode(IMailStore store, string actor, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(actor);

        _state = new MailState(actor, new MailDataLoader(store));
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    /// The board's current live state: messages, filter, selection, and
    /// focus.
    /// </summary>
    public MailState State => _state;

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
        TuiMessage.CycleView(var delta) => CycleFilter(delta),
        TuiMessage.ToggleMaximize => ToggleThreadView(),
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

        return new Layout("mail").SplitColumns(
            new Layout("list", RenderListPane(listWidth, height)).Size(listWidth),
            new Layout("detail", RenderDetailPane(detailWidth, height)));
    }

    /// <summary>
    /// Up/Down moves the list selection while the list has focus, or
    /// scrolls the detail body while the detail pane has focus.
    /// </summary>
    private IReadOnlyList<TuiMessage> MoveOrScroll(int delta)
    {
        if (_state.Focus == MailFocus.List)
        {
            if (_state.Messages.Count > 0)
            {
                _state.SelectedRow = Math.Clamp(_state.SelectedRow + delta, 0, _state.Messages.Count - 1);
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
        if (_state.Focus == MailFocus.List)
        {
            if (_state.Messages.Count > 0)
            {
                _state.SelectedRow = edge == EdgeTarget.Top ? 0 : _state.Messages.Count - 1;
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
    /// panes, direction carries no extra meaning.
    /// </summary>
    private IReadOnlyList<TuiMessage> TogglePane()
    {
        _state.Focus = _state.Focus == MailFocus.List ? MailFocus.Detail : MailFocus.List;
        return [];
    }

    private IReadOnlyList<TuiMessage> FocusDetail()
    {
        _state.Focus = MailFocus.Detail;
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
        _detailView.ResetScroll();
        return [];
    }

    private IReadOnlyList<TuiMessage> ToggleThreadView()
    {
        if (_state.ViewMode == MailViewMode.Thread)
        {
            _state.ShowMessage();
            _detailView.ResetScroll();
            return [];
        }

        var opened = _state.ShowThreadAsync(CancellationToken.None).GetAwaiter().GetResult();
        _detailView.ResetScroll();

        return opened ? [] : [new TuiMessage.ShowToast("No message selected.", ToastStyle.Warn)];
    }

    private IReadOnlyList<TuiMessage> CopySelectedId()
    {
        var id = _state.SelectedMessage?.Id;

        return id is null
            ? [new TuiMessage.ShowToast("No message selected.", ToastStyle.Warn)]
            : [new TuiMessage.ShowToast(id, ToastStyle.Info)];
    }

    private IRenderable RenderListPane(int width, int height)
    {
        var focused = _state.Focus == MailFocus.List;
        var safeWidth = Math.Max(1, width);
        var contentWidth = Math.Max(0, safeWidth - PanelChromeWidth);
        var interiorHeight = Math.Max(0, height - PanelChromeHeight);
        var now = _timeProvider.GetUtcNow();

        var lines = RenderListLines(contentWidth, interiorHeight, focused, now);
        var panel = ColumnPane.Render(FilterName(_state.Filter), _state.Messages.Count, lines, focused);
        panel.Width = safeWidth;
        panel.Height = Math.Max(1, height);

        return panel;
    }

    private IRenderable RenderDetailPane(int width, int height)
        => _detailView.Render(_state, width, height, _state.Focus == MailFocus.Detail);

    /// <summary>
    /// Renders the list's visible rows: the scrolled message badges,
    /// padded with blank lines so the panel reports a stable line count,
    /// with "N more above/below" indicators reserving their own rows once
    /// the messages no longer fit <paramref name="interiorHeight"/>.
    /// </summary>
    private IReadOnlyList<string> RenderListLines(
        int contentWidth, int interiorHeight, bool focused, DateTimeOffset now)
    {
        if (interiorHeight <= 0)
        {
            return [];
        }

        var messages = _state.Messages;
        var reservedRows = 0;

        for (var pass = 0; pass < MaxIndicatorSettlePasses; pass++)
        {
            var windowHeight = Math.Max(0, interiorHeight - reservedRows);
            _listViewport.Update(messages.Count, windowHeight);
            _listViewport.EnsureVisible(_state.SelectedRow);

            var needed = (_listViewport.HiddenAbove > 0 ? 1 : 0) + (_listViewport.HiddenBelow > 0 ? 1 : 0);

            if (needed == reservedRows)
            {
                break;
            }

            reservedRows = needed;
        }

        var (start, visibleCount) = _listViewport.Slice();
        var lines = new List<string>(interiorHeight);

        if (_listViewport.HiddenAbove > 0)
        {
            lines.Add(FormatIndicator(_listViewport.HiddenAbove, "above"));
        }

        for (var i = 0; i < visibleCount; i++)
        {
            var message = messages[start + i];
            var selected = focused && start + i == _state.SelectedRow;
            lines.Add(MailMessageBadge.Render(message, _state.Actor, now, selected, contentWidth));
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

    private static string FilterName(MailListFilter filter) => filter switch
    {
        MailListFilter.Unread => "Unread",
        MailListFilter.Archived => "Archived",
        _ => "Inbox"
    };

    private void RefreshBlocking() => _state.RefreshAsync(CancellationToken.None).GetAwaiter().GetResult();
}
