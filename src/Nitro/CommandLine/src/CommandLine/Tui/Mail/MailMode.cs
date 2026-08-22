using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Tui.Editing;
using ChilliCream.Nitro.CommandLine.Tui.Input;
using ChilliCream.Nitro.CommandLine.Tui.Shell;
using ChilliCream.Nitro.CommandLine.Tui.Theming;
using ChilliCream.Nitro.CommandLine.Tui.Widgets;
using ChilliCream.Nitro.CommandLine.Tui.Widgets.Form;
using Spectre.Console.Rendering;
using ConfirmDialog = ChilliCream.Nitro.CommandLine.Tui.Editing.ConfirmDialog;
using CursorDirection = ChilliCream.Nitro.CommandLine.Tui.Input.CursorDirection;

namespace ChilliCream.Nitro.CommandLine.Tui.Mail;

/// <summary>
/// Which form a <see cref="MailMode"/>'s active discard confirmation
/// applies to.
/// </summary>
internal enum MailDiscardTarget
{
    Compose,
    Reply
}

/// <summary>
/// The mail board <see cref="ITuiMode"/>: a message list pane next to a
/// detail pane for the selected message, with a key toggling the detail
/// pane between one message and its whole thread. Opening a message in the
/// detail pane marks it read for the actor; the u, a, r, and c gestures
/// toggle read/unread, archive (behind a confirmation), reply, and compose,
/// every write going through the same store operations the CLI uses. The
/// Shift+I/S/L/W gestures jump directly to the Inbox/Sent/All/Workspace
/// <see cref="MailMailbox"/>; u and a are refused with a toast, instead of
/// reaching the store, on any message the actor is not a recipient of.
/// <see cref="MailMailbox.Workspace"/> shows every agent's mail, so it is
/// read-only by default: u, a, c, and r are all refused with
/// <see cref="MailLifecycleActions.WorkspaceReadOnlyMessage"/> there
/// (see <see cref="MailLifecycleActions.IsReadOnly"/>), regardless of
/// whether the actor happens to be a recipient of the selected message,
/// rather than narrowing case by case. Workspace carries two redundant
/// mode indicators so the read-only default is never the list's only
/// signal: the list pane's own header names the mailbox (<see cref="HeaderName"/>)
/// and its border takes a distinct accent (<see cref="ResolveListBorderToken"/>).
/// This mode owns its own modal overlays (the archive confirmation, the
/// compose and reply forms, and their shared discard confirmation) rather
/// than routing through <see cref="TuiShell"/>'s task-specific overlay
/// fields.
/// </summary>
internal sealed class MailMode : ITuiMode, IRawKeyCapturingMode
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

    private readonly IMailStore _store;
    private readonly MailState _state;
    private readonly MailDetailView _detailView = new();
    private readonly TimeProvider _timeProvider;
    private readonly Viewport _listViewport = new(0, 0);

    private ConfirmDialog? _archiveDialog;
    private MailMessage? _archiveTarget;
    private MailComposeForm? _composeForm;
    private MailReplyForm? _replyForm;
    private ConfirmDialog? _discardDialog;
    private MailDiscardTarget _discardTarget;

    public MailMode(IMailStore store, string actor, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(actor);

        _store = store;
        _state = new MailState(actor, new MailDataLoader(store));
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    /// The board's current live state: messages, filter, selection, and
    /// focus.
    /// </summary>
    public MailState State => _state;

    /// <summary>
    /// Whether this mode currently owns an active overlay (the archive
    /// confirmation, the compose or reply form, or their shared discard
    /// confirmation) that must consume raw key input directly rather than
    /// through the semantic <see cref="TuiMessage"/> dispatch every other
    /// gesture goes through.
    /// </summary>
    public bool IsInputCapturing
        => _archiveDialog is not null
        || _composeForm is not null
        || _replyForm is not null
        || _discardDialog is not null;

    /// <inheritdoc />
    public IReadOnlyList<KeyHint> CapturingHints
        => _discardDialog is not null ? ConfirmDialog.Hints
        : _archiveDialog is not null ? ConfirmDialog.Hints
        : _composeForm is not null ? MailComposeForm.Hints
        : _replyForm is not null ? MailReplyForm.Hints
        : [];

    /// <summary>
    /// How many messages addressed to the actor are unread and not
    /// archived, as of the last refresh. Drives a hosting tab's unread
    /// badge.
    /// </summary>
    public int UnreadCount { get; private set; }

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
        TuiMessage.ToggleReadRequested => ToggleRead(),
        TuiMessage.ArchiveRequested => OpenArchiveDialog(),
        TuiMessage.ComposeRequested => OpenComposeForm(),
        TuiMessage.ReplyRequested => OpenReplyForm(),
        TuiMessage.SelectInboxRequested => SelectMailbox(MailMailbox.Inbox),
        TuiMessage.SelectSentRequested => SelectMailbox(MailMailbox.Sent),
        TuiMessage.SelectAllMailRequested => SelectMailbox(MailMailbox.All),
        TuiMessage.SelectWorkspaceMailRequested => SelectMailbox(MailMailbox.Workspace),
        _ => []
    };

    /// <summary>
    /// Handles one raw key while <see cref="IsInputCapturing"/> is true:
    /// routed here by the host instead of through the semantic
    /// <see cref="TuiMessage"/> dispatch, since the active overlay's text
    /// fields need raw characters, not key-bound intents.
    /// </summary>
    public IReadOnlyList<TuiMessage> HandleRawKey(ConsoleKeyInfo info)
    {
        if (_discardDialog is not null)
        {
            return HandleDiscardDialogKey(info);
        }

        if (_archiveDialog is not null)
        {
            return HandleArchiveDialogKey(info);
        }

        if (_composeForm is not null)
        {
            return HandleComposeFormKey(info);
        }

        if (_replyForm is not null)
        {
            return HandleReplyFormKey(info);
        }

        return [];
    }

    /// <inheritdoc />
    public IRenderable Render(int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            return new Markup(string.Empty);
        }

        if (_discardDialog is { } discardDialog)
        {
            return discardDialog.Render(width, height);
        }

        if (_archiveDialog is { } archiveDialog)
        {
            return archiveDialog.Render(width, height);
        }

        if (_composeForm is { } composeForm)
        {
            return composeForm.Render(width, height);
        }

        if (_replyForm is { } replyForm)
        {
            return replyForm.Render(width, height);
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
        return _state.Focus == MailFocus.Detail ? MaybeMarkSelectedRead() : [];
    }

    private IReadOnlyList<TuiMessage> FocusDetail()
    {
        _state.Focus = MailFocus.Detail;
        return MaybeMarkSelectedRead();
    }

    /// <summary>
    /// Marks the selected message read for the actor when it is currently
    /// unread, aligning the detail pane's "open" gesture with the CLI's
    /// read semantics. Silent on success; a failed write still surfaces as
    /// a toast.
    /// </summary>
    private IReadOnlyList<TuiMessage> MaybeMarkSelectedRead()
    {
        if (_state.SelectedMessage is not { } message || !MailRecipientView.IsUnread(message, _state.Actor))
        {
            return [];
        }

        var outcome = MailLifecycleActions.MarkReadAsync(_store, message, _state.Actor, CancellationToken.None)
            .GetAwaiter().GetResult();

        RefreshBlocking();

        return outcome is MailActionOutcome.Failed ? [outcome.ToShowToast()] : [];
    }

    /// <summary>
    /// Toggles the selected message between read and unread for the actor.
    /// Refused with a toast, rather than reaching the store, when
    /// <see cref="MailLifecycleActions.IsReadOnly"/> or when the actor has
    /// no recipient row on the message (for example most messages in the
    /// Sent mailbox): the store would otherwise reject the write with an
    /// <see cref="ExitException"/>.
    /// </summary>
    private IReadOnlyList<TuiMessage> ToggleRead()
    {
        if (RefuseIfReadOnly() is { } refused)
        {
            return refused;
        }

        if (_state.SelectedMessage is not { } message)
        {
            return [new TuiMessage.ShowToast("No message selected.", ToastStyle.Warn)];
        }

        if (MailRecipientView.FindRecipient(message, _state.Actor) is null)
        {
            return [new TuiMessage.ShowToast(NotARecipientMessage(message), ToastStyle.Warn)];
        }

        var outcome = MailLifecycleActions.ToggleReadAsync(_store, message, _state.Actor, CancellationToken.None)
            .GetAwaiter().GetResult();

        RefreshBlocking();

        return [outcome.ToShowToast()];
    }

    /// <summary>
    /// Opens the archive confirmation for the selected message. Refused
    /// with a toast, rather than reaching the store, when
    /// <see cref="MailLifecycleActions.IsReadOnly"/> or when the actor has
    /// no recipient row on the message; see <see cref="ToggleRead"/>.
    /// </summary>
    private IReadOnlyList<TuiMessage> OpenArchiveDialog()
    {
        if (RefuseIfReadOnly() is { } refused)
        {
            return refused;
        }

        if (_state.SelectedMessage is not { } message)
        {
            return [new TuiMessage.ShowToast("No message selected.", ToastStyle.Warn)];
        }

        if (MailRecipientView.FindRecipient(message, _state.Actor) is null)
        {
            return [new TuiMessage.ShowToast(NotARecipientMessage(message), ToastStyle.Warn)];
        }

        _archiveTarget = message;
        _archiveDialog = MailLifecycleActions.CreateArchiveDialog(message);
        return [];
    }

    private static string NotARecipientMessage(MailMessage message)
        => $"'{message.Id}' has no read/unread or archive state here.";

    /// <summary>
    /// Opens the compose form. Unlike reply and archive, composing needs no
    /// selected message, but is refused the same as every other mutating
    /// gesture when <see cref="MailLifecycleActions.IsReadOnly"/>.
    /// </summary>
    private IReadOnlyList<TuiMessage> OpenComposeForm()
    {
        if (RefuseIfReadOnly() is { } refused)
        {
            return refused;
        }

        _composeForm = new MailComposeForm();
        return [];
    }

    /// <summary>
    /// Opens the reply form for the selected message. Refused the same as
    /// every other mutating gesture when
    /// <see cref="MailLifecycleActions.IsReadOnly"/>, even for a thread the
    /// actor participates in and the store would otherwise accept a reply
    /// on; see <see cref="MailLifecycleActions.WorkspaceReadOnlyMessage"/>
    /// for why this mode does not special-case that.
    /// </summary>
    private IReadOnlyList<TuiMessage> OpenReplyForm()
    {
        if (RefuseIfReadOnly() is { } refused)
        {
            return refused;
        }

        if (_state.SelectedMessage is not { } message)
        {
            return [new TuiMessage.ShowToast("No message selected.", ToastStyle.Warn)];
        }

        _replyForm = new MailReplyForm(message);
        return [];
    }

    /// <summary>
    /// The shared guard behind <see cref="ToggleRead"/>, <see cref="OpenArchiveDialog"/>,
    /// <see cref="OpenComposeForm"/>, and <see cref="OpenReplyForm"/>: a
    /// refusal toast when <see cref="MailLifecycleActions.IsReadOnly"/> is
    /// true for <see cref="MailState.Mailbox"/>, or null when the gesture
    /// may proceed.
    /// </summary>
    private IReadOnlyList<TuiMessage>? RefuseIfReadOnly()
        => MailLifecycleActions.IsReadOnly(_state.Mailbox)
            ? [new TuiMessage.ShowToast(MailLifecycleActions.WorkspaceReadOnlyMessage, ToastStyle.Warn)]
            : null;

    private IReadOnlyList<TuiMessage> HandleArchiveDialogKey(ConsoleKeyInfo info)
    {
        var result = _archiveDialog!.HandleKey(info);

        return result switch
        {
            null => [],
            ConfirmDialogResult.Cancelled => CancelArchiveDialog(),
            ConfirmDialogResult.Confirmed => SubmitArchive(),
            _ => []
        };
    }

    private IReadOnlyList<TuiMessage> CancelArchiveDialog()
    {
        _archiveDialog = null;
        _archiveTarget = null;
        return [];
    }

    private IReadOnlyList<TuiMessage> SubmitArchive()
    {
        var target = _archiveTarget!;
        _archiveDialog = null;
        _archiveTarget = null;

        var outcome = MailLifecycleActions.ArchiveAsync(_store, target, _state.Actor, CancellationToken.None)
            .GetAwaiter().GetResult();

        RefreshBlocking();

        return [outcome.ToShowToast()];
    }

    private IReadOnlyList<TuiMessage> HandleComposeFormKey(ConsoleKeyInfo info)
    {
        var result = _composeForm!.HandleKey(info);

        return result switch
        {
            null => [],
            FormResult.Cancelled => TryDiscardCompose(),
            FormResult.ButtonActivated { ButtonId: MailComposeForm.CancelButtonId } => TryDiscardCompose(),
            FormResult.Submitted submitted => SubmitCompose(submitted),
            _ => []
        };
    }

    private IReadOnlyList<TuiMessage> TryDiscardCompose()
    {
        if (_composeForm!.IsDirty)
        {
            _discardTarget = MailDiscardTarget.Compose;
            _discardDialog = CreateDiscardDialog();
            return [];
        }

        _composeForm = null;
        return [];
    }

    private IReadOnlyList<TuiMessage> SubmitCompose(FormResult.Submitted submitted)
    {
        var outcome = _composeForm!.SubmitAsync(_store, submitted.Values, _state.Actor, CancellationToken.None)
            .GetAwaiter().GetResult();

        if (outcome is not MailSendOutcome.Succeeded)
        {
            return [outcome.ToShowToast()];
        }

        _composeForm = null;
        RefreshBlocking();

        return [outcome.ToShowToast()];
    }

    private IReadOnlyList<TuiMessage> HandleReplyFormKey(ConsoleKeyInfo info)
    {
        var result = _replyForm!.HandleKey(info);

        return result switch
        {
            null => [],
            FormResult.Cancelled => TryDiscardReply(),
            FormResult.ButtonActivated { ButtonId: MailReplyForm.CancelButtonId } => TryDiscardReply(),
            FormResult.Submitted submitted => SubmitReply(submitted),
            _ => []
        };
    }

    private IReadOnlyList<TuiMessage> TryDiscardReply()
    {
        if (_replyForm!.IsDirty)
        {
            _discardTarget = MailDiscardTarget.Reply;
            _discardDialog = CreateDiscardDialog();
            return [];
        }

        _replyForm = null;
        return [];
    }

    private IReadOnlyList<TuiMessage> SubmitReply(FormResult.Submitted submitted)
    {
        var outcome = _replyForm!.SubmitAsync(_store, submitted.Values, _state.Actor, CancellationToken.None)
            .GetAwaiter().GetResult();

        if (outcome is not MailSendOutcome.Succeeded)
        {
            return [outcome.ToShowToast()];
        }

        _replyForm = null;
        RefreshBlocking();

        return [outcome.ToShowToast()];
    }

    private IReadOnlyList<TuiMessage> HandleDiscardDialogKey(ConsoleKeyInfo info)
    {
        var result = _discardDialog!.HandleKey(info);

        return result switch
        {
            null => [],
            ConfirmDialogResult.Confirmed => ConfirmDiscard(),
            ConfirmDialogResult.Cancelled => CancelDiscard(),
            _ => []
        };
    }

    private IReadOnlyList<TuiMessage> ConfirmDiscard()
    {
        _discardDialog = null;

        if (_discardTarget == MailDiscardTarget.Compose)
        {
            _composeForm = null;
        }
        else
        {
            _replyForm = null;
        }

        return [];
    }

    private IReadOnlyList<TuiMessage> CancelDiscard()
    {
        _discardDialog = null;
        return [];
    }

    private static ConfirmDialog CreateDiscardDialog()
        => new("Discard unsaved changes?", "Discard", ButtonKind.Danger);

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

    private IReadOnlyList<TuiMessage> SelectMailbox(MailMailbox mailbox)
    {
        _state.SelectMailboxAsync(mailbox, CancellationToken.None).GetAwaiter().GetResult();
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
        var panel = BuildListPanel(HeaderName(_state), _state.Messages.Count, lines, focused);
        panel.Width = safeWidth;
        panel.Height = Math.Max(1, height);

        return panel;
    }

    /// <summary>
    /// Builds the list pane's panel directly, rather than through
    /// <see cref="ColumnPane"/>, so <see cref="ResolveListBorderToken"/> can
    /// give <see cref="MailMailbox.Workspace"/> a distinct border accent:
    /// the second of the mode's two redundant Workspace indicators (see the
    /// class doc), alongside the header text this still renders the same
    /// way <see cref="ColumnPane"/> does. Every other mailbox resolves to
    /// exactly the border tokens <see cref="ColumnPane"/> itself uses.
    /// </summary>
    private Panel BuildListPanel(string name, int count, IReadOnlyList<string> lines, bool focused)
    {
        IRenderable content = lines.Count == 0
            ? new Markup(string.Empty)
            : new Rows(lines.Select(line => (IRenderable)new Markup(line)));

        var borderToken = ResolveListBorderToken(_state.Mailbox, focused);

        return new Panel(content)
        {
            Header = new PanelHeader($"{name} ({count})"),
            Border = BoxBorder.Rounded,
            BorderStyle = ThemeTokens.GetStyle(borderToken)
        };
    }

    /// <summary>
    /// The list pane's border token for <paramref name="mailbox"/>:
    /// <see cref="MailMailbox.Workspace"/>'s own distinct accent, or the
    /// plain <c>board.column.border</c> family every other mailbox and
    /// every other board in the shell uses.
    /// </summary>
    internal static string ResolveListBorderToken(MailMailbox mailbox, bool focused) => mailbox switch
    {
        MailMailbox.Workspace when focused => "mail.mailbox.workspace.border.focused",
        MailMailbox.Workspace => "mail.mailbox.workspace.border",
        _ when focused => "board.column.border.focused",
        _ => "board.column.border"
    };

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

    /// <summary>
    /// The list pane's header name: the read-state filter's name within
    /// <see cref="MailMailbox.Inbox"/>, where it applies, or the mailbox's
    /// own display name otherwise, since <see cref="MailListFilter"/> is
    /// inert outside the Inbox mailbox and showing it there would be
    /// misleading.
    /// </summary>
    private static string HeaderName(MailState state)
        => state.Mailbox == MailMailbox.Inbox ? FilterName(state.Filter) : state.Mailbox.DisplayName();

    private void RefreshBlocking()
    {
        _state.RefreshAsync(CancellationToken.None).GetAwaiter().GetResult();
        UnreadCount = _store.CountUnreadAsync(_state.Actor, CancellationToken.None).GetAwaiter().GetResult();
    }
}
