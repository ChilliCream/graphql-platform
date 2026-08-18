using System.Diagnostics.CodeAnalysis;

namespace ChilliCream.Nitro.CommandLine.Tui.Input;

/// <summary>
/// A table of key bindings, looked up by the key chord that triggers them.
/// </summary>
internal sealed class KeyMap
{
    private readonly Dictionary<KeyChord, Func<TuiMessage>> _bindings;

    public KeyMap(IEnumerable<KeyBinding> bindings)
    {
        _bindings = bindings.ToDictionary(b => b.Chord, b => b.CreateMessage);
    }

    /// <summary>
    /// Resolves the <see cref="TuiMessage"/> bound to <paramref name="chord"/>. Returns
    /// <see langword="false"/> when <paramref name="chord"/> is unbound.
    /// </summary>
    public bool TryResolve(KeyChord chord, [NotNullWhen(true)] out TuiMessage? message)
    {
        if (_bindings.TryGetValue(chord, out var createMessage))
        {
            message = createMessage();
            return true;
        }

        message = null;
        return false;
    }

    /// <summary>
    /// The hardcoded global key bindings: vim-style navigation (j/k/h/l and arrow
    /// keys, g/G for edges), Enter to open, r to refresh, y to copy the selected id,
    /// q and Ctrl+C to request quit, Ctrl+N/Ctrl+P to cycle views, z to toggle the
    /// maximized layout, / to jump into search, t to open the dependency tree on
    /// the current selection, e to edit it, x to close or reopen it, X to delete
    /// it, and Escape to leave the active mode.
    /// </summary>
    public static KeyMap CreateDefaultGlobal() => new(
    [
        new KeyBinding(
            new KeyChord(ConsoleKey.J, ConsoleModifiers.None, 'j'),
            () => new TuiMessage.MoveCursor(CursorDirection.Down)),
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
            new KeyChord(ConsoleKey.G, ConsoleModifiers.None, 'g'),
            () => new TuiMessage.MoveToEdge(EdgeTarget.Top)),
        new KeyBinding(
            new KeyChord(ConsoleKey.G, ConsoleModifiers.Shift, 'G'),
            () => new TuiMessage.MoveToEdge(EdgeTarget.Bottom)),
        new KeyBinding(
            new KeyChord(ConsoleKey.Enter, ConsoleModifiers.None, '\r'),
            () => new TuiMessage.OpenSelected()),
        new KeyBinding(
            new KeyChord(ConsoleKey.R, ConsoleModifiers.None, 'r'),
            () => new TuiMessage.RefreshRequested()),
        new KeyBinding(
            new KeyChord(ConsoleKey.Y, ConsoleModifiers.None, 'y'),
            () => new TuiMessage.CopySelectedId()),
        new KeyBinding(
            new KeyChord(ConsoleKey.Q, ConsoleModifiers.None, 'q'),
            () => new TuiMessage.QuitRequested()),
        new KeyBinding(
            new KeyChord(ConsoleKey.C, ConsoleModifiers.Control, '\u0003'),
            () => new TuiMessage.QuitRequested()),
        new KeyBinding(
            new KeyChord(ConsoleKey.N, ConsoleModifiers.Control, '\u000e'),
            () => new TuiMessage.CycleView(1)),
        new KeyBinding(
            new KeyChord(ConsoleKey.P, ConsoleModifiers.Control, '\u0010'),
            () => new TuiMessage.CycleView(-1)),
        new KeyBinding(
            new KeyChord(ConsoleKey.Z, ConsoleModifiers.None, 'z'),
            () => new TuiMessage.ToggleMaximize()),
        new KeyBinding(
            new KeyChord(ConsoleKey.Oem2, ConsoleModifiers.None, '/'),
            () => new TuiMessage.FocusSearchRequested()),
        new KeyBinding(
            new KeyChord(ConsoleKey.T, ConsoleModifiers.None, 't'),
            () => new TuiMessage.OpenTreeRequested()),
        new KeyBinding(
            new KeyChord(ConsoleKey.E, ConsoleModifiers.None, 'e'),
            () => new TuiMessage.EditRequested()),
        new KeyBinding(
            new KeyChord(ConsoleKey.X, ConsoleModifiers.None, 'x'),
            () => new TuiMessage.CloseOrReopenRequested()),
        new KeyBinding(
            new KeyChord(ConsoleKey.X, ConsoleModifiers.Shift, 'X'),
            () => new TuiMessage.DeleteRequested()),
        new KeyBinding(
            new KeyChord(ConsoleKey.Escape, ConsoleModifiers.None, ''),
            () => new TuiMessage.Back())
    ]);
}
