using System.Collections.Concurrent;
using System.Threading.Channels;
using ChilliCream.Nitro.CommandLine.Commands.Agent.Mail;
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
/// The mail board <see cref="ITuiMode"/>: a message list pane next to a
/// detail pane for the selected message, with a key toggling the detail
/// pane between one message and its whole thread. Opening a message in the
/// detail pane marks it read for the actor; the u, a, r, and c gestures
/// toggle read/unread, archive (behind a confirmation), reply, and compose,
/// every write going through the same store operations the CLI uses. The
/// Shift+I/S/L/W gestures jump directly to the Inbox/Sent/All/Workspace
/// <see cref="MailMailbox"/>; u and a are refused with a toast, instead of
/// reaching the store, on any message the actor is not a recipient of.
/// A board with no actor is read-only in every mailbox, refusing each of
/// those gestures and every jump to a personal mailbox with
/// <see cref="Shell.BoardIdentity.NoIdentityMessage"/>.
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
/// compose and reply forms, their shared discard confirmation, and the
/// Workspace agent filter picker) rather than routing through
/// <see cref="TuiShell"/>'s task-specific overlay fields. The agent filter
/// picker (p) narrows <see cref="MailMailbox.Workspace"/> to messages one
/// agent sent or received, in either direction; it is refused with a toast
/// outside Workspace, the same way the mutating gestures are refused inside
/// it, since the filter has no meaning for any other mailbox. The list pane
/// itself renders as a table (<see cref="MailTable"/>): a heading row above
/// thread rollup rows by default (<see cref="MailListMode.Threads"/>,
/// <see cref="MailKeyMap"/>'s Shift+V toggling to the pre-epic flat
/// per-message rows and back), with za/zo/zc/zR/zM folding a thread's
/// messages into indented rows beneath it (see <see cref="OpenFoldPrefix"/>).
/// <see cref="MailState"/> defaults to <see cref="MailMailbox.Workspace"/>,
/// per the epic's user ruling, so this mode opens read-only until the
/// actor jumps elsewhere.
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
    /// pane takes the remainder. Widened from the pre-epic 2/5 split so the
    /// table's From/To/Subject/Preview/Age columns (see <see cref="MailTable"/>)
    /// have room to breathe at the epic's target of a typical 200+ column
    /// terminal, per the epic's layout-rethink ruling.
    /// </summary>
    private const int ListWidthNumerator = 3;
    private const int ListWidthDenominator = 5;

    /// <summary>
    /// The <see cref="Editing.QuickPickerOption.Id"/> for the agent filter
    /// picker's "all agents" entry, which clears <see cref="MailState.AgentFilter"/>
    /// rather than naming an agent.
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
    /// <see cref="TuiEffectCompletion{TResult}.Cancelled"/> completion: both
    /// mean the send effect itself never reached a <see cref="MailSendOutcome"/>
    /// at all. Every path once <see cref="_store"/>'s write returns
    /// successfully is shielded into a definite outcome (see
    /// <see cref="ReconcileWakeAsync"/>), so this almost always means the
    /// write was never attempted or never returned; but only an observed
    /// <see cref="ExitException"/> (see <see cref="MailSendOutcome.Failed"/>)
    /// actually proves a rejected write, so this text states the commit's
    /// outcome as unknown rather than asserting the message was not stored.
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
    private readonly IAgentRegistry _agentRegistry;
    private readonly IActorWakeDispatcher _wakeDispatcher;
    private readonly IMailWakeReceiptObserver _wakeObserver;
    private readonly MailState _state;
    private readonly MailDetailView _detailView = new();
    private readonly TimeProvider _timeProvider;
    private readonly CancellationToken _effectCancellationToken;
    private readonly Viewport _listViewport = new(0, 0);

    /// <summary>
    /// Runs a compose or reply submission's store-plus-wake workflow off the
    /// input/render thread; see <see cref="SubmitCompose"/>, <see cref="SubmitReply"/>,
    /// and <see cref="CreateQuitGate"/>. At most one submission is in flight
    /// at a time (see <see cref="SendDedupeKey"/>).
    /// </summary>
    private readonly TuiEffectQueue<MailSendOutcome> _sendEffects = new();

    /// <summary>
    /// One <see cref="MailSendOutcome.Stored"/> notice per submitted send,
    /// posted by <see cref="RunSendEffectAsync"/>/<see cref="RunReplyEffectAsync"/>
    /// right after the store commit and before <see cref="ReconcileWakeAsync"/>
    /// begins, alongside a <see cref="TuiEffectQueue{TResult}.SignalWake"/>
    /// call on <see cref="_sendEffects"/> so this side channel's own wake
    /// reaches the event loop through the same path a terminal completion
    /// does, rather than depending on some unrelated event (a key press, a
    /// tick, or a database watcher notification) to surface it. This side
    /// channel is what lets <see cref="DrainEffectQueue"/> surface a
    /// truthful "Stored" toast ahead of the terminal outcome, which can take
    /// up to <see cref="WakeDispatchPolicy.BatchDeadline"/> longer to
    /// resolve.
    /// </summary>
    private readonly ConcurrentQueue<MailSendOutcome.Stored> _storedNotices = new();

    /// <summary>
    /// Completions <see cref="CreateQuitGate"/> drained during its own
    /// bounded wait, held here so <see cref="DrainEffectQueue"/> still
    /// turns them into toasts on this mode's next <see cref="Handle"/> call:
    /// a cancelled second quit confirmation resumes the live TUI, and a
    /// completion the gate already observed must not go unreported just
    /// because it was drained outside the normal <see cref="Handle"/> path.
    /// </summary>
    private readonly List<TuiEffectCompletion<MailSendOutcome>> _deferredCompletions = [];

    /// <summary>
    /// Every registered agent's <see cref="AgentRecord.Client"/>, keyed by
    /// name (case-insensitively, matching <see cref="MailRecipientView"/>'s
    /// comparer), loaded once per <see cref="RefreshBlocking"/> rather than
    /// once per rendered row so the detail pane's client attribution never
    /// issues a registry query per line.
    /// </summary>
    private IReadOnlyDictionary<string, string> _clientsByName =
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
    /// <param name="agentRegistry">Resolves every registered agent's client attribution.</param>
    /// <param name="wakeDispatcher">
    /// Dispatches or defers wake work for a compose or reply's recipients,
    /// run as part of <see cref="MailWakeDispatch.RunAsync"/> off the input
    /// thread. The unified dashboard passes
    /// <see cref="DaemonOwnedActorWakeDispatcher"/> because its
    /// <see cref="IMailWakeDaemonCoordinator"/> already owns dispatch for the
    /// whole session, so this mode only observes what the daemon settles.
    /// </param>
    /// <param name="wakeObserver">Reads back a submission's durable wake outcome.</param>
    /// <param name="timeProvider">Defaults to <see cref="TimeProvider.System"/>.</param>
    /// <param name="effectCancellationToken">
    /// Cancels a pending send's post-commit wake dispatch/observe step (see
    /// <see cref="ReconcileWakeAsync"/>); the store write itself is always
    /// shielded from it. Defaults to <see cref="CancellationToken.None"/>
    /// for a caller with no shutdown signal to plumb through.
    /// </param>
    public MailMode(
        IMailStore store,
        string? actor,
        IAgentRegistry agentRegistry,
        IActorWakeDispatcher wakeDispatcher,
        IMailWakeReceiptObserver wakeObserver,
        TimeProvider? timeProvider = null,
        CancellationToken effectCancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(agentRegistry);
        ArgumentNullException.ThrowIfNull(wakeDispatcher);
        ArgumentNullException.ThrowIfNull(wakeObserver);

        _store = store;
        _agentRegistry = agentRegistry;
        _wakeDispatcher = wakeDispatcher;
        _wakeObserver = wakeObserver;
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
    /// The actor for a write, which every mutating gesture refuses without
    /// (see <see cref="RefuseIfReadOnly"/>).
    /// </summary>
    private string WritingActor
        => _state.Actor ?? throw new InvalidOperationException("The board has no agent identity.");

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
    /// How many messages addressed to the actor are unread and not
    /// archived, as of the last refresh. Drives a hosting tab's unread
    /// badge.
    /// </summary>
    public int UnreadCount { get; private set; }

    /// <inheritdoc />
    public KeyMap? KeyMap => null;

    /// <inheritdoc />
    /// <remarks>
    /// Hides the u, a, r, and c hints (see <see cref="MailKeyMap.ToggleReadHint"/>,
    /// <see cref="MailKeyMap.ArchiveHint"/>, <see cref="MailKeyMap.ReplyHint"/>,
    /// and <see cref="MailKeyMap.ComposeHint"/>) from the footer while
    /// <see cref="MailLifecycleActions.IsReadOnly"/> is true for
    /// <see cref="MailState.Mailbox"/>: the same four gestures
    /// <see cref="RefuseIfReadOnly"/> refuses with a toast, rather than
    /// reaching the store, in <see cref="MailMailbox.Workspace"/>.
    /// </remarks>
    public IReadOnlyCollection<KeyHint> SuppressedGlobalHints
        => MailLifecycleActions.IsReadOnly(_state.Mailbox, _state.Actor) ? s_workspaceReadOnlyHints : [];

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
    /// <remarks>
    /// Drains <see cref="_sendEffects"/> ahead of dispatching
    /// <paramref name="message"/> itself, so any compose or reply completion
    /// persisted since the last call surfaces as a toast the moment any
    /// message reaches this mode - most reliably <see cref="TuiMessage.RefreshRequested"/>,
    /// which the workspace database watcher raises on the same write this
    /// mode's own send effect just made (see <see cref="DrainEffectQueue"/>).
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
        // The drain at the top of Handle already turned any completion into
        // its toast before HandleCore ever runs, so this message itself
        // needs no further action here.
        TuiMessage.EffectCompleted => [],
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
    /// a toast. Inert when <see cref="MailLifecycleActions.IsReadOnly"/>, so
    /// opening a message in Workspace never writes.
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
    {
        if (_state.Actor is null)
        {
            return [new TuiMessage.ShowToast(Shell.BoardIdentity.NoIdentityMessage, ToastStyle.Warn)];
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
    /// Snapshots the compose form's validated values on this, the input
    /// thread, then submits the whole store-plus-wake workflow to
    /// <see cref="_sendEffects"/> so it runs off-thread. Duplicate submits
    /// (a second send already in flight under <see cref="SendDedupeKey"/>)
    /// are refused with a toast, leaving the form open with its values
    /// intact rather than losing them; a successful submit closes the form
    /// immediately but retains it in <see cref="_submittedComposeForm"/> so
    /// <see cref="DrainEffectQueue"/> can reopen it, values intact, if the
    /// store ends up rejecting the write.
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
    /// The reply form's counterpart to <see cref="SubmitCompose"/>: same
    /// snapshot-then-submit-off-thread shape, sharing <see cref="SendDedupeKey"/>
    /// with compose so at most one send of either kind is ever in flight,
    /// and the same retain-until-resolved handling of the closed form
    /// through <see cref="_submittedReplyForm"/>.
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
    /// The compose send effect body: writes the message, then hands off to
    /// <see cref="ReconcileWakeAsync"/>. The store write itself uses
    /// <see cref="CancellationToken.None"/>, never <paramref name="cancellationToken"/>:
    /// a submitted send must always either fully commit or fully roll back
    /// on its own terms, never be left ambiguous by an unrelated shutdown
    /// signal arriving mid-write (see the type remarks on
    /// <see cref="MailSendOutcome"/>). Only <see cref="ExitException"/> means
    /// the store rejected the write outright; every other exception (a
    /// genuine bug) is left to propagate and reach the effect queue as
    /// <see cref="TuiEffectCompletion{TResult}.Faulted"/>, which
    /// <see cref="DrainEffectQueue"/> also reports as unsent.
    /// </summary>
    private async Task<MailSendOutcome> RunSendEffectAsync(
        MailMessageCreation creation, CancellationToken cancellationToken)
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
        return await ReconcileWakeAsync(message, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// The reply form's counterpart to <see cref="RunSendEffectAsync"/>.
    /// </summary>
    private async Task<MailSendOutcome> RunReplyEffectAsync(
        MailReplyRequest request, CancellationToken cancellationToken)
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
        return await ReconcileWakeAsync(message, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Runs the actor-wake dispatch-and-observe step for an already-committed
    /// <paramref name="message"/>: <see cref="_wakeDispatcher"/> and
    /// <see cref="_wakeObserver"/> either dispatch wake work or observe what
    /// the running daemon settles (see the constructor's remarks on
    /// <c>wakeDispatcher</c>). <paramref name="message"/> is
    /// already known and durable by the time this runs, so any failure here,
    /// cancellation included, can never mean the message itself was not
    /// sent - only that its wake outcome could not be reconciled this time,
    /// reported as <see cref="MailSendOutcome.Reconciled"/> rather than
    /// letting the exception reach the effect queue as a
    /// <see cref="TuiEffectCompletion{TResult}.Faulted"/> or
    /// <see cref="TuiEffectCompletion{TResult}.Cancelled"/> completion, which
    /// would obscure that the store write itself already succeeded.
    /// </summary>
    private async Task<MailSendOutcome> ReconcileWakeAsync(MailMessage message, CancellationToken cancellationToken)
    {
        try
        {
            var notification = await MailWakeDispatch.RunAsync(
                    message, _wakeDispatcher, _wakeObserver, _timeProvider, cancellationToken)
                .ConfigureAwait(false);

            return new MailSendOutcome.Succeeded(message, notification);
        }
        catch (Exception)
        {
            return new MailSendOutcome.Reconciled(message);
        }
    }

    /// <summary>
    /// Drains every <see cref="_storedNotices"/> notice and compose/reply
    /// completion persisted to <see cref="_sendEffects"/> since the last
    /// call, turning each into its toast: a <see cref="MailSendOutcome.Stored"/>
    /// notice's own <see cref="MailSendOutcome.ToShowToast"/> surfaces the
    /// truthful "Stored" state ahead of the terminal outcome, which
    /// <see cref="MailSendOutcome.ToShowToast"/> also formats for a completed
    /// effect; <see cref="SendOutcomeUnknownToastText"/> covers the
    /// <see cref="TuiEffectCompletion{TResult}.Faulted"/> or
    /// <see cref="TuiEffectCompletion{TResult}.Cancelled"/> cases a submitted
    /// effect can, in principle, still reach before its store write is even
    /// attempted. When this same drain also observes the terminal
    /// completion (the common case once the wake step resolves quickly), the
    /// stored notice is dropped rather than turned into its own toast: the
    /// shell's toaster shows one toast at a time for a fixed duration, so
    /// queuing "Stored" immediately ahead of the terminal toast would only
    /// delay the terminal toast by that duration for no benefit, when both
    /// are already known at once. A stored notice or committed completion
    /// also refreshes this mode's loaded data, so the new message appears
    /// without waiting for the next database-watcher tick. A
    /// <see cref="MailSendOutcome.Failed"/> outcome reopens whichever of
    /// <see cref="_submittedComposeForm"/> or <see cref="_submittedReplyForm"/>
    /// the rejected submit retained, so the typed values survive behind the
    /// error toast; every other outcome discards them.
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
    /// Discards every <see cref="MailSendOutcome.Stored"/> notice posted to
    /// <see cref="_storedNotices"/> since the last call, without turning any
    /// of them into a toast; see <see cref="CreateQuitGate"/>.
    /// </summary>
    private void ClearStoredNotices()
    {
        while (_storedNotices.TryDequeue(out _))
        {
        }
    }

    /// <summary>
    /// Builds the <see cref="TuiQuitGate"/> a hosting command registers with
    /// <see cref="TuiShell"/>: stops accepting new sends, bounded-drains
    /// whatever is already in flight, and reports what remained. A
    /// completion the drain observes is classified truthfully rather than
    /// counted as resolved: a <see cref="MailSendOutcome.Succeeded"/> whose
    /// notification is still <see cref="MailWakeTargetStatus.Pending"/> adds
    /// to <see cref="TuiQuitGateReport.PendingCount"/>, and a
    /// <see cref="MailSendOutcome.Reconciled"/> outcome or a
    /// <see cref="TuiEffectCompletion{TResult}.Faulted"/>/<see cref="TuiEffectCompletion{TResult}.Cancelled"/>
    /// completion adds to <see cref="TuiQuitGateReport.OutcomeUnknownCount"/>.
    /// Every completion the drain observes, counted or not, is stashed into
    /// <see cref="_deferredCompletions"/> so its toast still surfaces on this
    /// mode's next <see cref="Handle"/> call rather than being silently
    /// dropped: a normal quit that proceeds never calls <see cref="Handle"/>
    /// again, but a cancelled second confirmation (see
    /// <see cref="ResumeSendAcceptance"/>) returns to a live TUI that must
    /// still report it. A cancelled second confirmation must call
    /// <see cref="ResumeSendAcceptance"/>, per <see cref="TuiQuitGate"/>'s
    /// own contract. Also discards whatever <see cref="_storedNotices"/> the
    /// drained submission posted: this report already classifies it more
    /// precisely by its terminal completion, so surfacing the stale
    /// intermediate "Stored" toast too on a later <see cref="Handle"/> call
    /// would only be redundant with, and could arrive out of order before,
    /// that terminal toast.
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
                case TuiEffectCompletion<MailSendOutcome>.Completed
                {
                    Result: MailSendOutcome.Succeeded { Notification.Status: MailWakeTargetStatus.Pending }
                }:
                    pendingCount++;
                    operationIds.Add(completion.OperationId);
                    break;

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
    /// Reverses <see cref="CreateQuitGate"/>'s <c>StopAccepting</c> after a
    /// cancelled second quit confirmation, per <see cref="TuiShell.QuitCancelled"/>'s
    /// own contract.
    /// </summary>
    public void ResumeSendAcceptance() => _sendEffects.ResumeAccepting();

    /// <summary>
    /// A final, unconditional, bounded chance for a send still in flight to
    /// land before the host process exits: unlike <see cref="CreateQuitGate"/>,
    /// this never runs interactively and never blocks a normal quit, so it
    /// is the only shield a Ctrl+C or host-cancelled exit gets. A submitted
    /// send's own store write is itself un-cancellable (see <see cref="RunSendEffectAsync"/>),
    /// so this bound only needs to cover that commit landing, not the
    /// (separately bounded, and safely abandonable) wake dispatch that
    /// follows it.
    /// </summary>
    public Task ShieldPendingSendsAsync(TimeSpan bound, CancellationToken cancellationToken)
        => _sendEffects.DrainPendingAsync(bound, cancellationToken);

    /// <summary>
    /// Relays <see cref="_sendEffects"/>'s own wake event onto the TUI event
    /// loop's channel; matches <see cref="TuiEventSource"/>, so a hosting
    /// command merges this into <see cref="TuiApplication.RunAsync"/>
    /// alongside its workspace database watcher. Without this registered,
    /// a compose or reply completion still persists into
    /// <see cref="_sendEffects"/> and is never lost, but its toast surfaces
    /// only once some other event (a key press, a tick, or a database
    /// watcher notification) happens to reach this mode's <see cref="Handle"/>.
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
            return [new TuiMessage.ShowToast(Shell.BoardIdentity.NoIdentityMessage, ToastStyle.Warn)];
        }

        _state.SelectMailboxAsync(mailbox, CancellationToken.None).GetAwaiter().GetResult();
        _detailView.ResetScroll();
        return [];
    }

    /// <summary>
    /// Opens the agent filter picker, sourced from <see cref="IAgentRegistry.ListAsync"/>
    /// with an "All agents" entry prepended to clear the filter, pre-selected
    /// on <see cref="MailState.AgentFilter"/>. Refused with a toast, rather
    /// than reaching the registry, when <see cref="MailState.Mailbox"/> is
    /// not <see cref="MailMailbox.Workspace"/>: the filter has no effect
    /// anywhere else.
    /// </summary>
    private IReadOnlyList<TuiMessage> OpenAgentFilterPicker()
    {
        if (_state.Mailbox != MailMailbox.Workspace)
        {
            return [new TuiMessage.ShowToast(AgentFilterRequiresWorkspaceMessage, ToastStyle.Warn)];
        }

        var agents = _agentRegistry.ListAsync(role: null, staleBefore: null, CancellationToken.None)
            .GetAwaiter().GetResult();

        _agentPicker = BuildAgentPicker(agents, _state.AgentFilter);
        return [];
    }

    private static QuickPicker BuildAgentPicker(IReadOnlyList<AgentRecord> agents, string? selectedAgent)
    {
        var options = new List<QuickPickerOption> { new(AllAgentsOptionId, "All agents") };
        options.AddRange(agents.Select(a => new QuickPickerOption(a.Name, FormatAgentOptionMarkup(a))));

        return new QuickPicker("Filter by agent", options, selectedAgent ?? AllAgentsOptionId);
    }

    /// <summary>
    /// An agent picker row's markup: the name, plus its
    /// <see cref="AgentRecord.Client"/> in dim parentheses when non-empty,
    /// the same "nothing shown for empty" rule <see cref="MailDetailView"/>
    /// applies to its own client attribution.
    /// </summary>
    private static string FormatAgentOptionMarkup(AgentRecord agent)
    {
        var name = Markup.Escape(agent.Name);
        return agent.Client.Length == 0 ? name : $"{name} [dim]({Markup.Escape(agent.Client)})[/]";
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
    /// Toggles <see cref="MailState.ListMode"/> between
    /// <see cref="MailListMode.Threads"/> and <see cref="MailListMode.Flat"/>
    /// (Shift+V). Unlike every other mailbox/filter switch, this never
    /// reaches the store: <see cref="MailState.ToggleListMode"/> rebuilds
    /// <see cref="MailState.Rows"/> from data already loaded.
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
    /// <see cref="HandleFoldPrefixKey"/> instead of the normal semantic
    /// dispatch, the same capturing mechanism the archive/compose/reply
    /// overlays use.
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
    /// thread and need no selection. Any other key, including Escape, cancels
    /// the prefix with no action - mirroring vim's own za/zo/zc/zR/zM, there
    /// is no error toast for an unrecognized second key.
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
    /// Builds the list pane's panel directly, rather than through
    /// <see cref="ColumnPane"/>, so <see cref="ResolveListBorderToken"/> can
    /// give <see cref="MailMailbox.Workspace"/> a distinct border accent.
    /// Spectre paints the panel header text with <c>BorderStyle</c>, so the
    /// header picks up that accent too without any separate styling. Every
    /// other mailbox resolves to exactly the border tokens
    /// <see cref="ColumnPane"/> itself uses.
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
        => _detailView.Render(_state, width, height, _state.Focus == MailFocus.Detail, _clientsByName);

    /// <summary>
    /// Renders the list's visible rows: a fixed heading row (see
    /// <see cref="MailTable.RenderHeading"/>) followed by the scrolled
    /// thread/message rows, padded with blank lines so the panel reports a
    /// stable line count, with "N more above/below" indicators reserving
    /// their own rows once the rows no longer fit <paramref name="interiorHeight"/>.
    /// Column widths (<see cref="MailTable.ComputeColumns"/>) are computed
    /// once per render from <paramref name="contentWidth"/> so the heading
    /// and every row line up.
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
    /// Renders one <see cref="MailListRow"/> via <see cref="MailTable"/>,
    /// resolving the unread-to-me highlight per row type: a thread row asks
    /// <see cref="MailState.IsThreadUnreadToMe"/> (which knows how to read
    /// Workspace's unscoped rollups safely); a message row asks
    /// <see cref="MailRecipientView.IsUnread"/> directly, correct in every
    /// mailbox since a message's own embedded recipients carry the actor's
    /// real read state wherever it is queried from.
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
    /// <see cref="MailMailbox.Inbox"/>, in both <see cref="MailListMode.Flat"/>
    /// and <see cref="MailListMode.Threads"/> - so cycling f/F (<see cref="MailState.CycleFilterAsync"/>)
    /// is always visible, including for <see cref="MailListFilter.Archived"/>
    /// in Threads mode, which now has the same row-level effect there as in
    /// Flat (see <see cref="MailState.Filter"/>); the mailbox's own display
    /// name suffixed with the selected agent within <see cref="MailMailbox.Workspace"/>
    /// when <see cref="MailState.AgentFilter"/> is set, the third of
    /// Workspace's mode indicators alongside <see cref="HeaderName"/>'s own
    /// text and <see cref="ResolveListBorderToken"/>'s border accent (see the
    /// class doc); or the plain mailbox display name otherwise.
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

        var agents = _agentRegistry.ListAsync(role: null, staleBefore: null, CancellationToken.None)
            .GetAwaiter().GetResult();

        // ToLookup + first-wins rather than ToDictionary: an externally
        // written registry could hold case-variant duplicate names (the
        // OrdinalIgnoreCase comparer only affects lookup, not uniqueness),
        // and ToDictionary throws on a duplicate key where ToLookup does not.
        _clientsByName = agents
            .ToLookup(a => a.Name, a => a.Client, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
    }
}
