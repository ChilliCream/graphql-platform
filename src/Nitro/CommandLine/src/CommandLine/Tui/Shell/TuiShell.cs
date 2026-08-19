using System.Globalization;
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
/// The root handler and renderer for <see cref="TuiApplication"/>. Owns the mode
/// stack and composites the shell-level overlays (the quit confirmation, the task
/// editor, the close/reopen/delete confirmation, the status and priority quick
/// pickers, the task create form, and the toast row) over whichever mode is active.
/// </summary>
internal sealed class TuiShell
{
    private const string QuitConfirmMessage = "Quit? (y/n)";
    private const int StatusRowHeight = 1;
    private const string FooterSeparator = "  ";
    private const string FooterEllipsis = "…";

    private readonly KeyDispatcher _dispatcher;
    private readonly Stack<ITuiMode> _modeStack = new();
    private readonly Toaster _toaster = new();
    private readonly SearchMode? _searchMode;
    private readonly DependencyTreeView? _treeView;
    private readonly ITaskStore? _store;
    private readonly string? _actor;

    private ITuiMode _activeMode;
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
        string? actor = null)
    {
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _activeMode = activeMode ?? throw new ArgumentNullException(nameof(activeMode));
        _searchMode = searchMode;
        _treeView = treeView;
        _store = store;
        _actor = actor;
        _width = initialWidth;
        _height = initialHeight;
        _activeMode.OnEnter();
    }

    /// <summary>
    /// Raised once a pending quit is confirmed. The caller is expected to stop the
    /// <see cref="TuiApplication"/> event loop in response.
    /// </summary>
    public event Action? QuitConfirmed;

    private int ContentHeight => Math.Max(0, _height - StatusRowHeight);

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
        TuiEvent.DataChangedEvent => HandleMessage(new TuiMessage.RefreshRequested()),
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
                                : _activeMode.Render(_width, contentHeight);

        var toastRow = _toaster.Render() ?? (IRenderable)new Markup(FormatFooter(BuildFooterHints(), _width));

        return new Layout("root").SplitRows(
            new Layout("content", content),
            new Layout("status", toastRow).Size(StatusRowHeight));
    }

    private bool HandleResize(int width, int height)
    {
        _width = width;
        _height = height;
        _activeMode.OnResize(width, ContentHeight);
        return true;
    }

    private bool HandleTick(DateTimeOffset now)
    {
        var toastDirty = _toaster.Tick(now);
        var searchDirty = _searchMode is { } search
            && ReferenceEquals(_activeMode, search)
            && search.TickAsync(now, CancellationToken.None).GetAwaiter().GetResult();

        return toastDirty || searchDirty;
    }

    private bool HandleKey(ConsoleKeyInfo info)
    {
        // The task editor, the close/reopen/delete confirmation, and the task
        // create form are modal: while one is active it consumes every key
        // itself, and unresolved keys are swallowed rather than falling
        // through to the active mode or the global table. The quit confirmation
        // keeps its original
        // dialog-priority-with-global-fallback behavior: a key unresolved by
        // its own key map falls through to dispatch against the active
        // mode's key map.
        if (_confirmDialog is { } quitDialog)
        {
            var quitMessage = _dispatcher.Dispatch(info, quitDialog.KeyMap);
            return quitMessage is not null && HandleMessage(quitMessage);
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
            && ReferenceEquals(_activeMode, searchMode)
            && searchMode.Focus == SearchFocus.Input
            && info.Key is not (ConsoleKey.Escape or ConsoleKey.Tab or ConsoleKey.Enter))
        {
            searchMode.HandleQueryKey(info, DateTimeOffset.UtcNow);
            return true;
        }

        var message = _dispatcher.Dispatch(info, _activeMode.KeyMap);
        return message is not null && HandleMessage(message);
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

        _editorForm = null;
        HandleMessage(outcome.ToShowToast());
        HandleMessage(new TuiMessage.RefreshRequested());
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

        _createForm = null;
        HandleMessage(outcome.ToShowToast());
        HandleMessage(new TuiMessage.RefreshRequested());

        if (outcome is TaskCreateOutcome.Succeeded succeeded)
        {
            _activeMode.SelectTask(succeeded.TaskId);
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
                _confirmDialog = null;
                QuitConfirmed?.Invoke();
                return true;

            case TuiMessage.CancelQuit:
                _confirmDialog = null;
                return true;

            case TuiMessage.ShowToast showToast:
                _toaster.Enqueue(showToast.Text, showToast.Style, DateTimeOffset.UtcNow);
                return true;

            case TuiMessage.Back:
                PopMode();
                return true;

            case TuiMessage.OpenSelected when _activeMode is BoardMode:
                return TryOpenDetail();

            case TuiMessage.FocusSearchRequested:
                if (_searchMode is not { } search)
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
                foreach (var followUp in _activeMode.Handle(message))
                {
                    HandleMessage(followUp);
                }

                return true;
        }
    }

    /// <summary>
    /// Switches to the board's task detail mode, rooted on the board's
    /// currently selected task. Reuses the same mode-stack semantics as
    /// <see cref="TryOpenTree"/>: Back returns to the board with its
    /// selection untouched.
    /// </summary>
    private bool TryOpenDetail()
    {
        if (_store is null)
        {
            return false;
        }

        if (_activeMode.SelectedTaskId is not { } id)
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

        if (_activeMode.SelectedTaskId is not { } id)
        {
            return ShowToastNow("No task selected.", ToastStyle.Warn);
        }

        tree.EnterOnTask(id);
        SwitchTo(tree);
        return true;
    }

    private bool TryOpenEditor()
    {
        if (_store is null)
        {
            return false;
        }

        if (_activeMode.SelectedTaskId is not { } id)
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
        if (_store is null)
        {
            return false;
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
        if (_store is null)
        {
            return false;
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
        if (_store is null)
        {
            return false;
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
        if (_store is null)
        {
            return false;
        }

        // A selected task becomes the new task's parent: creating unconditionally
        // requires no selection (unlike edit, lifecycle, and the pickers), so no
        // "no task selected" toast gates this on the active mode's selection.
        _createForm = new TaskCreateForm(typePreset, _activeMode.SelectedTaskId);
        return true;
    }

    private TaskItem? LoadSelectedTask()
    {
        if (_store is null)
        {
            return null;
        }

        if (_activeMode.SelectedTaskId is not { } id)
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

    private void SwitchTo(ITuiMode mode)
    {
        if (ReferenceEquals(_activeMode, mode))
        {
            return;
        }

        _modeStack.Push(_activeMode);
        _activeMode = mode;
        _activeMode.OnResize(_width, ContentHeight);
        _activeMode.OnEnter();
    }

    private void PopMode()
    {
        if (_modeStack.Count == 0)
        {
            return;
        }

        _activeMode = _modeStack.Pop();
        _activeMode.OnResize(_width, ContentHeight);
        _activeMode.OnEnter();
    }

    /// <summary>
    /// Builds the footer's hint list for whichever context currently owns
    /// key input, mirroring <see cref="HandleKey"/>'s own priority order so
    /// the footer can never show a hint the active input context would not
    /// actually honor. The fully modal overlays (the discard confirmation,
    /// the task editor, the lifecycle confirmation, the quick pickers, and
    /// the task create form) show only their own hints, since they consume
    /// every key themselves; every other context's hints are followed by the
    /// global table's, with quit last.
    /// </summary>
    private IReadOnlyList<KeyHint> BuildFooterHints()
    {
        if (_confirmDialog is not null)
        {
            return Combine(ConfirmDialog.Hints);
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

        var contextHints = _activeMode.KeyMap?.Hints ?? [];

        if (_searchMode is { } search
            && ReferenceEquals(_activeMode, search)
            && search.Focus == SearchFocus.Input)
        {
            contextHints = [SearchMode.TypingHint, .. contextHints];
        }

        return Combine(contextHints);
    }

    /// <summary>
    /// Appends the global key table's hints after <paramref name="contextHints"/>,
    /// matching how <see cref="KeyDispatcher.Dispatch"/> falls back to the
    /// global table for anything a context-specific key table does not bind.
    /// A global hint already present among <paramref name="contextHints"/>
    /// (for example a mode's own back-to-global Escape binding) is not
    /// repeated.
    /// </summary>
    private IReadOnlyList<KeyHint> Combine(IReadOnlyList<KeyHint> contextHints)
    {
        var globalHints = _dispatcher.GlobalKeyMap.Hints;

        if (contextHints.Count == 0)
        {
            return globalHints;
        }

        if (globalHints.Count == 0)
        {
            return contextHints;
        }

        var seen = new HashSet<KeyHint>(contextHints);
        var combined = new List<KeyHint>(contextHints.Count + globalHints.Count);
        combined.AddRange(contextHints);

        foreach (var hint in globalHints)
        {
            if (seen.Add(hint))
            {
                combined.Add(hint);
            }
        }

        return combined;
    }

    /// <summary>
    /// Formats <paramref name="hints"/> as the footer's single status-row
    /// line: dimmed key labels, normal-weight action labels, separated hint
    /// entries, truncated with a trailing ellipsis once <paramref name="width"/>
    /// cannot fit every hint.
    /// </summary>
    private static string FormatFooter(IReadOnlyList<KeyHint> hints, int width)
    {
        if (width <= 0 || hints.Count == 0)
        {
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

        var fullPlainWidth = plainItems.Sum(item => item.Length) + FooterSeparator.Length * (hints.Count - 1);

        if (fullPlainWidth <= width)
        {
            return string.Join(FooterSeparator, markupItems);
        }

        var included = 0;
        var usedWidth = 0;
        var trailerWidth = FooterSeparator.Length + FooterEllipsis.Length;

        for (var i = 0; i < hints.Count; i++)
        {
            var itemWidth = (i == 0 ? 0 : FooterSeparator.Length) + plainItems[i].Length;

            if (usedWidth + itemWidth + trailerWidth > width)
            {
                break;
            }

            usedWidth += itemWidth;
            included++;
        }

        if (included == 0)
        {
            return width >= FooterEllipsis.Length ? FooterEllipsis : string.Empty;
        }

        return string.Join(FooterSeparator, markupItems.Take(included)) + FooterSeparator + FooterEllipsis;
    }
}

/// <summary>
/// Which field a <see cref="TuiShell"/>'s active quick picker is editing.
/// </summary>
internal enum PickerKind
{
    Status,
    Priority
}

/// <summary>
/// Which form a <see cref="TuiShell"/>'s active discard confirmation applies
/// to.
/// </summary>
internal enum DiscardTarget
{
    EditorForm,
    CreateForm
}
