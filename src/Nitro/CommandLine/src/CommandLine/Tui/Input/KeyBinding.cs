namespace ChilliCream.Nitro.CommandLine.Tui.Input;

/// <summary>
/// A key combination looked up in a <see cref="KeyMap"/>: the console key, its
/// modifiers, and the character it produced.
/// </summary>
internal readonly record struct KeyChord(ConsoleKey Key, ConsoleModifiers Modifiers, char KeyChar)
{
    /// <summary>
    /// Builds the <see cref="KeyChord"/> for a raw <see cref="ConsoleKeyInfo"/>.
    /// </summary>
    public static KeyChord From(ConsoleKeyInfo keyInfo) =>
        new(keyInfo.Key, keyInfo.Modifiers, keyInfo.KeyChar);
}

/// <summary>
/// The footer display metadata for a <see cref="KeyBinding"/>: the short key
/// label shown dimmed (for example <c>j/k</c> or <c>ctrl+e</c>) and the
/// action label shown next to it (for example <c>move</c> or <c>edit</c>).
/// </summary>
internal readonly record struct KeyHint(string Key, string Action);

/// <summary>
/// A binding from a <see cref="KeyChord"/> to a factory that creates the
/// <see cref="TuiMessage"/> it produces. <see cref="Hint"/> is the binding's
/// footer display metadata; a null <see cref="Hint"/> hides the binding from
/// the footer while leaving it fully functional. Bindings that are paired
/// (for example j/k, or a printable key with its arrow-key equivalent) carry
/// their <see cref="Hint"/> on a single representative binding so the pair
/// renders as one footer entry.
/// </summary>
internal sealed record KeyBinding(KeyChord Chord, Func<TuiMessage> CreateMessage, KeyHint? Hint = null);
