using System.Diagnostics.CodeAnalysis;

namespace ChilliCream.Nitro.CommandLine.Tui.Input;

/// <summary>
/// A table of key bindings, looked up primarily by key chord. Bindings whose key char
/// is a printable character also resolve when the console key differs but the
/// modifiers and produced character match, so a binding is reachable regardless of
/// which <see cref="ConsoleKey"/> a terminal reports for that character.
/// </summary>
internal sealed class KeyMap
{
    private readonly Dictionary<KeyChord, Func<TuiMessage>> _bindings;
    private readonly Dictionary<(char KeyChar, ConsoleModifiers Modifiers), Func<TuiMessage>> _charFallback;

    public KeyMap(IEnumerable<KeyBinding> bindings)
    {
        var bindingList = bindings as IReadOnlyCollection<KeyBinding> ?? bindings.ToList();
        _bindings = bindingList.ToDictionary(b => b.Chord, b => b.CreateMessage);

        _charFallback = new Dictionary<(char, ConsoleModifiers), Func<TuiMessage>>();
        foreach (var binding in bindingList)
        {
            if (IsPrintable(binding.Chord.KeyChar))
            {
                _charFallback.TryAdd((binding.Chord.KeyChar, binding.Chord.Modifiers), binding.CreateMessage);
            }
        }

        Hints = bindingList
            .Where(b => b.Hint is not null)
            .Select(b => b.Hint!.Value)
            .ToList();
    }

    /// <summary>
    /// The footer hints carried by this map's bindings, in the order they were
    /// given to the constructor. Bindings without a <see cref="KeyBinding.Hint"/>
    /// are omitted.
    /// </summary>
    public IReadOnlyList<KeyHint> Hints { get; }

    /// <summary>
    /// Resolves the <see cref="TuiMessage"/> bound to <paramref name="chord"/>, falling
    /// back to a char-based match for printable characters when no binding matches the
    /// exact chord. Returns <see langword="false"/> when <paramref name="chord"/> is
    /// unbound in both.
    /// </summary>
    public bool TryResolve(KeyChord chord, [NotNullWhen(true)] out TuiMessage? message)
    {
        if (_bindings.TryGetValue(chord, out var createMessage))
        {
            message = createMessage();
            return true;
        }

        if (IsPrintable(chord.KeyChar)
            && _charFallback.TryGetValue((chord.KeyChar, chord.Modifiers), out var fallbackMessage))
        {
            message = fallbackMessage();
            return true;
        }

        message = null;
        return false;
    }

    private static bool IsPrintable(char keyChar) => keyChar is not '\0' && !char.IsControl(keyChar);

    /// <summary>
    /// The hardcoded global key bindings: vim-style navigation (j/k/h/l and arrow
    /// keys, g and End for edges), Enter to open, r to refresh, y to copy the selected id,
    /// q and Ctrl+C to request quit, Ctrl+N/Ctrl+P to cycle views, z to toggle the
    /// maximized layout, / to jump into search, t to open the dependency tree on
    /// the current selection, e to edit it, x to close or reopen it, X to delete
    /// it, s to open the status quick picker on it, p to open the priority quick
    /// picker on it, c to open the task create form (as a child of the current
    /// selection when there is one), C to open it preset to the epic type, and
    /// Escape to leave the active mode.
    /// </summary>
    /// <remarks>
    /// Only a curated subset carries a <see cref="KeyBinding.Hint"/>, so the
    /// footer stays a short-help bar rather than a full binding dump: the four
    /// direction keys collapse into one <c>hjkl move</c> hint (their arrow-key
    /// equivalents stay unhinted), and the rarer or currently inert gestures
    /// (g, End, /, t, x, X, s, p, c, C, Ctrl+C, and Ctrl+N/P, which cycles
    /// between views but has nothing to cycle to until a second board view
    /// exists) stay hidden from the footer while remaining fully bound. Quit's
    /// binding is declared last so it is also the last global hint, per the
    /// footer's display order.
    /// </remarks>
    public static KeyMap CreateDefaultGlobal() => new(
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
            new KeyChord(ConsoleKey.G, ConsoleModifiers.None, 'g'),
            () => new TuiMessage.MoveToEdge(EdgeTarget.Top)),
        new KeyBinding(
            new KeyChord(ConsoleKey.End, ConsoleModifiers.None, '\0'),
            () => new TuiMessage.MoveToEdge(EdgeTarget.Bottom)),
        new KeyBinding(
            new KeyChord(ConsoleKey.Enter, ConsoleModifiers.None, '\r'),
            () => new TuiMessage.OpenSelected(),
            new KeyHint("enter", "open")),
        new KeyBinding(
            new KeyChord(ConsoleKey.R, ConsoleModifiers.None, 'r'),
            () => new TuiMessage.RefreshRequested(),
            new KeyHint("r", "refresh")),
        new KeyBinding(
            new KeyChord(ConsoleKey.Y, ConsoleModifiers.None, 'y'),
            () => new TuiMessage.CopySelectedId(),
            new KeyHint("y", "copy id")),
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
            () => new TuiMessage.ToggleMaximize(),
            new KeyHint("z", "zoom")),
        new KeyBinding(
            new KeyChord(ConsoleKey.Oem2, ConsoleModifiers.None, '/'),
            () => new TuiMessage.FocusSearchRequested()),
        new KeyBinding(
            new KeyChord(ConsoleKey.T, ConsoleModifiers.None, 't'),
            () => new TuiMessage.OpenTreeRequested()),
        new KeyBinding(
            new KeyChord(ConsoleKey.E, ConsoleModifiers.None, 'e'),
            () => new TuiMessage.EditRequested(),
            new KeyHint("e", "edit")),
        new KeyBinding(
            new KeyChord(ConsoleKey.X, ConsoleModifiers.None, 'x'),
            () => new TuiMessage.CloseOrReopenRequested()),
        new KeyBinding(
            new KeyChord(ConsoleKey.X, ConsoleModifiers.Shift, 'X'),
            () => new TuiMessage.DeleteRequested()),
        new KeyBinding(
            new KeyChord(ConsoleKey.S, ConsoleModifiers.None, 's'),
            () => new TuiMessage.StatusPickerRequested()),
        new KeyBinding(
            new KeyChord(ConsoleKey.P, ConsoleModifiers.None, 'p'),
            () => new TuiMessage.PriorityPickerRequested()),
        new KeyBinding(
            new KeyChord(ConsoleKey.C, ConsoleModifiers.None, 'c'),
            () => new TuiMessage.CreateTaskRequested()),
        new KeyBinding(
            new KeyChord(ConsoleKey.C, ConsoleModifiers.Shift, 'C'),
            () => new TuiMessage.CreateEpicRequested()),
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
