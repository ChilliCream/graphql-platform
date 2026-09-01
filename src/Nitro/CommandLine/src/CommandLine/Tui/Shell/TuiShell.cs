using System.Text;
using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Notify;
using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Tui.Board;
using ChilliCream.Nitro.CommandLine.Tui.Editing;
using ChilliCream.Nitro.CommandLine.Tui.Graph;
using ChilliCream.Nitro.CommandLine.Tui.Input;
using ChilliCream.Nitro.CommandLine.Tui.Runtime;
using ChilliCream.Nitro.CommandLine.Tui.Search;
using ChilliCream.Nitro.CommandLine.Tui.Theming;
using ChilliCream.Nitro.CommandLine.Tui.Tree;
using ChilliCream.Nitro.CommandLine.Tui.Widgets.Form;
using Spectre.Console;
using Spectre.Console.Rendering;
using EditingConfirmDialog = ChilliCream.Nitro.CommandLine.Tui.Editing.ConfirmDialog;

namespace ChilliCream.Nitro.CommandLine.Tui.Shell;

/// <summary>
/// The root handler and renderer for <see cref="TuiApplication"/>. Owns the mode
/// stack and composites the shell-level overlays (the quit confirmation, the task
/// editor, the close/reopen/delete confirmation, the status and priority quick
/// pickers, the task create form, and the toast row) over whichever mode is active.
/// </summary>
internal sealed class TuiShell
{
    private const string QuitConfirmMessage = "Quit? (y/n)";
    private const int StatusRowHeight = 1;
    private const int TabStripRowHeight = 1;
    private const string FooterSeparator = "  ";
    private const string FooterEllipsis = "…";
    private const string TabStripSeparator = " ";

    private static readonly TimeSpan s_quitGateDrainBound = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan s_quitGateGrace = TimeSpan.FromSeconds(1);

    private readonly IReadOnlyList<TuiTab> _tabs;
    private readonly TuiTab _tasksTab;
    private readonly Toaster _toaster = new();
    private readonly SearchMode? _searchMode;
    private readonly DependencyTreeView? _treeView;
    private readonly ITaskStore? _store;
    private readonly IMailStore? _mailStore;
    private readonly string? _actor;
    private readonly Dictionary<TuiTab, BoardDetailMode> _detailModes = [];

    private readonly Func<MailWakeDaemonState>? _mailWakeDaemonState;
    private readonly IReadOnlyList<TuiQuitGate> _quitGates;
    private readonly TimeSpan _quitGateDrainBound;

    private int _activeTabIndex;
    private bool _quitGateResolved;
    private ConfirmDialog? _confirmDialog;
    private TaskEditorForm? _editorForm;
    private EditingConfirmDialog? _lifecycleDialog;
    private TaskItem? _lifecycleTask;
    private TaskLifecycleAction _lifecycleAction;
    private QuickPicker? _picker;
    private TaskItem? _pickerTask;
    private PickerKind _pickerKind;
    private TaskCreateForm? _createForm;
    private EditingConfirmDialog? _discardDialog;
    private DiscardTarget _discardTarget;
    private int _width;
    private int _height;

    public TuiShell(
        KeyDispatcher dispatcher,
        ITuiMode activeMode,
        int initialWidth,
        int initialHeight,
        SearchMode? searchMode = null,
        DependencyTreeView? treeView = null,
        ITaskStore? store = null,
        string? actor = null,
        IMailStore? mailStore = null,
        Func<MailWakeDaemonState>? mailWakeDaemonState = null,
        IReadOnlyList<TuiQuitGate>? quitGates = null,
        TimeSpan? quitGateDrainBound = null)
        : this(
            [new TuiTab(
                string.Empty,
                mnemonic: '\0',
                activeMode ?? throw new ArgumentNullException(nameof(activeMode)),
                dispatcher ?? throw new ArgumentNullException(nameof(dispatcher)))],
            initialWidth,
            initialHeight,
            tasksTabIndex: 0,
            searchMode,
            treeView,
            store,
            actor,
            mailStore,
            mailWakeDaemonState,
            quitGates,
            quitGateDrainBound)
    {
    }

    /// <summary>
    /// Builds a tabbed shell hosting <paramref name="tabs"/> in order, starting on
    /// the first. Each tab keeps its own mode stack and key dispatcher, so switching
    /// tabs preserves nested mode state and routes keys only to the active tab.
    /// <paramref name="tasksTabIndex"/> identifies which tab owns the shell-level task
    /// overlay machinery (the task editor, the close/reopen/delete confirmation, the
    /// status and priority quick pickers, and the task create form): that machinery
    /// only activates while the tab at that index is active. A one-row tab strip
    /// renders above the content region whenever more than one tab is hosted.
    /// <paramref name="quitGates"/> runs before a confirmed normal quit is allowed to
    /// raise <see cref="QuitConfirmed"/>: each stops its own feature's new submissions
    /// and bounded-drains what is already in flight, and a nonzero pending or
    /// outcome-unknown count reported back turns the confirmation into a second
    /// prompt naming those counts rather than quitting immediately. Omitted, quitting
    /// behaves exactly as before.
    /// <paramref name="mailWakeDaemonState"/>, when given, is polled once per render
    /// and shown as a short badge in the footer's trailing segment (to the left of
    /// the actor identity), so the mail-wake daemon's Ready/Standby/Degraded/Stopping
    /// state stays visible while the dashboard is open, never blocking or altering
    /// any tab's own behavior.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="tasksTabIndex"/> is not a valid index into <paramref name="tabs"/>.
    /// </exception>
    public TuiShell(
        IReadOnlyList<TuiTab> tabs,
        int initialWidth,
        int initialHeight,
        int tasksTabIndex = 0,
        SearchMode? searchMode = null,
        DependencyTreeView? treeView = null,
        ITaskStore? store = null,
        string? actor = null,
        IMailStore? mailStore = null,
        Func<MailWakeDaemonState>? mailWakeDaemonState = null,
        IReadOnlyList<TuiQuitGate>? quitGates = null,
        TimeSpan? quitGateDrainBound = null)
    {
        ArgumentNullException.ThrowIfNull(tabs);

        if (tabs.Count == 0)
        {
            throw new ArgumentException("A shell needs at least one tab.", nameof(tabs));
        }

        if (tasksTabIndex < 0 || tasksTabIndex >= tabs.Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tasksTabIndex), tasksTabIndex, "Must be a valid index into tabs.");
        }

        _tabs = tabs;
        _activeTabIndex = 0;
        _tasksTab = tabs[tasksTabIndex];
        _searchMode = searchMode;
        _treeView = treeView;
        _store = store;
        _mailStore = mailStore;
        _actor = actor;
        _mailWakeDaemonState = mailWakeDaemonState;
        _quitGates = quitGates ?? [];
        _quitGateDrainBound = quitGateDrainBound ?? s_quitGateDrainBound;
        _width = initialWidth;
        _height = initialHeight;

        // Every tab's root mode initializes its own data on shell startup
        // (not only the active tab's), so an inactive tab's tab-strip title
        // (for example the mail tab's unread badge) is accurate before it is
        // ever switched to.
        foreach (var tab in _tabs)
        {
            tab.ActiveMode.OnEnter();
        }
    }

    /// <summary>
    /// Raised once a pending quit is confirmed. The caller is expected to stop the
    /// <see cref="TuiApplication"/> event loop in response.
    /// </summary>
    public event Action? QuitConfirmed;

    /// <summary>
    /// Raised when a second quit confirmation, shown because a registered
    /// <see cref="TuiQuitGate"/> reported unresolved work, is itself cancelled. A
    /// feature that registered a gate is expected to resume its own effect queue's
    /// submissions in response, since the TUI stays live.
    /// </summary>
    public event Action? QuitCancelled;

    private TuiTab ActiveTab => _tabs[_activeTabIndex];

    private ITuiMode ActiveMode => ActiveTab.ActiveMode;

    /// <summary>
    /// Whether the tab owning the shell-level task overlay machinery is
    /// currently active.
    /// </summary>
    private bool IsTasksTabActive => ReferenceEquals(ActiveTab, _tasksTab);

    private int TabStripHeight => _tabs.Count > 1 ? TabStripRowHeight : 0;

    private int ContentHeight => Math.Max(0, _height - StatusRowHeight - TabStripHeight);

    /// <summary>
    /// Handles one <see cref="TuiEvent"/>, returning whether the frame needs to be
    /// repainted. Matches the <see cref="TuiEventHandler"/> shape expected by
    /// <see cref="TuiApplication.RunAsync"/>.
    /// </summary>
    public bool Handle(TuiEvent tuiEvent) => tuiEvent switch
    {
        TuiEvent.KeyEvent keyEvent => HandleKey(keyEvent.Info),
        TuiEvent.ResizeEvent resize => HandleResize(resize.Width, resize.Height),
        TuiEvent.TickEvent tick => HandleTick(tick.Now),
        TuiEvent.DataChangedEvent => HandleDataChanged(),
        TuiEvent.EffectCompletedEvent => HandleEffectCompleted(),
        _ => false
    };

    /// <summary>
    /// Renders the current frame: whichever overlay is active, or the active mode,
    /// filling the content region, with a reserved bottom row for the current
    /// toast. Matches the <see cref="TuiFrameRenderer"/> shape expected by
    /// <see cref="TuiApplication.RunAsync"/>.
    /// </summary>
    public IRenderable Render()
    {
        var contentHeight = ContentHeight;

        IRenderable content = _confirmDialog is { } quitDialog
            ? quitDialog.Render(_width, contentHeight)
            : _discardDialog is { } discardDialog
                ? discardDialog.Render(_width, contentHeight)
                : _editorForm is { } form
                    ? form.Render(_width, contentHeight)
                    : _lifecycleDialog is { } lifecycleDialog
                        ? lifecycleDialog.Render(_width, contentHeight)
                        : _picker is { } picker
                            ? picker.Render(_width, contentHeight)
                            : _createForm is { } createForm
                                ? createForm.Render(_width, contentHeight)
                                : ActiveMode.Render(_width, contentHeight);

        var toastRow = _toaster.Render()
            ?? (IRenderable)new Markup(
                FormatFooter(
                    BuildFooterHints(),
                    _width,
                    _actor,
                    _mailWakeDaemonState?.Invoke(),
                    ActiveMode.FooterStatus));

        if (_tabs.Count <= 1)
        {
            return new Layout("root").SplitRows(
                new Layout("content", content),
                new Layout("status", toastRow).Size(StatusRowHeight));
        }

        return new Layout("root").SplitRows(
            new Layout("tabs", RenderTabStrip()).Size(TabStripRowHeight),
            new Layout("content", content),
            new Layout("status", toastRow).Size(StatusRowHeight));
    }

    /// <summary>
    /// Renders the one-row tab strip: every hosted tab's title with its
    /// <see cref="TuiTab.Mnemonic"/> bracketed (for example <c>[A]gents</c>)
    /// and styled in the footer's own key token, the active tab highlighted
    /// the same way a selected board row is, reusing the footer's own
    /// tokens rather than introducing new ones.
    /// </summary>
    private IRenderable RenderTabStrip()
    {
        var activeStyle = ThemeTokens.GetStyle("selection.highlight").ToMarkup();
        var inactiveStyle = ThemeTokens.GetStyle("footer.action").ToMarkup();
        var keyStyle = ThemeTokens.GetStyle("footer.key").ToMarkup();
        var parts = new string[_tabs.Count];

        for (var i = 0; i < _tabs.Count; i++)
        {
            var style = i == _activeTabIndex ? activeStyle : inactiveStyle;
            var titleMarkup = FormatMnemonicTitle(_tabs[i].Title, _tabs[i].Mnemonic, keyStyle);
            parts[i] = $"[{style}] {titleMarkup} [/]";
        }

        return new Markup(string.Join(TabStripSeparator, parts));
    }

    /// <summary>
    /// Formats <paramref name="title"/> with its <paramref name="mnemonic"/>
    /// letter bracketed and styled in <paramref name="keyStyle"/> (for
    /// example <c>[A]gents</c>), matched case-insensitively against
    /// <paramref name="title"/> so a title's natural capitalization (for
    /// example <c>Agents</c>) survives untouched. Falls back to the plain
    /// escaped title when <paramref name="mnemonic"/> does not occur in
    /// <paramref name="title"/> at all.
    /// </summary>
    private static string FormatMnemonicTitle(string title, char mnemonic, string keyStyle)
    {
        var index = title.IndexOf(mnemonic.ToString(), StringComparison.OrdinalIgnoreCase);

        if (index < 0)
        {
            return Markup.Escape(title);
        }

        var prefix = Markup.Escape(title[..index]);
        var letter = Markup.Escape(title[index].ToString());
        var suffix = Markup.Escape(title[(index + 1)..]);

        return $"{prefix}[{keyStyle}][[{letter}]][/]{suffix}";
    }

    private bool HandleResize(int width, int height)
    {
        _width = width;
        _height = height;
        ActiveMode.OnResize(width, ContentHeight);
        return true;
    }

    private bool HandleTick(DateTimeOffset now)
    {
        var toastDirty = _toaster.Tick(now);
        var searchDirty = _searchMode is { } search
            && ReferenceEquals(ActiveMode, search)
            && search.TickAsync(now, CancellationToken.None).GetAwaiter().GetResult();

        return toastDirty || searchDirty;
    }

    /// <summary>
    /// Refreshes every hosted tab's currently active mode, not only the
    /// active tab's, so a tab's data stays current while another tab is
    /// focused (for example the mail tab's unread badge while the tasks tab
    /// is active).
    /// </summary>
    private bool HandleDataChanged() => BroadcastToTabs(new TuiMessage.RefreshRequested());

    /// <summary>
    /// Drains every hosted tab's own effect queue, not only the active
    /// tab's, the same way <see cref="HandleDataChanged"/> refreshes every
    /// tab: a compose or reply completing on the mail tab must still show
    /// its toast while another tab is active. A mode that owns no effect
    /// queue ignores this message (see <see cref="TuiMessage.EffectCompleted"/>).
    /// </summary>
    private bool HandleEffectCompleted() => BroadcastToTabs(new TuiMessage.EffectCompleted());

    /// <summary>
    /// Sends <paramref name="message"/> to every hosted tab's currently
    /// active mode. The active tab's own follow-ups are routed through the
    /// shell's normal <see cref="HandleMessage"/> dispatch; an inactive
    /// tab's follow-ups are scoped to <see cref="TuiMessage.ShowToast"/>
    /// only, since a toast is shell-global on the toaster row regardless of
    /// which tab is active, while every other follow-up kind stays dropped:
    /// <see cref="HandleMessage"/> acts on shell-level state (dialogs,
    /// overlays) and the currently ACTIVE tab, so routing an inactive tab's
    /// non-toast follow-up through it would leak that tab's outcome onto
    /// whichever tab the user is actually looking at.
    /// </summary>
    private bool BroadcastToTabs(TuiMessage message)
    {
        foreach (var tab in _tabs)
        {
            var followUps = tab.ActiveMode.Handle(message);
            var isActiveTab = ReferenceEquals(tab, ActiveTab);

            foreach (var followUp in followUps)
            {
                if (isActiveTab || followUp is TuiMessage.ShowToast)
                {
                    HandleMessage(followUp);
                }
            }
        }

        return true;
    }

    private bool HandleKey(ConsoleKeyInfo info)
    {
        // The quit confirmation, the task editor, the close/reopen/delete
        // confirmation, and the task create form are modal: while one is
        // active it consumes every key itself, and unresolved keys are
        // swallowed rather than falling through to the active mode or the
        // global table.
        if (_confirmDialog is { } quitDialog)
        {
            var chord = KeyChord.From(info);
            return quitDialog.KeyMap.TryResolve(chord, out var quitMessage) && HandleMessage(quitMessage);
        }

        if (_discardDialog is not null)
        {
            return HandleDiscardDialogKey(info);
        }

        if (_editorForm is not null)
        {
            return HandleEditorFormKey(info);
        }

        if (_lifecycleDialog is not null)
        {
            return HandleLifecycleDialogKey(info);
        }

        if (_picker is not null)
        {
            return HandlePickerKey(info);
        }

        if (_createForm is not null)
        {
            return HandleCreateFormKey(info);
        }

        if (_searchMode is { } searchMode
            && ReferenceEquals(ActiveMode, searchMode)
            && searchMode.Focus == SearchFocus.Input
            && info.Key is not (ConsoleKey.Escape or ConsoleKey.Tab or ConsoleKey.Enter))
        {
            searchMode.HandleQueryKey(info, DateTimeOffset.UtcNow);
            return true;
        }

        // A mode that owns its own overlays (for example the mail mode's
        // archive confirmation, compose and reply forms, and their shared
        // discard confirmation) needs raw key input for its text fields
        // while one is active, the same way the search mode's query input
        // does above, rather than the semantic TuiMessage dispatch below.
        if (ActiveMode is IRawKeyCapturingMode { IsInputCapturing: true } capturingMode)
        {
            foreach (var followUp in capturingMode.HandleRawKey(info))
            {
                HandleMessage(followUp);
            }

            return true;
        }

        // Tab switching is checked ahead of the active tab's own dispatch,
        // so it stays inert whenever any of the overlays above are
        // capturing input, and never collides with either tab's key table.
        if (_tabs.Count > 1)
        {
            var tabChord = KeyChord.From(info);

            if (TabSwitchKeys.Resolve(tabChord) is { } delta)
            {
                return SwitchTab(delta);
            }

            if (TabSwitchKeys.ResolveMnemonic(tabChord, _tabs) is { } mnemonicIndex)
            {
                return SwitchToTab(mnemonicIndex);
            }
        }

        var message = ActiveTab.Dispatcher.Dispatch(info, ActiveMode.KeyMap);
        return message is not null && HandleMessage(message);
    }

    /// <summary>
    /// Switches the active tab by <paramref name="delta"/> positions,
    /// wrapping around at either end. Returns <see langword="false"/>
    /// without effect when only one tab is hosted.
    /// </summary>
    private bool SwitchTab(int delta)
    {
        if (_tabs.Count <= 1)
        {
            return false;
        }

        var next = ((_activeTabIndex + delta) % _tabs.Count + _tabs.Count) % _tabs.Count;
        return SwitchToTab(next);
    }

    /// <summary>
    /// Switches directly to the tab at <paramref name="index"/>, used by
    /// the mnemonic jump (Shift+&lt;letter&gt;) as well as <see cref="SwitchTab"/>'s
    /// delta-relative cycling. Returns <see langword="false"/> without
    /// effect when <paramref name="index"/> is already the active tab.
    /// </summary>
    private bool SwitchToTab(int index)
    {
        if (index == _activeTabIndex)
        {
            return false;
        }

        _activeTabIndex = index;
        ActiveTab.Activate(_width, ContentHeight);
        return true;
    }

    private bool HandleEditorFormKey(ConsoleKeyInfo info)
    {
        var result = _editorForm!.HandleKey(info);

        switch (result)
        {
            case null:
                return true;

            case FormResult.Cancelled:
            case FormResult.ButtonActivated { ButtonId: TaskEditorForm.CancelButtonId }:
                return TryDiscardEditorForm();

            case FormResult.Submitted submitted:
                return SubmitEditorForm(submitted);

            default:
                return true;
        }
    }

    private bool TryDiscardEditorForm()
    {
        if (_editorForm!.IsDirty)
        {
            _discardTarget = DiscardTarget.EditorForm;
            _discardDialog = CreateDiscardDialog();
            return true;
        }

        _editorForm = null;
        return true;
    }

    private bool SubmitEditorForm(FormResult.Submitted submitted)
    {
        var outcome = _editorForm!.SubmitAsync(_store!, submitted.Values, _actor!, CancellationToken.None)
            .GetAwaiter().GetResult();

        HandleMessage(outcome.ToShowToast());
        HandleMessage(new TuiMessage.RefreshRequested());

        // A failed save leaves the form open with its entered values so the
        // user can see the error and retry; only a successful save closes it.
        if (outcome is TaskEditorOutcome.Succeeded)
        {
            _editorForm = null;
        }

        return true;
    }

    private bool HandleLifecycleDialogKey(ConsoleKeyInfo info)
    {
        var result = _lifecycleDialog!.HandleKey(info);

        switch (result)
        {
            case null:
                return true;

            case ConfirmDialogResult.Cancelled:
                _lifecycleDialog = null;
                _lifecycleTask = null;
                return true;

            case ConfirmDialogResult.Confirmed confirmed:
                return SubmitLifecycleAction(confirmed.Reason);

            default:
                return true;
        }
    }

    private bool SubmitLifecycleAction(string reason)
    {
        var task = _lifecycleTask!;
        var action = _lifecycleAction;
        _lifecycleDialog = null;
        _lifecycleTask = null;

        var outcomeTask = action switch
        {
            TaskLifecycleAction.Close => TaskLifecycleActions.CloseAsync(_store!, task, reason, _actor!, CancellationToken.None),
            TaskLifecycleAction.Reopen => TaskLifecycleActions.ReopenAsync(_store!, task, reason, _actor!, CancellationToken.None),
            TaskLifecycleAction.Delete => TaskLifecycleActions.DeleteAsync(_store!, task, reason, _actor!, CancellationToken.None),
            _ => throw new NotSupportedException()
        };
        var outcome = outcomeTask.GetAwaiter().GetResult();

        HandleMessage(outcome.ToShowToast());
        HandleMessage(new TuiMessage.RefreshRequested());
        return true;
    }

    private bool HandleDiscardDialogKey(ConsoleKeyInfo info)
    {
        var result = _discardDialog!.HandleKey(info);

        switch (result)
        {
            case null:
                return true;

            case ConfirmDialogResult.Confirmed:
                _discardDialog = null;

                if (_discardTarget == DiscardTarget.EditorForm)
                {
                    _editorForm = null;
                }
                else
                {
                    _createForm = null;
                }

                return true;

            case ConfirmDialogResult.Cancelled:
                _discardDialog = null;
                return true;

            default:
                return true;
        }
    }

    /// <summary>
    /// Builds the confirmation dialog shown when Esc is pressed on a dirty
    /// task editor or create form: confirming discards the form's edits,
    /// cancelling returns to it with its values intact.
    /// </summary>
    private static EditingConfirmDialog CreateDiscardDialog()
        => new("Discard unsaved changes?", "Discard", ButtonKind.Danger);

    private bool HandlePickerKey(ConsoleKeyInfo info)
    {
        var result = _picker!.HandleKey(info);

        switch (result)
        {
            case null:
                return true;

            case QuickPickerResult.Cancelled:
                _picker = null;
                _pickerTask = null;
                return true;

            case QuickPickerResult.Applied applied:
                return SubmitPicker(applied.SelectedId);

            default:
                return true;
        }
    }

    private bool SubmitPicker(string selectedId)
    {
        var task = _pickerTask!;
        var kind = _pickerKind;
        _picker = null;
        _pickerTask = null;

        // Picking Closed on the status picker is not a bare status write: it
        // routes through the same close confirmation flow the x key uses.
        if (kind == PickerKind.Status && selectedId == TaskStates.Closed)
        {
            _lifecycleTask = task;
            _lifecycleAction = TaskLifecycleAction.Close;
            _lifecycleDialog = TaskLifecycleActions.CreateCloseDialog(task);
            return true;
        }

        var outcomeTask = kind switch
        {
            PickerKind.Status => StatusPicker.ApplyAsync(_store!, task, selectedId, _actor!, CancellationToken.None),
            PickerKind.Priority => PriorityPicker.ApplyAsync(
                _store!, task, int.Parse(selectedId, CultureInfo.InvariantCulture), _actor!, CancellationToken.None),
            _ => throw new NotSupportedException()
        };
        var outcome = outcomeTask.GetAwaiter().GetResult();

        HandleMessage(outcome.ToShowToast());
        HandleMessage(new TuiMessage.RefreshRequested());
        return true;
    }

    private bool HandleCreateFormKey(ConsoleKeyInfo info)
    {
        var result = _createForm!.HandleKey(info);

        switch (result)
        {
            case null:
                return true;

            case FormResult.Cancelled:
            case FormResult.ButtonActivated { ButtonId: TaskCreateForm.CancelButtonId }:
                return TryDiscardCreateForm();

            case FormResult.Submitted submitted:
                return SubmitCreateForm(submitted);

            default:
                return true;
        }
    }

    private bool TryDiscardCreateForm()
    {
        if (_createForm!.IsDirty)
        {
            _discardTarget = DiscardTarget.CreateForm;
            _discardDialog = CreateDiscardDialog();
            return true;
        }

        _createForm = null;
        return true;
    }

    private bool SubmitCreateForm(FormResult.Submitted submitted)
    {
        var outcome = _createForm!.SubmitAsync(_store!, submitted.Values, _actor!, CancellationToken.None)
            .GetAwaiter().GetResult();

        HandleMessage(outcome.ToShowToast());
        HandleMessage(new TuiMessage.RefreshRequested());

        // A failed save leaves the form open with its entered values so the
        // user can see the error and retry; only a successful save closes it.
        if (outcome is TaskCreateOutcome.Succeeded succeeded)
        {
            _createForm = null;
            ActiveMode.SelectTask(succeeded.TaskId);
        }

        return true;
    }

    private bool HandleMessage(TuiMessage message)
    {
        switch (message)
        {
            case TuiMessage.QuitRequested:
                _confirmDialog = new ConfirmDialog(QuitConfirmMessage);
                return true;

            case TuiMessage.ConfirmQuit:
                return HandleConfirmQuit();

            case TuiMessage.CancelQuit:
                _confirmDialog = null;

                if (_quitGateResolved)
                {
                    _quitGateResolved = false;
                    QuitCancelled?.Invoke();
                }

                return true;

            case TuiMessage.ShowToast showToast:
                _toaster.Enqueue(showToast.Text, showToast.Style, DateTimeOffset.UtcNow);
                return true;

            case TuiMessage.Back:
                PopMode();
                return true;

            case TuiMessage.OpenSelected when ActiveMode is BoardMode or GraphMode:
                return TryOpenDetail();

            case TuiMessage.FocusSearchRequested:
                if (ActiveMode is GraphMode graph)
                {
                    foreach (var followUp in graph.Handle(message))
                    {
                        HandleMessage(followUp);
                    }

                    return true;
                }

                if (!IsTasksTabActive || _searchMode is not { } search)
                {
                    return false;
                }

                SwitchTo(search);
                search.FocusInput();
                return true;

            case TuiMessage.OpenTreeRequested:
                return TryOpenTree();

            case TuiMessage.EditRequested:
                return TryOpenEditor();

            case TuiMessage.CloseOrReopenRequested:
                return TryOpenCloseOrReopenDialog();

            case TuiMessage.DeleteRequested:
                return TryOpenDeleteDialog();

            case TuiMessage.StatusPickerRequested:
                return TryOpenPicker(PickerKind.Status);

            case TuiMessage.PriorityPickerRequested:
                return TryOpenPicker(PickerKind.Priority);

            case TuiMessage.CreateTaskRequested:
                return TryOpenCreateForm(TaskTypes.Task);

            case TuiMessage.CreateEpicRequested:
                return TryOpenCreateForm(TaskTypes.Epic);

            default:
                foreach (var followUp in ActiveMode.Handle(message))
                {
                    HandleMessage(followUp);
                }

                return true;
        }
    }

    /// <summary>
    /// Runs the pre-cancellation quit gate: on the first confirmation, every
    /// registered <see cref="TuiQuitGate"/> stops its feature's new submissions and
    /// bounded-drains what is already in flight. With nothing left unresolved,
    /// <see cref="QuitConfirmed"/> fires immediately, exactly as it would with no
    /// gates registered. Otherwise a second confirmation naming the pending and
    /// outcome-unknown counts is shown, and only confirming that fires
    /// <see cref="QuitConfirmed"/>; gates are not re-run for that second confirmation.
    /// </summary>
    private bool HandleConfirmQuit()
    {
        _confirmDialog = null;

        if (_quitGateResolved || _quitGates.Count == 0)
        {
            _quitGateResolved = false;
            QuitConfirmed?.Invoke();
            return true;
        }

        var report = RunQuitGates();

        if (!report.HasUnresolvedWork)
        {
            QuitConfirmed?.Invoke();
            return true;
        }

        _quitGateResolved = true;
        _confirmDialog = new ConfirmDialog(FormatQuitGateMessage(report));
        return true;
    }

    private TuiQuitGateReport RunQuitGates()
    {
        var pendingCount = 0;
        var outcomeUnknownCount = 0;
        var operationIds = new List<TuiOperationId>();

        foreach (var gate in _quitGates)
        {
            TuiQuitGateReport report;

            try
            {
                report = gate(_quitGateDrainBound, CancellationToken.None)
                    .WaitAsync(_quitGateDrainBound + s_quitGateGrace)
                    .GetAwaiter().GetResult();
            }
            catch (TimeoutException)
            {
                // The gate itself failed to answer within its own bound plus a grace
                // period. This reports the gate's silence; it does not guess whether
                // the feature's commit boundary resolved.
                outcomeUnknownCount++;
                continue;
            }
            catch (Exception)
            {
                // A gate that faults, whether it throws synchronously or returns a
                // faulted task, is reported as outcome-unknown for that gate. The
                // runtime does not guess the feature's commit state and never lets
                // a gate fault tear down the TUI.
                outcomeUnknownCount++;
                continue;
            }

            pendingCount += report.PendingCount;
            outcomeUnknownCount += report.OutcomeUnknownCount;
            operationIds.AddRange(report.DiscoverableOperationIds);
        }

        return new TuiQuitGateReport(pendingCount, outcomeUnknownCount, operationIds);
    }

    private static string FormatQuitGateMessage(TuiQuitGateReport report) =>
        $"Exit with {report.PendingCount} stored-but-pending, {report.OutcomeUnknownCount} outcome-unknown? (y/n)";

    /// <summary>
    /// Opens the selected Board or Graph task in the active tab's detail mode.
    /// Back returns to the prior mode with its retained state.
    /// </summary>
    private bool TryOpenDetail()
    {
        if (_store is null)
        {
            return false;
        }

        if (ActiveMode.SelectedTaskId is not { } id)
        {
            return ShowToastNow("No task selected.", ToastStyle.Warn);
        }

        if (!_detailModes.TryGetValue(ActiveTab, out var detailMode))
        {
            detailMode = new BoardDetailMode(_store);
            _detailModes.Add(ActiveTab, detailMode);
        }

        detailMode.OpenOnTask(id);
        SwitchTo(detailMode);
        return true;
    }

    private bool TryOpenTree()
    {
        if (_treeView is not { } tree)
        {
            return false;
        }

        if (ActiveMode.SelectedTaskId is not { } id)
        {
            return ShowToastNow("No task selected.", ToastStyle.Warn);
        }

        tree.EnterOnTask(id);
        SwitchTo(tree);
        return true;
    }

    private bool TryOpenEditor()
    {
        if (!IsTasksTabActive || _store is null)
        {
            return false;
        }

        if (_actor is null)
        {
            return ShowToastNow(BoardIdentity.NoIdentityMessage, ToastStyle.Warn);
        }

        if (ActiveMode.SelectedTaskId is not { } id)
        {
            return ShowToastNow("No task selected.", ToastStyle.Warn);
        }

        var task = _store.GetTaskAsync(id, CancellationToken.None).GetAwaiter().GetResult();

        if (task is null)
        {
            return ShowToastNow($"Task '{id}' not found.", ToastStyle.Error);
        }

        var labels = _store.GetLabelsAsync(id, CancellationToken.None).GetAwaiter().GetResult();
        _editorForm = new TaskEditorForm(task, labels);
        return true;
    }

    private bool TryOpenCloseOrReopenDialog()
    {
        if (!IsTasksTabActive || _store is null)
        {
            return false;
        }

        if (_actor is null)
        {
            return ShowToastNow(BoardIdentity.NoIdentityMessage, ToastStyle.Warn);
        }

        if (LoadSelectedTask() is not { } task)
        {
            return true;
        }

        _lifecycleTask = task;

        if (TaskLifecycleActions.CanReopen(task))
        {
            _lifecycleAction = TaskLifecycleAction.Reopen;
            _lifecycleDialog = TaskLifecycleActions.CreateReopenDialog(task);
        }
        else
        {
            _lifecycleAction = TaskLifecycleAction.Close;
            _lifecycleDialog = TaskLifecycleActions.CreateCloseDialog(task);
        }

        return true;
    }

    private bool TryOpenDeleteDialog()
    {
        if (!IsTasksTabActive || _store is null)
        {
            return false;
        }

        if (_actor is null)
        {
            return ShowToastNow(BoardIdentity.NoIdentityMessage, ToastStyle.Warn);
        }

        if (LoadSelectedTask() is not { } task)
        {
            return true;
        }

        _lifecycleTask = task;
        _lifecycleAction = TaskLifecycleAction.Delete;
        _lifecycleDialog = TaskLifecycleActions.CreateDeleteDialog(task);
        return true;
    }

    private bool TryOpenPicker(PickerKind kind)
    {
        if (!IsTasksTabActive || _store is null)
        {
            return false;
        }

        if (_actor is null)
        {
            return ShowToastNow(BoardIdentity.NoIdentityMessage, ToastStyle.Warn);
        }

        if (LoadSelectedTask() is not { } task)
        {
            return true;
        }

        _pickerTask = task;
        _pickerKind = kind;
        _picker = kind == PickerKind.Status ? StatusPicker.Create(task) : PriorityPicker.Create(task);
        return true;
    }

    private bool TryOpenCreateForm(string typePreset)
    {
        if (!IsTasksTabActive || _store is null)
        {
            return false;
        }

        if (_actor is null)
        {
            return ShowToastNow(BoardIdentity.NoIdentityMessage, ToastStyle.Warn);
        }

        // A selected task becomes the new task's parent: creating unconditionally
        // requires no selection (unlike edit, lifecycle, and the pickers), so no
        // "no task selected" toast gates this on the active mode's selection.
        _createForm = new TaskCreateForm(typePreset, ActiveMode.SelectedTaskId);
        return true;
    }

    private TaskItem? LoadSelectedTask()
    {
        if (_store is null)
        {
            return null;
        }

        if (ActiveMode.SelectedTaskId is not { } id)
        {
            ShowToastNow("No task selected.", ToastStyle.Warn);
            return null;
        }

        var task = _store.GetTaskAsync(id, CancellationToken.None).GetAwaiter().GetResult();

        if (task is null)
        {
            ShowToastNow($"Task '{id}' not found.", ToastStyle.Error);
        }

        return task;
    }

    private bool ShowToastNow(string text, ToastStyle style)
    {
        _toaster.Enqueue(text, style, DateTimeOffset.UtcNow);
        return true;
    }

    private void SwitchTo(ITuiMode mode) => ActiveTab.SwitchTo(mode, _width, ContentHeight);

    /// <summary>
    /// Pops the active tab's own navigation stack back to its previous mode,
    /// so <see cref="TuiMessage.Back"/> never crosses tabs.
    /// </summary>
    private void PopMode()
    {
        if (ActiveTab.RootMode is GraphMode
            && _detailModes.TryGetValue(ActiveTab, out var detailMode)
            && ReferenceEquals(ActiveMode, detailMode))
        {
            ActiveTab.ResumeSuspendedMode(_width, ContentHeight);
            return;
        }

        ActiveTab.PopMode(_width, ContentHeight);
    }

    /// <summary>
    /// Builds the footer's hint list for whichever context currently owns
    /// key input, mirroring <see cref="HandleKey"/>'s own priority order so
    /// the footer can never show a hint the active input context would not
    /// actually honor. The fully modal overlays (the quit confirmation, the
    /// discard confirmation, the task editor, the lifecycle confirmation,
    /// the quick pickers, and the task create form) show only their own
    /// hints, since they consume every key themselves; the search mode's
    /// query input is treated the same way while it has focus, since
    /// <see cref="SearchMode.HandleQueryKey"/> swallows every key that is
    /// not one of its own bindings into the query rather than falling
    /// through to the global table. Every other context's hints are
    /// followed by the global table's, with quit last.
    /// </summary>
    private IReadOnlyList<KeyHint> BuildFooterHints()
    {
        if (_confirmDialog is not null)
        {
            return ConfirmDialog.Hints;
        }

        if (_discardDialog is not null)
        {
            return EditingConfirmDialog.Hints;
        }

        if (_editorForm is not null)
        {
            return TaskEditorForm.Hints;
        }

        if (_lifecycleDialog is not null)
        {
            return EditingConfirmDialog.Hints;
        }

        if (_picker is not null)
        {
            return QuickPicker.Hints;
        }

        if (_createForm is not null)
        {
            return TaskCreateForm.Hints;
        }

        var contextHints = ActiveMode.KeyMap?.Hints ?? [];

        if (_searchMode is { } search
            && ReferenceEquals(ActiveMode, search)
            && search.Focus == SearchFocus.Input)
        {
            return [SearchMode.TypingHint, .. contextHints, SearchMode.EnterHint];
        }

        // A mode capturing raw key input (see HandleKey) swallows every key
        // into its own overlay the same way the search query input does
        // above, so the footer must show only that overlay's own hints too.
        if (ActiveMode is IRawKeyCapturingMode { IsInputCapturing: true } capturingMode)
        {
            return capturingMode.CapturingHints;
        }

        // Without an identity every task write is refused, so the chord
        // must not be advertised, the same way a read-only mail mailbox
        // hides its own.
        var suppressed = _actor is null
            ? [.. ActiveMode.SuppressedGlobalHints, new KeyHint("e", "edit")]
            : ActiveMode.SuppressedGlobalHints;
        var hints = ActiveTab.Dispatcher.CombineHints(contextHints, suppressed);
        return _tabs.Count > 1 ? [.. hints, TabSwitchKeys.Hint] : hints;
    }

    /// <summary>
    /// Formats <paramref name="hints"/>, the mail-wake daemon badge (when
    /// <paramref name="mailWakeDaemonState"/> is given), and the current agent
    /// identity (when <paramref name="actor"/> is given) as the footer's single
    /// status-row line: the hints on the left as <see cref="FormatFooterHints"/>
    /// lays them out, the badge and identity right-aligned to the row's right
    /// edge, badge first, so they stand out from the key and action styles. This
    /// trailing segment never steals width the hints would otherwise use: it is
    /// fit into whatever room is left over after the hints are laid out. The
    /// badge is fixed-width and shown whole or not at all; the identity is
    /// truncated with a trailing ellipsis (or omitted entirely on widths too
    /// narrow for even that) rather than shrinking the hints further.
    /// </summary>
    private static string FormatFooter(
        IReadOnlyList<KeyHint> hints,
        int width,
        string? actor,
        MailWakeDaemonState? mailWakeDaemonState,
        string? inlineStatus)
    {
        if (width <= 0)
        {
            return string.Empty;
        }

        var statusText = string.IsNullOrEmpty(inlineStatus) ? null : TruncateFooterText(inlineStatus, width);
        var statusWidth = statusText is null ? 0 : GetFooterDisplayWidth(statusText);
        var hintWidth = Math.Max(0, width - statusWidth - (statusWidth > 0 ? FooterSeparator.Length : 0));
        var hintMarkup = FormatFooterHints(hints, hintWidth, out var hintPlainWidth);
        var leadingWidth = statusWidth + hintPlainWidth + (statusWidth > 0 && hintPlainWidth > 0 ? FooterSeparator.Length : 0);
        var leadingMarkup = statusWidth == 0
            ? hintMarkup
            : hintPlainWidth == 0
                ? $"[{ThemeTokens.GetStyle("footer.action").ToMarkup()}]{Markup.Escape(statusText!)}[/]"
                : $"[{ThemeTokens.GetStyle("footer.action").ToMarkup()}]{Markup.Escape(statusText!)}[/]"
                    + FooterSeparator
                    + hintMarkup;
        var available = width - leadingWidth - (leadingWidth > 0 ? FooterSeparator.Length : 0);

        if (available <= 0)
        {
            return leadingMarkup;
        }

        var trailingMarkup = string.Empty;
        var trailingPlainWidth = 0;

        if (mailWakeDaemonState is { } state)
        {
            var badgeText = FormatMailWakeDaemonBadge(state);

            if (GetFooterDisplayWidth(badgeText) <= available)
            {
                var badgeStyle = ThemeTokens.GetStyle(MailWakeDaemonStyleToken(state)).ToMarkup();
                trailingMarkup = $"[{badgeStyle}]{Markup.Escape(badgeText)}[/]";
                trailingPlainWidth = GetFooterDisplayWidth(badgeText);
                available -= trailingPlainWidth;
            }
        }

        if (!string.IsNullOrEmpty(actor))
        {
            var separatorNeeded = trailingPlainWidth > 0 ? FooterSeparator.Length : 0;
            var identityAvailable = available - separatorNeeded;

            string? identityText = null;

            if (GetFooterDisplayWidth(actor) <= identityAvailable)
            {
                identityText = actor;
            }
            else if (identityAvailable > GetFooterDisplayWidth(FooterEllipsis))
            {
                identityText = TruncateFooterText(actor, identityAvailable);
            }

            if (identityText is not null)
            {
                var identityStyle = ThemeTokens.GetStyle("footer.identity").ToMarkup();
                var identityMarkup = $"[{identityStyle}]{Markup.Escape(identityText)}[/]";

                trailingMarkup = trailingPlainWidth > 0
                    ? trailingMarkup + FooterSeparator + identityMarkup
                    : identityMarkup;
                trailingPlainWidth += separatorNeeded + GetFooterDisplayWidth(identityText);
            }
        }

        if (trailingPlainWidth == 0)
        {
            return leadingMarkup;
        }

        var padding = Math.Max(0, width - leadingWidth - trailingPlainWidth);

        return leadingMarkup + new string(' ', padding) + trailingMarkup;
    }

    private static string TruncateFooterText(string value, int width)
    {
        if (GetFooterDisplayWidth(value) <= width)
        {
            return value;
        }

        var ellipsisWidth = GetFooterDisplayWidth(FooterEllipsis);

        if (width <= ellipsisWidth)
        {
            return FooterEllipsis;
        }

        var builder = new StringBuilder(width);
        var enumerator = StringInfo.GetTextElementEnumerator(value);
        var remaining = width - ellipsisWidth;

        while (enumerator.MoveNext())
        {
            var element = enumerator.GetTextElement();

            var elementWidth = GetFooterDisplayWidth(element);

            if (elementWidth > remaining)
            {
                break;
            }

            builder.Append(element);
            remaining -= elementWidth;
        }

        return builder.Append('…').ToString();
    }

    private static int GetFooterDisplayWidth(string value)
    {
        var width = 0;
        var enumerator = StringInfo.GetTextElementEnumerator(value);

        while (enumerator.MoveNext())
        {
            width += GetFooterTextElementWidth(enumerator.GetTextElement());
        }

        return width;
    }

    private static int GetFooterTextElementWidth(string element)
        => Math.Max(0, element.GetCellWidth());

    /// <summary>
    /// The mail-wake daemon coordinator's current state as a short, fixed-width
    /// footer badge label.
    /// </summary>
    private static string FormatMailWakeDaemonBadge(MailWakeDaemonState state) => state switch
    {
        MailWakeDaemonState.Ready => "mail-wake:ready",
        MailWakeDaemonState.Standby => "mail-wake:standby",
        MailWakeDaemonState.Degraded => "mail-wake:degraded",
        MailWakeDaemonState.Stopping => "mail-wake:stopping",
        _ => "mail-wake:standby"
    };

    /// <summary>
    /// The theme token styling <see cref="FormatMailWakeDaemonBadge"/>'s label
    /// for <paramref name="state"/>.
    /// </summary>
    private static string MailWakeDaemonStyleToken(MailWakeDaemonState state) => state switch
    {
        MailWakeDaemonState.Ready => "footer.daemon.ready",
        MailWakeDaemonState.Degraded => "footer.daemon.degraded",
        _ => "footer.daemon.standby"
    };

    /// <summary>
    /// Formats <paramref name="hints"/> as dimmed key labels and
    /// normal-weight action labels, separated hint entries, truncated with a
    /// trailing ellipsis once <paramref name="width"/> cannot fit every
    /// hint. <paramref name="plainWidth"/> reports the unmarked-up column
    /// width of the returned markup, so callers can lay out further content
    /// (for example the footer identity) in whatever room is left.
    /// </summary>
    private static string FormatFooterHints(IReadOnlyList<KeyHint> hints, int width, out int plainWidth)
    {
        if (width <= 0 || hints.Count == 0)
        {
            plainWidth = 0;
            return string.Empty;
        }

        var keyStyle = ThemeTokens.GetStyle("footer.key").ToMarkup();
        var actionStyle = ThemeTokens.GetStyle("footer.action").ToMarkup();

        var plainItems = new string[hints.Count];
        var markupItems = new string[hints.Count];

        for (var i = 0; i < hints.Count; i++)
        {
            plainItems[i] = $"{hints[i].Key} {hints[i].Action}";
            markupItems[i] =
                $"[{keyStyle}]{Markup.Escape(hints[i].Key)}[/] [{actionStyle}]{Markup.Escape(hints[i].Action)}[/]";
        }

        var separatorWidth = GetFooterDisplayWidth(FooterSeparator);
        var fullPlainWidth = plainItems.Sum(GetFooterDisplayWidth) + separatorWidth * (hints.Count - 1);

        if (fullPlainWidth <= width)
        {
            plainWidth = fullPlainWidth;
            return string.Join(FooterSeparator, markupItems);
        }

        var included = 0;
        var usedWidth = 0;
        var trailerWidth = separatorWidth + GetFooterDisplayWidth(FooterEllipsis);

        for (var i = 0; i < hints.Count; i++)
        {
            var itemWidth = (i == 0 ? 0 : separatorWidth) + GetFooterDisplayWidth(plainItems[i]);

            if (usedWidth + itemWidth + trailerWidth > width)
            {
                break;
            }

            usedWidth += itemWidth;
            included++;
        }

        if (included == 0)
        {
            var ellipsisWidth = GetFooterDisplayWidth(FooterEllipsis);
            plainWidth = width >= ellipsisWidth ? ellipsisWidth : 0;
            return width >= ellipsisWidth ? FooterEllipsis : string.Empty;
        }

        plainWidth = usedWidth + trailerWidth;
        return string.Join(FooterSeparator, markupItems.Take(included)) + FooterSeparator + FooterEllipsis;
    }
}
