namespace ChilliCream.Nitro.CommandLine.Tui.Input;

/// <summary>
/// The footer display metadata for a <see cref="KeyBinding"/>: the short key
/// label shown dimmed (for example <c>j/k</c> or <c>ctrl+e</c>) and the
/// action label shown next to it (for example <c>move</c> or <c>edit</c>).
/// </summary>
internal readonly record struct KeyHint(string Key, string Action);
