using ChilliCream.Nitro.CommandLine.Tui.Input;
using CursorDirection = ChilliCream.Nitro.CommandLine.Tui.Input.CursorDirection;

namespace ChilliCream.Nitro.CommandLine.Tui.Mail;

/// <summary>
/// Builds the mail tab's key bindings for navigation, mailbox and view selection,
/// message actions, and exit requests.
/// </summary>
internal static class MailKeyMap
{
    /// <summary>
    /// The read/unread footer hint, hidden when mail actions are read-only.
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
