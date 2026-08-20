using ChilliCream.Nitro.CommandLine.Tui.Input;
using CursorDirection = ChilliCream.Nitro.CommandLine.Tui.Input.CursorDirection;

namespace ChilliCream.Nitro.CommandLine.Tui.Mail;

/// <summary>
/// Builds the mail board's key table, for the mail command's own
/// <see cref="KeyDispatcher"/> in place of <see cref="KeyMap.CreateDefaultGlobal"/>:
/// vim-style navigation (j/k/h/l and arrow keys, g/G for edges), Enter to
/// focus the detail pane, Tab to switch panes, u to toggle read/unread, a
/// to archive, r to reply, c to compose, Shift+R to refresh, y to copy the
/// selected message id, f to cycle the list filter, t to toggle the detail
/// pane's thread view, q and Ctrl+C to request quit, and Escape to leave
/// the mode.
/// </summary>
/// <remarks>
/// This is a standalone table rather than an extension of the task
/// board's global table: most of that table's bindings (edit, close,
/// delete, the status and priority pickers, search, the dependency tree,
/// task creation) have no meaning for a mail board, and refresh moves off
/// the bare r chord (the global table's binding) to make room for reply.
/// </remarks>
internal static class MailKeyMap
{
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
            new KeyHint("u", "read/unread")),
        new KeyBinding(
            new KeyChord(ConsoleKey.A, ConsoleModifiers.None, 'a'),
            () => new TuiMessage.ArchiveRequested(),
            new KeyHint("a", "archive")),
        new KeyBinding(
            new KeyChord(ConsoleKey.R, ConsoleModifiers.None, 'r'),
            () => new TuiMessage.ReplyRequested(),
            new KeyHint("r", "reply")),
        new KeyBinding(
            new KeyChord(ConsoleKey.C, ConsoleModifiers.None, 'c'),
            () => new TuiMessage.ComposeRequested(),
            new KeyHint("c", "compose")),
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
