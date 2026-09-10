namespace ChilliCream.Nitro.CommandLine.Tui.Input;

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
