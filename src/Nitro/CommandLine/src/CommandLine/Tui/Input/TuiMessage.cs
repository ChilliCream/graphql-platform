namespace ChilliCream.Nitro.CommandLine.Tui.Input;

/// <summary>
/// A semantic intent produced by the keymap layer. Modes and the shell handle these
/// instead of raw key input.
/// </summary>
internal abstract record TuiMessage
{
    private TuiMessage()
    {
    }

    /// <summary>
    /// The user asked to quit. The shell is expected to confirm before exiting.
    /// </summary>
    public sealed record QuitRequested : TuiMessage;

    /// <summary>
    /// The pending quit was confirmed.
    /// </summary>
    public sealed record ConfirmQuit : TuiMessage;

    /// <summary>
    /// The pending quit was cancelled.
    /// </summary>
    public sealed record CancelQuit : TuiMessage;

    /// <summary>
    /// The active mode should reload its data.
    /// </summary>
    public sealed record RefreshRequested : TuiMessage;

    /// <summary>
    /// The graph should switch between its tree and canvas projections.
    /// </summary>
    public sealed record ToggleGraphProjection : TuiMessage;

    /// <summary>
    /// Graph canvas nodes should switch between boxed and compact rendering.
    /// </summary>
    public sealed record ToggleGraphCompact : TuiMessage;

    /// <summary>
    /// The graph canvas should toggle its parent-child edge overlay.
    /// </summary>
    public sealed record ToggleGraphParentChild : TuiMessage;

    /// <summary>
    /// The graph should toggle the visibility of terminal tasks.
    /// </summary>
    public sealed record ToggleGraphClosed : TuiMessage;

    /// <summary>
    /// The selected graph epic or super-node should collapse.
    /// </summary>
    public sealed record CollapseSelectedGraphEpic : TuiMessage;

    /// <summary>
    /// The selected graph epic or super-node should expand.
    /// </summary>
    public sealed record ExpandSelectedGraphEpic : TuiMessage;

    /// <summary>
    /// Every visible graph epic should collapse.
    /// </summary>
    public sealed record CollapseAllGraphEpics : TuiMessage;

    /// <summary>
    /// Every graph epic should expand.
    /// </summary>
    public sealed record ExpandAllGraphEpics : TuiMessage;

    /// <summary>
    /// An asynchronous effect completed. A mode that owns no effect queue
    /// ignores this; one that does is expected to drain it the same way it
    /// already does on every other message.
    /// </summary>
    public sealed record EffectCompleted : TuiMessage;

    /// <summary>
    /// The selection cursor should move one step in <paramref name="Direction"/>.
    /// </summary>
    public sealed record MoveCursor(CursorDirection Direction) : TuiMessage;

    /// <summary>
    /// The selection cursor should jump to <paramref name="Edge"/> of the current list.
    /// </summary>
    public sealed record MoveToEdge(EdgeTarget Edge) : TuiMessage;

    /// <summary>
    /// The active view should change by <paramref name="Delta"/> positions.
    /// </summary>
    public sealed record CycleView(int Delta) : TuiMessage;

    /// <summary>
    /// The active mode should toggle its maximized layout.
    /// </summary>
    public sealed record ToggleMaximize : TuiMessage;

    /// <summary>
    /// The current selection should be opened.
    /// </summary>
    public sealed record OpenSelected : TuiMessage;

    /// <summary>
    /// The identifier of the current selection should be copied.
    /// </summary>
    public sealed record CopySelectedId : TuiMessage;

    /// <summary>
    /// A toast with <paramref name="Text"/> should be shown using <paramref name="Style"/>.
    /// </summary>
    public sealed record ShowToast(string Text, ToastStyle Style) : TuiMessage;

    /// <summary>
    /// The active mode should be left in favor of whichever mode preceded it.
    /// The shell is expected to pop its mode stack; has no effect at the base
    /// mode.
    /// </summary>
    public sealed record Back : TuiMessage;

    /// <summary>
    /// The search mode should become active with its query input focused.
    /// </summary>
    public sealed record FocusSearchRequested : TuiMessage;

    /// <summary>
    /// The dependency tree should become active, rooted on the active mode's
    /// currently selected task.
    /// </summary>
    public sealed record OpenTreeRequested : TuiMessage;

    /// <summary>
    /// The task editor should open for the active mode's currently selected
    /// task.
    /// </summary>
    public sealed record EditRequested : TuiMessage;

    /// <summary>
    /// The close (or, when the task is already closed, reopen) confirmation
    /// should open for the active mode's currently selected task.
    /// </summary>
    public sealed record CloseOrReopenRequested : TuiMessage;

    /// <summary>
    /// The delete confirmation should open for the active mode's currently
    /// selected task.
    /// </summary>
    public sealed record DeleteRequested : TuiMessage;

    /// <summary>
    /// The status quick picker should open for the active mode's currently
    /// selected task.
    /// </summary>
    public sealed record StatusPickerRequested : TuiMessage;

    /// <summary>
    /// The priority quick picker should open for the active mode's currently
    /// selected task.
    /// </summary>
    public sealed record PriorityPickerRequested : TuiMessage;

    /// <summary>
    /// The dependency tree should toggle between the blocking-dependency and
    /// parent-child edge sets.
    /// </summary>
    public sealed record ToggleTreeEdgeMode : TuiMessage;

    /// <summary>
    /// The dependency tree should toggle between showing what the root
    /// depends on and what depends on the root.
    /// </summary>
    public sealed record ToggleTreeDirection : TuiMessage;

    /// <summary>
    /// The dependency tree should pop its breadcrumb stack and re-root on the
    /// popped task.
    /// </summary>
    public sealed record NavigateTreeBack : TuiMessage;

    /// <summary>
    /// The task create form should open, preset to the task type. When the
    /// active mode has a currently selected task, the new task is created as
    /// a child of it; otherwise it is created top-level.
    /// </summary>
    public sealed record CreateTaskRequested : TuiMessage;

    /// <summary>
    /// The task create form should open the same way as
    /// <see cref="CreateTaskRequested"/>, preset to the epic type.
    /// </summary>
    public sealed record CreateEpicRequested : TuiMessage;

    /// <summary>
    /// The active mode's currently selected item should toggle between read
    /// and unread.
    /// </summary>
    public sealed record ToggleReadRequested : TuiMessage;

    /// <summary>
    /// The archive confirmation should open for the active mode's currently
    /// selected item.
    /// </summary>
    public sealed record ArchiveRequested : TuiMessage;

    /// <summary>
    /// The compose form should open.
    /// </summary>
    public sealed record ComposeRequested : TuiMessage;

    /// <summary>
    /// The reply form should open for the active mode's currently selected
    /// item.
    /// </summary>
    public sealed record ReplyRequested : TuiMessage;

    /// <summary>
    /// The mail board's Inbox mailbox should become the active mailbox.
    /// </summary>
    public sealed record SelectInboxRequested : TuiMessage;

    /// <summary>
    /// The mail board's Sent mailbox should become the active mailbox.
    /// </summary>
    public sealed record SelectSentRequested : TuiMessage;

    /// <summary>
    /// The mail board's All mailbox should become the active mailbox.
    /// </summary>
    public sealed record SelectAllMailRequested : TuiMessage;

    /// <summary>
    /// The mail board's Workspace mailbox should become the active mailbox.
    /// </summary>
    public sealed record SelectWorkspaceMailRequested : TuiMessage;

    /// <summary>
    /// The mail board's agent filter quick picker should open, scoped to
    /// <see cref="ChilliCream.Nitro.CommandLine.Tui.Mail.MailMailbox.Workspace"/>.
    /// </summary>
    public sealed record AgentFilterPickerRequested : TuiMessage;

    /// <summary>
    /// The mail board's list pane should toggle between
    /// <see cref="ChilliCream.Nitro.CommandLine.Tui.Mail.MailListMode.Threads"/>
    /// and <see cref="ChilliCream.Nitro.CommandLine.Tui.Mail.MailListMode.Flat"/>
    /// (Shift+V).
    /// </summary>
    public sealed record ToggleListModeRequested : TuiMessage;

    /// <summary>
    /// The mail board's list pane should enter its fold-prefix capture
    /// state (the vim <c>z</c> prefix): the next raw key resolves one of
    /// za/zo/zc/zR/zM, or is dropped when it matches none of them.
    /// </summary>
    public sealed record FoldPrefixRequested : TuiMessage;

    /// <summary>
    /// The active mode's own inline search input should gain focus.
    /// </summary>
    public sealed record SearchRequested : TuiMessage;

    /// <summary>
    /// The active mode's secondary scope filter should cycle to its next
    /// value.
    /// </summary>
    public sealed record CycleScopeRequested : TuiMessage;

    /// <summary>
    /// The promote form should open for the active mode's currently selected
    /// journal entry.
    /// </summary>
    public sealed record PromoteRequested : TuiMessage;

    /// <summary>
    /// The forget confirmation should open for the active mode's currently
    /// selected curated memory.
    /// </summary>
    public sealed record ForgetRequested : TuiMessage;
}
