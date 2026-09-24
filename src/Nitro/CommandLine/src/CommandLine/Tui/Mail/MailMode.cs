using System.Collections.Concurrent;
using System.Threading.Channels;
using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Notify;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tui.Editing;
using ChilliCream.Nitro.CommandLine.Tui.Input;
using ChilliCream.Nitro.CommandLine.Tui.Runtime;
using ChilliCream.Nitro.CommandLine.Tui.Shell;
using ChilliCream.Nitro.CommandLine.Tui.Theming;
using ChilliCream.Nitro.CommandLine.Tui.Widgets;
using ChilliCream.Nitro.CommandLine.Tui.Widgets.Form;
using Spectre.Console.Rendering;
using ConfirmDialog = ChilliCream.Nitro.CommandLine.Tui.Editing.ConfirmDialog;
using CursorDirection = ChilliCream.Nitro.CommandLine.Tui.Input.CursorDirection;

namespace ChilliCream.Nitro.CommandLine.Tui.Mail;

/// <summary>
/// Displays mailboxes as thread or message lists with a detail pane.
/// Supports composing, replying, and recipient-state changes when the board has
/// an acting agent and the selected mailbox is writable.
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
    /// The maximum number of passes used to reserve viewport indicator rows.
    /// </summary>
    private const int MaxIndicatorSettlePasses = 3;

    /// <summary>
    /// The fraction of the frame width the list pane occupies; the detail
    /// pane takes the remainder.
    /// </summary>
    private const int ListWidthNumerator = 3;
    private const int ListWidthDenominator = 5;

    /// <summary>
    /// The <see cref="QuickPickerOption.Id"/> for the agent filter picker's "all agents" entry,
    /// which clears <see cref="MailState.AgentFilter"/> rather than naming an agent.
    /// </summary>
    private const string AllAgentsOptionId = "";

    /// <summary>
    /// The toast shown when the agent filter picker is requested outside
    /// <see cref="MailMailbox.Workspace"/>, where the filter has no effect.
    /// </summary>
    private const string AgentFilterRequiresWorkspaceMessage =
        "Agent filter only applies to Workspace. Press Shift+W for Workspace.";

    /// <summary>
    /// The toast shown when a fold gesture (z-prefix) is requested while
    /// <see cref="MailState.ListMode"/> is <see cref="MailListMode.Flat"/>,
    /// where there is nothing to fold.
    /// </summary>
    private const string FoldRequiresThreadsMessage =
        "Fold only applies to threaded view. Press Shift+V for threaded view.";

    /// <summary>
    /// The <see cref="TuiEffectQueue{TResult}.TrySubmit"/> dedupe key every
    /// compose and reply submission shares: only one send may be in flight
    /// at a time, regardless of which form it came from.
    /// </summary>
    private const string SendDedupeKey = "mail.send";

    private const string SendingToastText = "Sending…";
    private const string SendInFlightToastText = "A message is already sending. Try again shortly.";

    /// <summary>
    /// Shown for a <see cref="TuiEffectCompletion{TResult}.Faulted"/> or
    /// <see cref="TuiEffectCompletion{TResult}.Cancelled"/> completion.
    /// States the commit's outcome as unknown rather than asserting the
    /// message was not stored.
    /// </summary>
    private const string SendOutcomeUnknownToastText = "Sending did not complete. The message's outcome is unknown.";

    /// <summary>
    /// The <see cref="CapturingHints"/> shown while the fold prefix (z) is
    /// pending its second key.
    /// </summary>
    private static readonly KeyHint[] s_foldPrefixHints =
    [
        new("a/o/c", "toggle/open/close thread"),
        new("R/M", "unfold/fold all")
    ];

    /// <summary>
    /// The footer hints <see cref="SuppressedGlobalHints"/> hides while
    /// <see cref="MailMailbox.Workspace"/> is active.
    /// </summary>
    private static readonly KeyHint[] s_workspaceReadOnlyHints =
    [
        MailKeyMap.ToggleReadHint,
        MailKeyMap.ArchiveHint,
        MailKeyMap.ReplyHint,
        MailKeyMap.ComposeHint
    ];

    private readonly IMailStore _store;
    private readonly IAgentStore _agentStore;
    private readonly MailState _state;
    private readonly MailDetailView _detailView = new();
    private readonly TimeProvider _timeProvider;
    private readonly CancellationToken _effectCancellationToken;
    private readonly Viewport _listViewport = new(0, 0);

    /// <summary>
    /// Runs asynchronous compose and reply writes with wake intent for
    /// <see cref="IMailWakeDaemonCoordinator"/>. At most one send is in flight at a time.
    /// </summary>
    private readonly TuiEffectQueue<MailSendOutcome> _sendEffects = new();

    /// <summary>
    /// Store-commit notices awaiting display before the final send outcome is drained.
    /// </summary>
    private readonly ConcurrentQueue<MailSendOutcome.Stored> _storedNotices = new();

    /// <summary>
    /// Completions retained by the quit gate for processing on the next
    /// <see cref="Handle"/> call.
    /// </summary>
    private readonly List<TuiEffectCompletion<MailSendOutcome>> _deferredCompletions = [];

    /// <summary>
    /// Every agent's harness display name, keyed by agent name (case-insensitively),
    /// loaded once per <see cref="RefreshBlocking"/>.
    /// </summary>
    private IReadOnlyDictionary<string, string> _harnessesByName =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    private ConfirmDialog? _archiveDialog;
    private MailMessage? _archiveTarget;
    private MailComposeForm? _composeForm;
    private MailReplyForm? _replyForm;
    private MailComposeForm? _submittedComposeForm;
    private MailReplyForm? _submittedReplyForm;
    private ConfirmDialog? _discardDialog;
    private MailDiscardTarget _discardTarget;
    private QuickPicker? _agentPicker;
    private bool _foldPrefixPending;

    /// <summary>
    /// Builds a mail board over <paramref name="store"/> for <paramref name="actor"/>.
    /// </summary>
    /// <param name="store">The mail store every read and write goes through.</param>
    /// <param name="actor">
    /// The acting agent, or null when the board has no identity: only
    /// <see cref="MailMailbox.Workspace"/> is then reachable, and every
    /// write is refused.
    /// </param>
    /// <param name="agentStore">Resolves every agent's harness attribution.</param>
    /// <param name="timeProvider">Defaults to <see cref="TimeProvider.System"/>.</param>
    /// <param name="effectCancellationToken">
    /// Passed to submitted effects; the current send and reply effects do not use it
    /// to cancel their store writes.
    /// </param>
    public MailMode(
        IMailStore store,
        string? actor,
        IAgentStore agentStore,
        TimeProvider? timeProvider = null,
        CancellationToken effectCancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(agentStore);

        _store = store;
        _agentStore = agentStore;
        _state = new MailState(actor, new MailDataLoader(store));
        _timeProvider = timeProvider ?? TimeProvider.System;
        _effectCancellationToken = effectCancellationToken;
    }

    /// <summary>
    /// The board's current live state: messages, filter, selection, and
    /// focus.
    /// </summary>
    public MailState State => _state;

    /// <summary>
    /// The actor used to render read state: an empty name without an
    /// identity, which matches no recipient, so nothing renders as
    /// addressed to me.
    /// </summary>
    private string RenderActor => _state.Actor ?? string.Empty;

    /// <summary>
    /// The acting agent for writes; throws when no identity is available.
    /// </summary>
    private string WritingActor
        => _state.Actor ?? throw new InvalidOperationException("The board has no agent identity.");

    /// <summary>
    /// Whether an overlay or pending fold prefix consumes raw key input.
    /// </summary>
    public bool IsInputCapturing
        => _archiveDialog is not null
        || _composeForm is not null
        || _replyForm is not null
        || _discardDialog is not null
        || _agentPicker is not null
        || _foldPrefixPending;

    /// <inheritdoc />
    public IReadOnlyList<KeyHint> CapturingHints
        => _discardDialog is not null ? ConfirmDialog.Hints
        : _archiveDialog is not null ? ConfirmDialog.Hints
        : _composeForm is not null ? MailComposeForm.Hints
        : _replyForm is not null ? MailReplyForm.Hints
        : _agentPicker is not null ? QuickPicker.Hints
        : _foldPrefixPending ? s_foldPrefixHints
        : [];

    /// <summary>
    /// The number of unread, unarchived messages addressed to the actor at the last
    /// refresh, or zero without an acting agent.
    /// </summary>
    public int UnreadCount { get; private set; }

    /// <inheritdoc />
    public KeyMap? KeyMap => null;

    /// <inheritdoc />
    /// <remarks>
    /// Hides the u, a, r, and c hints from the footer while
    /// <see cref="MailLifecycleActions.IsReadOnly"/> is true for <see cref="MailState.Mailbox"/>.
    /// </remarks>
    public IReadOnlyCollection<KeyHint> SuppressedGlobalHints
        => MailLifecycleActions.IsReadOnly(_state.Mailbox, _state.Actor) ? s_workspaceReadOnlyHints : [];

    /// <inheritdoc />
    public void OnEnter() => RefreshBlocking();

    /// <inheritdoc />
    public void OnResize(int width, int height)
    {
        // Layout is recomputed during rendering.
    }

    /// <inheritdoc />
    /// <remarks>
    /// Drains send outcomes before handling the message.
    /// </remarks>
    public IReadOnlyList<TuiMessage> Handle(TuiMessage message)
    {
        var completions = DrainEffectQueue();
        var handled = HandleCore(message);
        return completions.Count == 0 ? handled : [.. completions, .. handled];
    }

    private IReadOnlyList<TuiMessage> HandleCore(TuiMessage message) => message switch
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
        TuiMessage.AgentFilterPickerRequested => OpenAgentFilterPicker(),
        TuiMessage.ToggleListModeRequested => ToggleListMode(),
        TuiMessage.FoldPrefixRequested => OpenFoldPrefix(),
        // Already turned into a toast by the drain in Handle.
        TuiMessage.EffectCompleted => [],
        _ => []
    };

    /// <summary>
    /// Handles one raw key while <see cref="IsInputCapturing"/> is true, routed
    /// here by the host instead of through the semantic <see cref="TuiMessage"/> dispatch.
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

        if (_agentPicker is not null)
        {
            return HandleAgentPickerKey(info);
        }

        if (_foldPrefixPending)
        {
            return HandleFoldPrefixKey(info);
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

        if (_agentPicker is { } agentPicker)
        {
            return agentPicker.Render(width, height);
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
            if (_state.Rows.Count > 0)
            {
                _state.SelectedRow = Math.Clamp(_state.SelectedRow + delta, 0, _state.Rows.Count - 1);
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
            if (_state.Rows.Count > 0)
            {
                _state.SelectedRow = edge == EdgeTarget.Top ? 0 : _state.Rows.Count - 1;
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
    /// Toggles focus between the list and detail panes.
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
    /// Marks the selected unread message read when focusing its detail pane, unless
    /// the mailbox is read-only. Returns a toast only when the write fails.
    /// </summary>
    private IReadOnlyList<TuiMessage> MaybeMarkSelectedRead()
    {
        if (MailLifecycleActions.IsReadOnly(_state.Mailbox, _state.Actor))
        {
            return [];
        }

        if (_state.SelectedMessage is not { } message || !MailRecipientView.IsUnread(message, WritingActor))
        {
            return [];
        }

        var outcome = MailLifecycleActions.MarkReadAsync(_store, message, WritingActor, CancellationToken.None)
            .GetAwaiter().GetResult();

        RefreshBlocking();

        return outcome is MailActionOutcome.Failed ? [outcome.ToShowToast()] : [];
    }

    /// <summary>
    /// Toggles the selected message between read and unread for the actor.
    /// Refused with a toast, rather than reaching the store, when
    /// <see cref="MailLifecycleActions.IsReadOnly"/> or when the actor has
    /// no recipient row on the message.
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

        if (MailRecipientView.FindRecipient(message, WritingActor) is null)
        {
            return [new TuiMessage.ShowToast(NotARecipientMessage(message), ToastStyle.Warn)];
        }

        var outcome = MailLifecycleActions.ToggleReadAsync(_store, message, WritingActor, CancellationToken.None)
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

        if (MailRecipientView.FindRecipient(message, WritingActor) is null)
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
    /// Opens the compose form, or returns a refusal toast when the mailbox is read-only.
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
    /// every other mutating gesture when <see cref="MailLifecycleActions.IsReadOnly"/>.
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
    /// Returns a refusal toast when the current mailbox is read-only, or null when
    /// a write may proceed.
    /// </summary>
    private IReadOnlyList<TuiMessage>? RefuseIfReadOnly()
    {
        if (_state.Actor is null)
        {
            return [new TuiMessage.ShowToast(BoardIdentity.NoIdentityMessage, ToastStyle.Warn)];
        }

        return MailLifecycleActions.IsReadOnly(_state.Mailbox, _state.Actor)
            ? [new TuiMessage.ShowToast(MailLifecycleActions.WorkspaceReadOnlyMessage, ToastStyle.Warn)]
            : null;
    }

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

        var outcome = MailLifecycleActions.ArchiveAsync(_store, target, WritingActor, CancellationToken.None)
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

    /// <summary>
    /// Submits the compose values for an asynchronous store write, retaining the form
    /// until the outcome is known. A refused submission leaves the form open.
    /// </summary>
    private IReadOnlyList<TuiMessage> SubmitCompose(FormResult.Submitted submitted)
    {
        var creation = MailComposeForm.BuildCreation(submitted.Values, WritingActor);

        if (!_sendEffects.TrySubmit(
            SendDedupeKey, (_, ct) => RunSendEffectAsync(creation, ct), _effectCancellationToken, out _))
        {
            return [new TuiMessage.ShowToast(SendInFlightToastText, ToastStyle.Warn)];
        }

        _submittedComposeForm = _composeForm;
        _composeForm = null;
        return [new TuiMessage.ShowToast(SendingToastText, ToastStyle.Info)];
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

    /// <summary>
    /// Submits the reply values for an asynchronous store write using the shared send
    /// submission slot, retaining the form until the outcome is known.
    /// </summary>
    private IReadOnlyList<TuiMessage> SubmitReply(FormResult.Submitted submitted)
    {
        var request = _replyForm!.BuildRequest(submitted.Values, WritingActor);

        if (!_sendEffects.TrySubmit(
            SendDedupeKey, (_, ct) => RunReplyEffectAsync(request, ct), _effectCancellationToken, out _))
        {
            return [new TuiMessage.ShowToast(SendInFlightToastText, ToastStyle.Warn)];
        }

        _submittedReplyForm = _replyForm;
        _replyForm = null;
        return [new TuiMessage.ShowToast(SendingToastText, ToastStyle.Info)];
    }

    /// <summary>
    /// Stores the message and enqueues wake intent without cancellation, then posts a
    /// stored notice and returns success. Converts <see cref="ExitException"/> to a
    /// failed outcome; other exceptions propagate to the effect queue.
    /// </summary>
    private async Task<MailSendOutcome> RunSendEffectAsync(
        MailMessageCreation creation, CancellationToken _)
    {
        MailMessage message;

        try
        {
            message = await _store.SendMessageAsync(creation, CancellationToken.None).ConfigureAwait(false);
        }
        catch (ExitException ex)
        {
            return new MailSendOutcome.Failed(ex.Message);
        }

        _storedNotices.Enqueue(new MailSendOutcome.Stored(message));
        _sendEffects.SignalWake();
        return new MailSendOutcome.Succeeded(message);
    }

    /// <summary>
    /// The reply form's counterpart to <see cref="RunSendEffectAsync"/>.
    /// </summary>
    private async Task<MailSendOutcome> RunReplyEffectAsync(
        MailReplyRequest request, CancellationToken _)
    {
        MailMessage message;

        try
        {
            message = await _store.ReplyMessageAsync(
                    request.InReplyToId, request.Actor, request.Body, MailWakePolicy.Enqueue, CancellationToken.None)
                .ConfigureAwait(false);
        }
        catch (ExitException ex)
        {
            return new MailSendOutcome.Failed(ex.Message);
        }

        _storedNotices.Enqueue(new MailSendOutcome.Stored(message));
        _sendEffects.SignalWake();
        return new MailSendOutcome.Succeeded(message);
    }

    /// <summary>
    /// Processes queued store notices and send completions, refreshing mail after a
    /// confirmed write. A rejected write restores the submitted form unless another
    /// form of that kind is already open.
    /// </summary>
    private IReadOnlyList<TuiMessage> DrainEffectQueue()
    {
        var storedNotices = DrainStoredNotices();
        var drained = _sendEffects.DrainCompletions();
        List<TuiEffectCompletion<MailSendOutcome>> completions;

        if (_deferredCompletions.Count == 0)
        {
            completions = [.. drained];
        }
        else
        {
            completions = [.. _deferredCompletions, .. drained];
            _deferredCompletions.Clear();
        }

        if (storedNotices.Count == 0 && completions.Count == 0)
        {
            return [];
        }

        var toasts = new List<TuiMessage>(storedNotices.Count + completions.Count);
        var committed = storedNotices.Count > 0;

        if (completions.Count == 0)
        {
            foreach (var notice in storedNotices)
            {
                toasts.Add(notice.ToShowToast());
            }
        }

        foreach (var completion in completions)
        {
            switch (completion)
            {
                case TuiEffectCompletion<MailSendOutcome>.Completed completed:
                    toasts.Add(completed.Result.ToShowToast());
                    committed |= completed.Result is MailSendOutcome.Succeeded or MailSendOutcome.Reconciled;

                    if (completed.Result is MailSendOutcome.Failed)
                    {
                        _composeForm ??= _submittedComposeForm;
                        _replyForm ??= _submittedReplyForm;
                    }

                    break;

                default:
                    toasts.Add(new TuiMessage.ShowToast(SendOutcomeUnknownToastText, ToastStyle.Error));
                    break;
            }

            _submittedComposeForm = null;
            _submittedReplyForm = null;
        }

        if (committed)
        {
            RefreshBlocking();
        }

        return toasts;
    }

    /// <summary>
    /// Drains every <see cref="MailSendOutcome.Stored"/> notice posted to
    /// <see cref="_storedNotices"/> since the last call.
    /// </summary>
    private IReadOnlyList<MailSendOutcome.Stored> DrainStoredNotices()
    {
        if (_storedNotices.IsEmpty)
        {
            return [];
        }

        var drained = new List<MailSendOutcome.Stored>();

        while (_storedNotices.TryDequeue(out var notice))
        {
            drained.Add(notice);
        }

        return drained;
    }

    /// <summary>
    /// Discards all queued store notices without producing toasts.
    /// </summary>
    private void ClearStoredNotices()
    {
        while (_storedNotices.TryDequeue(out _))
        {
        }
    }

    /// <summary>
    /// Creates a quit gate that stops submissions, waits within the supplied bound,
    /// and reports pending effects and unknown outcomes. Drained completions remain
    /// available to <see cref="Handle"/>; a cancelled quit must resume send acceptance.
    /// </summary>
    public TuiQuitGate CreateQuitGate() => async (bound, cancellationToken) =>
    {
        _sendEffects.StopAccepting();
        await _sendEffects.DrainPendingAsync(bound, cancellationToken).ConfigureAwait(false);

        ClearStoredNotices();
        var completions = _sendEffects.DrainCompletions();
        _deferredCompletions.AddRange(completions);

        var pendingCount = _sendEffects.PendingCount;
        var outcomeUnknownCount = 0;
        var operationIds = new List<TuiOperationId>(_sendEffects.PendingOperationIds);

        foreach (var completion in completions)
        {
            switch (completion)
            {
                case TuiEffectCompletion<MailSendOutcome>.Completed { Result: MailSendOutcome.Reconciled }:
                case TuiEffectCompletion<MailSendOutcome>.Faulted:
                case TuiEffectCompletion<MailSendOutcome>.Cancelled:
                    outcomeUnknownCount++;
                    operationIds.Add(completion.OperationId);
                    break;
            }
        }

        return new TuiQuitGateReport(pendingCount, outcomeUnknownCount, operationIds);
    };

    /// <summary>
    /// Resumes send acceptance after a cancelled quit confirmation.
    /// </summary>
    public void ResumeSendAcceptance() => _sendEffects.ResumeAccepting();

    /// <summary>
    /// Waits within the supplied bound for pending sends to finish without cancelling
    /// the sends or requesting user confirmation.
    /// </summary>
    public Task ShieldPendingSendsAsync(TimeSpan bound, CancellationToken cancellationToken)
        => _sendEffects.DrainPendingAsync(bound, cancellationToken);

    /// <summary>
    /// Relays send-effect wake signals to the TUI event channel until cancellation.
    /// </summary>
    public Task RunSendEffectEventsAsync(ChannelWriter<TuiEvent> writer, CancellationToken cancellationToken)
        => _sendEffects.RunAsync(writer, cancellationToken);

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
        if (_state.Actor is null && mailbox != MailMailbox.Workspace)
        {
            return [new TuiMessage.ShowToast(BoardIdentity.NoIdentityMessage, ToastStyle.Warn)];
        }

        _state.SelectMailboxAsync(mailbox, CancellationToken.None).GetAwaiter().GetResult();
        _detailView.ResetScroll();
        return [];
    }

    /// <summary>
    /// Opens the Workspace agent filter picker with the current filter selected and
    /// an entry to clear it. Returns a refusal toast outside the Workspace mailbox.
    /// </summary>
    private IReadOnlyList<TuiMessage> OpenAgentFilterPicker()
    {
        if (_state.Mailbox != MailMailbox.Workspace)
        {
            return [new TuiMessage.ShowToast(AgentFilterRequiresWorkspaceMessage, ToastStyle.Warn)];
        }

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

    /// <summary>
    /// Toggles between thread and flat lists using the loaded list data.
    /// Selecting a thread in the rebuilt list may load its messages.
    /// </summary>
    private IReadOnlyList<TuiMessage> ToggleListMode()
    {
        _state.ToggleListMode();
        _detailView.ResetScroll();
        return [];
    }

    /// <summary>
    /// Enters the fold-prefix (z) capture state, refused with a toast when
    /// <see cref="MailState.ListMode"/> is <see cref="MailListMode.Flat"/>,
    /// where there is nothing to fold. The next raw key is routed to
    /// <see cref="HandleFoldPrefixKey"/> instead of the normal semantic dispatch.
    /// </summary>
    private IReadOnlyList<TuiMessage> OpenFoldPrefix()
    {
        if (_state.ListMode != MailListMode.Threads)
        {
            return [new TuiMessage.ShowToast(FoldRequiresThreadsMessage, ToastStyle.Warn)];
        }

        _foldPrefixPending = true;
        return [];
    }

    /// <summary>
    /// Resolves the key following a fold prefix: a (toggle), o (open/expand),
    /// c (close/collapse) act on the thread under the cursor (see
    /// <see cref="CurrentRowThreadId"/>); Shift+R/Shift+M unfold/fold every
    /// thread and need no selection. Any other key, including Escape,
    /// cancels the prefix with no action.
    /// </summary>
    private IReadOnlyList<TuiMessage> HandleFoldPrefixKey(ConsoleKeyInfo info)
    {
        _foldPrefixPending = false;

        switch (info.KeyChar)
        {
            case 'a':
                if (CurrentRowThreadId() is { } toggleId)
                {
                    _state.ToggleThreadFold(toggleId);
                }

                break;

            case 'o':
                if (CurrentRowThreadId() is { } openId)
                {
                    _state.ExpandThread(openId);
                }

                break;

            case 'c':
                if (CurrentRowThreadId() is { } closeId)
                {
                    _state.CollapseThread(closeId);
                }

                break;

            case 'R':
                _state.ExpandAllThreads();
                break;

            case 'M':
                _state.CollapseAllThreads();
                break;
        }

        return [];
    }

    /// <summary>
    /// The thread id the fold prefix's second key acts on: the selected
    /// row's own thread id for a thread row, or its parent thread's id for
    /// an expanded child message row; null when nothing is selected.
    /// </summary>
    private string? CurrentRowThreadId()
    {
        if (_state.SelectedRow < 0 || _state.SelectedRow >= _state.Rows.Count)
        {
            return null;
        }

        return _state.Rows[_state.SelectedRow] switch
        {
            MailListRow.Thread t => t.Summary.ThreadId,
            MailListRow.MessageRow m => m.Message.ThreadId,
            _ => null
        };
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
        var count = _state.ListMode == MailListMode.Threads ? _state.Threads.Count : _state.Messages.Count;
        var panel = BuildListPanel(HeaderName(_state), count, lines, focused);
        panel.Width = safeWidth;
        panel.Height = Math.Max(1, height);

        return panel;
    }

    /// <summary>
    /// Renders the list panel with a distinct border accent for the Workspace mailbox.
    /// </summary>
    private Panel BuildListPanel(string name, int count, IReadOnlyList<string> lines, bool focused)
    {
        IRenderable content = lines.Count == 0
            ? new Markup(string.Empty)
            : new Rows(lines.Select(line => (IRenderable)new Markup(line)));

        var borderToken = ResolveListBorderToken(_state.Mailbox, focused);
        var headerText = Markup.Escape($"{name} ({count})");

        return new Panel(content)
        {
            Header = new PanelHeader(headerText),
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
        => _detailView.Render(_state, width, height, _state.Focus == MailFocus.Detail, _harnessesByName);

    /// <summary>
    /// Renders the heading and visible list rows with aligned columns, scroll
    /// indicators, and blank padding.
    /// </summary>
    private IReadOnlyList<string> RenderListLines(
        int contentWidth, int interiorHeight, bool focused, DateTimeOffset now)
    {
        if (interiorHeight <= 0)
        {
            return [];
        }

        var columns = MailTable.ComputeColumns(contentWidth, _state.ListMode == MailListMode.Threads);
        var heading = MailTable.RenderHeading(columns);

        if (interiorHeight == 1)
        {
            return [heading];
        }

        var bodyHeight = interiorHeight - 1;
        var rows = _state.Rows;
        var reservedRows = 0;

        for (var pass = 0; pass < MaxIndicatorSettlePasses; pass++)
        {
            var windowHeight = Math.Max(0, bodyHeight - reservedRows);
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
        var lines = new List<string>(interiorHeight) { heading };

        if (_listViewport.HiddenAbove > 0)
        {
            lines.Add(FormatIndicator(_listViewport.HiddenAbove, "above"));
        }

        for (var i = 0; i < visibleCount; i++)
        {
            var index = start + i;
            var selected = focused && index == _state.SelectedRow;
            lines.Add(RenderRow(rows[index], selected, now, columns));
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

    /// <summary>
    /// Renders a list row with the acting agent's unread highlight, resolved from the
    /// thread summary or the message's recipient state.
    /// </summary>
    private string RenderRow(MailListRow row, bool selected, DateTimeOffset now, MailTable.Columns columns) => row switch
    {
        MailListRow.Thread t => MailTable.RenderThreadRow(
            t.Summary, t.Expanded, _state.IsThreadUnreadToMe(t.Summary), selected, RenderActor, now, columns),
        MailListRow.MessageRow m => MailTable.RenderMessageRow(
            m.Message, m.ThreadChild, MailRecipientView.IsUnread(m.Message, RenderActor), selected, RenderActor, now, columns),
        _ => string.Empty
    };

    private static string FormatIndicator(int hiddenCount, string direction) => $"  {hiddenCount} more {direction}";

    private static string FilterName(MailListFilter filter) => filter switch
    {
        MailListFilter.Unread => "Unread",
        MailListFilter.Archived => "Archived",
        _ => "Inbox"
    };

    /// <summary>
    /// The list pane's header name: the read-state filter's name within
    /// <see cref="MailMailbox.Inbox"/>; the mailbox's own display name
    /// suffixed with the selected agent within <see cref="MailMailbox.Workspace"/>
    /// when <see cref="MailState.AgentFilter"/> is set; or the plain mailbox
    /// display name otherwise.
    /// </summary>
    private static string HeaderName(MailState state)
    {
        if (state.Mailbox == MailMailbox.Inbox)
        {
            return FilterName(state.Filter);
        }

        return state.Mailbox == MailMailbox.Workspace && state.AgentFilter is { } agent
            ? $"{state.Mailbox.DisplayName()}: {agent}"
            : state.Mailbox.DisplayName();
    }

    private void RefreshBlocking()
    {
        _state.RefreshAsync(CancellationToken.None).GetAwaiter().GetResult();
        UnreadCount = _state.Actor is null
            ? 0
            : _store.CountUnreadAsync(_state.Actor, CancellationToken.None).GetAwaiter().GetResult();

        var agents = _agentStore.ListAsync(CancellationToken.None).GetAwaiter().GetResult();

        // First-wins on duplicate names (case-insensitive).
        _harnessesByName = agents
            .ToLookup(
                a => a.Name,
                a => a.Harness is { Length: > 0 } h ? AgentHarnessDisplay.Name(h) : string.Empty,
                StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
    }
}
