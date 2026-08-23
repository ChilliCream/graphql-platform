using ChilliCream.Nitro.CommandLine.Tui.Input;
using CursorDirection = ChilliCream.Nitro.CommandLine.Tui.Input.CursorDirection;

namespace ChilliCream.Nitro.CommandLine.Tui.Mail;

/// <summary>
/// Builds the mail board's key table, for the mail command's own
/// <see cref="KeyDispatcher"/> in place of <see cref="KeyMap.CreateDefaultGlobal"/>:
/// vim-style navigation (j/k/h/l and arrow keys, g/G for edges), Enter to
/// focus the detail pane, Tab to switch panes, u to toggle read/unread, a
/// to archive, r to reply, c to compose, Shift+R to refresh, y to copy the
/// selected message id, f to cycle the list filter, p to open the Workspace
/// agent filter picker, t to toggle the detail pane's thread view, Shift+V
/// to toggle the list pane between threaded and flat rows, z as a fold
/// prefix (za/zo/zc toggle/open/close a thread, zR/zM unfold/fold every
/// thread - the vim binding docs/research-mail-clients-tui.md's "concrete
/// suggestion" section names directly), Shift+I/Shift+S/Shift+L/Shift+W to
/// jump directly to the Inbox/Sent/All/Workspace mailbox, q and Ctrl+C to
/// request quit, and Escape to leave the mode.
/// </summary>
/// <remarks>
/// This is a standalone table rather than an extension of the task
/// board's global table: most of that table's bindings (edit, close,
/// delete, the status and priority pickers, search, the dependency tree,
/// task creation) have no meaning for a mail board, and refresh moves off
/// the bare r chord (the global table's binding) to make room for reply.
/// The mailbox jump keys are a direct selection, not a cycle, matching the
/// convention of Gmail's g i/g t/g a and mu4e's j+letter; <see cref="KeyDispatcher"/>
/// resolves only single key chords with no concept of a chord prefix, so
/// each mailbox gets its own Shift+letter chord instead of a two-key "g"
/// prefix table. Shift+I carries its own dedicated <see cref="KeyHint"/>,
/// separate from Shift+S/L/W's, so it reads as the footer's persistent,
/// fixed-position exit affordance back to Inbox from
/// <see cref="MailMailbox.Workspace"/>'s read-only mode (see
/// <see cref="MailMode"/>'s class doc), rather than being buried in one
/// combined "jump to a mailbox" hint. This table's bindings are static and
/// carried once into the tabbed shell's per-tab <see cref="KeyDispatcher"/>
/// at startup (see <c>AgentTuiLauncher.BuildMailTab</c>); the four hints
/// <see cref="MailMailbox.Workspace"/> makes inert (u, a, r, c) are exposed
/// here as named constants so <see cref="MailMode.SuppressedGlobalHints"/>
/// can hide exactly them, by <see cref="KeyHint"/> value equality, from the
/// footer while that mailbox is active, without this table itself needing
/// to vary.
/// </remarks>
internal static class MailKeyMap
{
    /// <summary>
    /// The footer hint for the u chord, exposed so
    /// <see cref="MailMode.SuppressedGlobalHints"/> can hide it, by value,
    /// while <see cref="MailMailbox.Workspace"/> makes the gesture inert.
    /// </summary>
    public static readonly KeyHint ToggleReadHint = new("u", "read/unread");

    /// <summary>
    /// The footer hint for the a chord; see <see cref="ToggleReadHint"/>.
    /// </summary>
    public static readonly KeyHint ArchiveHint = new("a", "archive");

    /// <summary>
    /// The footer hint for the r chord; see <see cref="ToggleReadHint"/>.
    /// </summary>
    public static readonly KeyHint ReplyHint = new("r", "reply");

    /// <summary>
    /// The footer hint for the c chord; see <see cref="ToggleReadHint"/>.
    /// </summary>
    public static readonly KeyHint ComposeHint = new("c", "compose");

    public static KeyMap CreateDefault() => new(
    [
        new KeyBinding(
            new KeyChord(ConsoleKey.J, ConsoleModifiers.None, 'j'),
            () => new TuiMessage.MoveCursor(CursorDirection.Down),
            new KeyHint("hjkl", "move")),
        new KeyBinding(
            new KeyChord(ConsoleKey.DownArrow, ConsoleModifiers.None, '\0'),
            () => new TuiMessage.MoveCursor(CursorDirection.Down)),
        new KeyBinding(
            new KeyChord(ConsoleKey.K, ConsoleModifiers.None, 'k'),
            () => new TuiMessage.MoveCursor(CursorDirection.Up)),
        new KeyBinding(
            new KeyChord(ConsoleKey.UpArrow, ConsoleModifiers.None, '\0'),
            () => new TuiMessage.MoveCursor(CursorDirection.Up)),
        new KeyBinding(
            new KeyChord(ConsoleKey.H, ConsoleModifiers.None, 'h'),
            () => new TuiMessage.MoveCursor(CursorDirection.Left)),
        new KeyBinding(
            new KeyChord(ConsoleKey.LeftArrow, ConsoleModifiers.None, '\0'),
            () => new TuiMessage.MoveCursor(CursorDirection.Left)),
        new KeyBinding(
            new KeyChord(ConsoleKey.L, ConsoleModifiers.None, 'l'),
            () => new TuiMessage.MoveCursor(CursorDirection.Right)),
        new KeyBinding(
            new KeyChord(ConsoleKey.RightArrow, ConsoleModifiers.None, '\0'),
            () => new TuiMessage.MoveCursor(CursorDirection.Right)),
        new KeyBinding(
            new KeyChord(ConsoleKey.Tab, ConsoleModifiers.None, '\t'),
            () => new TuiMessage.MoveCursor(CursorDirection.Right),
            new KeyHint("tab", "switch pane")),
        new KeyBinding(
            new KeyChord(ConsoleKey.G, ConsoleModifiers.None, 'g'),
            () => new TuiMessage.MoveToEdge(EdgeTarget.Top)),
        new KeyBinding(
            new KeyChord(ConsoleKey.G, ConsoleModifiers.Shift, 'G'),
            () => new TuiMessage.MoveToEdge(EdgeTarget.Bottom)),
        new KeyBinding(
            new KeyChord(ConsoleKey.Enter, ConsoleModifiers.None, '\r'),
            () => new TuiMessage.OpenSelected(),
            new KeyHint("enter", "focus detail")),
        new KeyBinding(
            new KeyChord(ConsoleKey.U, ConsoleModifiers.None, 'u'),
            () => new TuiMessage.ToggleReadRequested(),
            ToggleReadHint),
        new KeyBinding(
            new KeyChord(ConsoleKey.A, ConsoleModifiers.None, 'a'),
            () => new TuiMessage.ArchiveRequested(),
            ArchiveHint),
        new KeyBinding(
            new KeyChord(ConsoleKey.R, ConsoleModifiers.None, 'r'),
            () => new TuiMessage.ReplyRequested(),
            ReplyHint),
        new KeyBinding(
            new KeyChord(ConsoleKey.C, ConsoleModifiers.None, 'c'),
            () => new TuiMessage.ComposeRequested(),
            ComposeHint),
        new KeyBinding(
            new KeyChord(ConsoleKey.R, ConsoleModifiers.Shift, 'R'),
            () => new TuiMessage.RefreshRequested()),
        new KeyBinding(
            new KeyChord(ConsoleKey.Y, ConsoleModifiers.None, 'y'),
            () => new TuiMessage.CopySelectedId(),
            new KeyHint("y", "copy id")),
        new KeyBinding(
            new KeyChord(ConsoleKey.F, ConsoleModifiers.None, 'f'),
            () => new TuiMessage.CycleView(1),
            new KeyHint("f", "filter")),
        new KeyBinding(
            new KeyChord(ConsoleKey.F, ConsoleModifiers.Shift, 'F'),
            () => new TuiMessage.CycleView(-1)),
        new KeyBinding(
            new KeyChord(ConsoleKey.T, ConsoleModifiers.None, 't'),
            () => new TuiMessage.ToggleMaximize(),
            new KeyHint("t", "thread")),
        new KeyBinding(
            new KeyChord(ConsoleKey.V, ConsoleModifiers.Shift, 'V'),
            () => new TuiMessage.ToggleListModeRequested(),
            new KeyHint("V", "flat/threads")),
        new KeyBinding(
            new KeyChord(ConsoleKey.Z, ConsoleModifiers.None, 'z'),
            () => new TuiMessage.FoldPrefixRequested(),
            new KeyHint("za/zo/zc/zR/zM", "fold")),
        new KeyBinding(
            new KeyChord(ConsoleKey.P, ConsoleModifiers.None, 'p'),
            () => new TuiMessage.AgentFilterPickerRequested(),
            new KeyHint("p", "filter agent")),
        new KeyBinding(
            new KeyChord(ConsoleKey.I, ConsoleModifiers.Shift, 'I'),
            () => new TuiMessage.SelectInboxRequested(),
            new KeyHint("I", "inbox")),
        new KeyBinding(
            new KeyChord(ConsoleKey.S, ConsoleModifiers.Shift, 'S'),
            () => new TuiMessage.SelectSentRequested(),
            new KeyHint("S/L/W", "sent/all/workspace")),
        new KeyBinding(
            new KeyChord(ConsoleKey.L, ConsoleModifiers.Shift, 'L'),
            () => new TuiMessage.SelectAllMailRequested()),
        new KeyBinding(
            new KeyChord(ConsoleKey.W, ConsoleModifiers.Shift, 'W'),
            () => new TuiMessage.SelectWorkspaceMailRequested()),
        new KeyBinding(
            new KeyChord(ConsoleKey.C, ConsoleModifiers.Control, ''),
            () => new TuiMessage.QuitRequested()),
        new KeyBinding(
            new KeyChord(ConsoleKey.Escape, ConsoleModifiers.None, ''),
            () => new TuiMessage.Back(),
            new KeyHint("esc", "back")),
        new KeyBinding(
            new KeyChord(ConsoleKey.Q, ConsoleModifiers.None, 'q'),
            () => new TuiMessage.QuitRequested(),
            new KeyHint("q", "quit"))
    ]);
}
