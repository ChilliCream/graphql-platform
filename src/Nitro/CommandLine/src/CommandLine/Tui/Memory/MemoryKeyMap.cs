using ChilliCream.Nitro.CommandLine.Tui.Input;
using CursorDirection = ChilliCream.Nitro.CommandLine.Tui.Input.CursorDirection;

namespace ChilliCream.Nitro.CommandLine.Tui.Memory;

/// <summary>
/// Builds the memory tab's key bindings for navigation, collection selection,
/// search, promotion, deletion, and exit requests.
/// </summary>
internal static class MemoryKeyMap
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
            new KeyChord(ConsoleKey.F, ConsoleModifiers.None, 'f'),
            () => new TuiMessage.CycleView(1),
            new KeyHint("f", "curated/journal")),
        new KeyBinding(
            new KeyChord(ConsoleKey.Oem2, ConsoleModifiers.None, '/'),
            () => new TuiMessage.SearchRequested(),
            new KeyHint("/", "search")),
        new KeyBinding(
            new KeyChord(ConsoleKey.P, ConsoleModifiers.None, 'p'),
            () => new TuiMessage.PromoteRequested(),
            new KeyHint("p", "promote")),
        new KeyBinding(
            new KeyChord(ConsoleKey.D, ConsoleModifiers.None, 'd'),
            () => new TuiMessage.ForgetRequested(),
            new KeyHint("d", "forget")),
        new KeyBinding(
            new KeyChord(ConsoleKey.R, ConsoleModifiers.Shift, 'R'),
            () => new TuiMessage.RefreshRequested()),
        new KeyBinding(
            new KeyChord(ConsoleKey.Y, ConsoleModifiers.None, 'y'),
            () => new TuiMessage.CopySelectedId(),
            new KeyHint("y", "copy id")),
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
