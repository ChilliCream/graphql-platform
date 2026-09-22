using System.Diagnostics.CodeAnalysis;

namespace ChilliCream.Nitro.CommandLine.Tui.Input;

/// <summary>
/// Resolves key chords with a fallback for matching printable characters
/// and modifiers when no exact chord matches.
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
    /// Creates the default global bindings for navigation, task actions, mode
    /// selection, and quit requests.
    /// </summary>
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
            new KeyChord(ConsoleKey.G, ConsoleModifiers.Shift, 'G'),
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
