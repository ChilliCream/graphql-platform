using System.Globalization;
using ChilliCream.Nitro.CommandLine.Services.Notify;
using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Tui.Board;
using ChilliCream.Nitro.CommandLine.Tui.Editing;
using ChilliCream.Nitro.CommandLine.Tui.Input;
using ChilliCream.Nitro.CommandLine.Tui.Runtime;
using ChilliCream.Nitro.CommandLine.Tui.Search;
using ChilliCream.Nitro.CommandLine.Tui.Theming;
using ChilliCream.Nitro.CommandLine.Tui.Tree;
using ChilliCream.Nitro.CommandLine.Tui.Widgets.Form;
using Spectre.Console.Rendering;
using EditingConfirmDialog = ChilliCream.Nitro.CommandLine.Tui.Editing.ConfirmDialog;

namespace ChilliCream.Nitro.CommandLine.Tui.Shell;

/// <summary>
/// Handles TUI events and renders the active tab, shell overlays, and status row.
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
    private readonly string? _actor;

    private readonly Func<MailWakeDaemonState>? _mailWakeDaemonState;
    private readonly IReadOnlyList<TuiQuitGate> _quitGates;
    private readonly TimeSpan _quitGateDrainBound;

    private int _activeTabIndex;
    private bool _quitGateResolved;
    private BoardDetailMode? _detailMode;
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
            mailWakeDaemonState,
            quitGates,
            quitGateDrainBound)
    {
    }

    /// <summary>
    /// Creates a shell with independent tab navigation and key dispatch, initially
    /// showing the first tab. Initializes every tab's current mode.
    /// </summary>
    /// <param name="tabs">The non-empty list of hosted tabs in display order.</param>
    /// <param name="initialWidth">The initial frame width.</param>
    /// <param name="initialHeight">The initial frame height.</param>
    /// <param name="searchMode">The task search mode, or null to disable shell search entry.</param>
    /// <param name="treeView">The dependency tree mode, or null to disable shell tree entry.</param>
    /// <param name="store">The task store, or null to disable shell task writes and detail entry.</param>
    /// <param name="actor">The acting agent for task writes, or null to refuse those writes.</param>
    /// <param name="tasksTabIndex">The tab that supports shell-level task editing overlays.</param>
    /// <param name="quitGates">
    /// Gates run before confirmed quit; unresolved work requires a second confirmation.
    /// Null registers no gates.
    /// </param>
    /// <param name="quitGateDrainBound">The per-gate drain bound, or the default when null.</param>
    /// <param name="mailWakeDaemonState">
    /// Supplies the daemon state for the footer when no toast is shown; null omits the badge.
    /// </param>
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
        _actor = actor;
        _mailWakeDaemonState = mailWakeDaemonState;
        _quitGates = quitGates ?? [];
        _quitGateDrainBound = quitGateDrainBound ?? s_quitGateDrainBound;
        _width = initialWidth;
        _height = initialHeight;

        // Initialize the current mode of every hosted tab.
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
    /// submissions in response.
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
    /// Renders the active overlay or mode, a tab strip when needed, and a bottom row
    /// showing the current toast or footer hints.
    /// </summary>
    public IRenderable Render()
    {
        var contentHeight = ContentHeight;

        var content = _confirmDialog is { } quitDialog
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
            ?? new Markup(FormatFooter(BuildFooterHints(), _width, _actor, _mailWakeDaemonState?.Invoke()));

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
    /// Renders tab titles with their mnemonic highlighted and the active tab styled
    /// with the selection theme.
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
    /// Brackets and styles the first case-insensitive mnemonic match, preserving
    /// the title's original case. Returns the escaped title when no match exists.
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
    /// active tab's.
    /// </summary>
    private bool HandleDataChanged() => BroadcastToTabs(new TuiMessage.RefreshRequested());

    /// <summary>
    /// Sends an effect-completion message to every tab's current mode to drain
    /// its queued outcomes.
    /// </summary>
    private bool HandleEffectCompleted() => BroadcastToTabs(new TuiMessage.EffectCompleted());

    /// <summary>
    /// Sends the message to every tab's current mode. Dispatches all active-tab
    /// follow-ups, but only toast follow-ups from inactive tabs.
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
        // Shell overlays consume input before mode or global dispatch.
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

        // A capturing mode receives raw keys before tab or semantic dispatch.
        if (ActiveMode is IRawKeyCapturingMode { IsInputCapturing: true } capturingMode)
        {
            foreach (var followUp in capturingMode.HandleRawKey(info))
            {
                HandleMessage(followUp);
            }

            return true;
        }

        // Tab switching is checked ahead of the active tab's own dispatch.
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

        // A failed save leaves the form open with its entered values; only
        // a successful save closes it.
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

        // A failed save leaves the form open with its entered values; only
        // a successful save closes it.
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

            case TuiMessage.OpenSelected when ActiveMode is BoardMode:
                return TryOpenDetail();

            case TuiMessage.FocusSearchRequested:
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
    /// Runs registered gates on the first quit confirmation and raises
    /// <see cref="QuitConfirmed"/> if no work remains unresolved. Otherwise,
    /// requires a second confirmation without rerunning the gates.
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
                // The gate itself failed to answer within its own bound plus a
                // grace period.
                outcomeUnknownCount++;
                continue;
            }
            catch (Exception)
            {
                // A gate that faults, whether it throws synchronously or returns
                // a faulted task, is reported as outcome-unknown for that gate.
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
    /// Opens the selected board task in a detail mode on the active tab's
    /// navigation stack.
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

        _detailMode ??= new BoardDetailMode(_store);
        _detailMode.OpenOnTask(id);
        SwitchTo(_detailMode);
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

        // A selected task becomes the new task's parent. Creating requires
        // no selection, unlike edit, lifecycle, and the pickers.
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
    /// Returns to the previous mode on the active tab's navigation stack,
    /// or does nothing when that stack is empty.
    /// </summary>
    private void PopMode() => ActiveTab.PopMode(_width, ContentHeight);

    /// <summary>
    /// Returns hints for the current input context. Capturing contexts show only
    /// their own hints; other modes add unsuppressed global hints and, when
    /// multiple tabs are hosted, the tab-switch hint.
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

        // Capturing modes supply their own footer hints.
        if (ActiveMode is IRawKeyCapturingMode { IsInputCapturing: true } capturingMode)
        {
            return capturingMode.CapturingHints;
        }

        // Hide the task-edit hint when no acting identity is available.
        var suppressed = _actor is null
            ? [.. ActiveMode.SuppressedGlobalHints, new KeyHint("e", "edit")]
            : ActiveMode.SuppressedGlobalHints;
        var hints = ActiveTab.Dispatcher.CombineHints(contextHints, suppressed);
        return _tabs.Count > 1 ? [.. hints, TabSwitchKeys.Hint] : hints;
    }

    /// <summary>
    /// Formats footer hints on the left and the optional daemon badge and acting
    /// identity on the right. The badge is shown whole when space remains after
    /// hints; the identity is truncated with an ellipsis or omitted when it cannot fit.
    /// </summary>
    private static string FormatFooter(
        IReadOnlyList<KeyHint> hints, int width, string? actor, MailWakeDaemonState? mailWakeDaemonState)
    {
        var hintMarkup = FormatFooterHints(hints, width, out var hintPlainWidth);
        var available = width - hintPlainWidth - (hintPlainWidth > 0 ? DisplayWidth.Measure(FooterSeparator) : 0);

        if (available <= 0)
        {
            return hintMarkup;
        }

        var trailingMarkup = string.Empty;
        var trailingPlainWidth = 0;

        if (mailWakeDaemonState is { } state)
        {
            var badgeText = FormatMailWakeDaemonBadge(state);

            if (DisplayWidth.Measure(badgeText) <= available)
            {
                var badgeStyle = ThemeTokens.GetStyle(MailWakeDaemonStyleToken(state)).ToMarkup();
                trailingMarkup = $"[{badgeStyle}]{Markup.Escape(badgeText)}[/]";
                trailingPlainWidth = DisplayWidth.Measure(badgeText);
                available -= trailingPlainWidth;
            }
        }

        if (!string.IsNullOrEmpty(actor))
        {
            var separatorNeeded = trailingPlainWidth > 0 ? DisplayWidth.Measure(FooterSeparator) : 0;
            var identityAvailable = available - separatorNeeded;

            string? identityText = null;

            if (DisplayWidth.Measure(actor) <= identityAvailable)
            {
                identityText = actor;
            }
            else if (identityAvailable > DisplayWidth.Measure(FooterEllipsis))
            {
                identityText = DisplayWidth.Truncate(actor, identityAvailable);
            }

            if (identityText is not null)
            {
                var identityStyle = ThemeTokens.GetStyle("footer.identity").ToMarkup();
                var identityMarkup = $"[{identityStyle}]{Markup.Escape(identityText)}[/]";

                trailingMarkup = trailingPlainWidth > 0
                    ? trailingMarkup + FooterSeparator + identityMarkup
                    : identityMarkup;
                trailingPlainWidth += separatorNeeded + DisplayWidth.Measure(identityText);
            }
        }

        if (trailingPlainWidth == 0)
        {
            return hintMarkup;
        }

        var padding = Math.Max(0, width - hintPlainWidth - trailingPlainWidth);

        return hintMarkup + new string(' ', padding) + trailingMarkup;
    }

    /// <summary>
    /// The daemon state as a short footer badge label.
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
    internal static string FormatFooterHints(IReadOnlyList<KeyHint> hints, int width, out int plainWidth)
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

        var separatorWidth = DisplayWidth.Measure(FooterSeparator);
        var ellipsisWidth = DisplayWidth.Measure(FooterEllipsis);
        var fullPlainWidth = plainItems.Sum(DisplayWidth.Measure) + separatorWidth * (hints.Count - 1);

        if (fullPlainWidth <= width)
        {
            plainWidth = fullPlainWidth;
            return string.Join(FooterSeparator, markupItems);
        }

        var included = 0;
        var usedWidth = 0;
        var trailerWidth = separatorWidth + ellipsisWidth;

        for (var i = 0; i < hints.Count; i++)
        {
            var itemWidth = (i == 0 ? 0 : separatorWidth) + DisplayWidth.Measure(plainItems[i]);

            if (usedWidth + itemWidth + trailerWidth > width)
            {
                break;
            }

            usedWidth += itemWidth;
            included++;
        }

        if (included == 0)
        {
            plainWidth = width >= ellipsisWidth ? ellipsisWidth : 0;
            return width >= ellipsisWidth ? FooterEllipsis : string.Empty;
        }

        plainWidth = usedWidth + trailerWidth;
        return string.Join(FooterSeparator, markupItems.Take(included)) + FooterSeparator + FooterEllipsis;
    }
}
