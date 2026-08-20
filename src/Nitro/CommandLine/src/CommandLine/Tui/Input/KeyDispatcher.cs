namespace ChilliCream.Nitro.CommandLine.Tui.Input;

/// <summary>
/// Resolves raw console key input into <see cref="TuiMessage"/> intents.
/// </summary>
internal sealed class KeyDispatcher
{
    private readonly KeyMap _globalKeyMap;

    public KeyDispatcher(KeyMap globalKeyMap)
    {
        _globalKeyMap = globalKeyMap ?? throw new ArgumentNullException(nameof(globalKeyMap));
    }

    /// <summary>
    /// The global key table every dispatch falls back to. Exposed so the
    /// footer can append its hints after whichever context-specific hints
    /// are active.
    /// </summary>
    public KeyMap GlobalKeyMap => _globalKeyMap;

    /// <summary>
    /// Resolves <paramref name="keyInfo"/> into a <see cref="TuiMessage"/>, checking
    /// <paramref name="modeKeyMap"/> first, then the global key table. Returns
    /// <see langword="null"/> when the key is unbound in both.
    /// </summary>
    public TuiMessage? Dispatch(ConsoleKeyInfo keyInfo, KeyMap? modeKeyMap)
    {
        var chord = KeyChord.From(keyInfo);

        if (modeKeyMap is not null && modeKeyMap.TryResolve(chord, out var modeMessage))
        {
            return modeMessage;
        }

        return _globalKeyMap.TryResolve(chord, out var globalMessage) ? globalMessage : null;
    }
}
