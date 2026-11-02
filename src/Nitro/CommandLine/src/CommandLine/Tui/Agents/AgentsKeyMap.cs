using ChilliCream.Nitro.CommandLine.Tui.Input;
using CursorDirection = ChilliCream.Nitro.CommandLine.Tui.Input.CursorDirection;

namespace ChilliCream.Nitro.CommandLine.Tui.Agents;

/// <summary>
/// Builds the Agents tab's key bindings for navigation, delete actions, search, and
/// exit requests. Bound only to this tab, not the global key table.
/// </summary>
internal static class AgentsKeyMap
{
    /// <summary>
    /// Builds the default bindings. <paramref name="selectedName"/> is read when d is
    /// pressed, so the delete message always names the row selected at that moment.
    /// </summary>
    public static KeyMap CreateDefault(Func<string?> selectedName) => new(
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
            new KeyChord(ConsoleKey.G, ConsoleModifiers.None, 'g'),
            () => new TuiMessage.MoveToEdge(EdgeTarget.Top)),
        new KeyBinding(
            new KeyChord(ConsoleKey.G, ConsoleModifiers.Shift, 'G'),
            () => new TuiMessage.MoveToEdge(EdgeTarget.Bottom)),
        new KeyBinding(
            new KeyChord(ConsoleKey.Enter, ConsoleModifiers.None, '\r'),
            () => new TuiMessage.OpenSelected(),
            new KeyHint("enter", "open")),
        new KeyBinding(
            new KeyChord(ConsoleKey.D, ConsoleModifiers.None, 'd'),
            () => new TuiMessage.DeleteAgentRequested(selectedName() ?? string.Empty),
            new KeyHint("d", "delete")),
        new KeyBinding(
            new KeyChord(ConsoleKey.D, ConsoleModifiers.Shift, 'D'),
            () => new TuiMessage.DeleteOfflineAgentsRequested(),
            new KeyHint("D", "delete offline")),
        new KeyBinding(
            new KeyChord(ConsoleKey.Y, ConsoleModifiers.None, 'y'),
            () => new TuiMessage.CopySelectedId(),
            new KeyHint("y", "copy id")),
        new KeyBinding(
            new KeyChord(ConsoleKey.R, ConsoleModifiers.None, 'r'),
            () => new TuiMessage.RefreshRequested(),
            new KeyHint("r", "refresh")),
        new KeyBinding(
            new KeyChord(ConsoleKey.Oem2, ConsoleModifiers.None, '/'),
            () => new TuiMessage.SearchRequested(),
            new KeyHint("/", "search")),
        new KeyBinding(
            new KeyChord(ConsoleKey.C, ConsoleModifiers.Control, '\u0003'),
            () => new TuiMessage.QuitRequested()),
        new KeyBinding(
            new KeyChord(ConsoleKey.Escape, ConsoleModifiers.None, '\u001b'),
            () => new TuiMessage.Back()),
        new KeyBinding(
            new KeyChord(ConsoleKey.Q, ConsoleModifiers.None, 'q'),
            () => new TuiMessage.QuitRequested())
    ]);
}
