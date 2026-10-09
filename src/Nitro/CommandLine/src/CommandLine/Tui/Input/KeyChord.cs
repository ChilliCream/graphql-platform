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
