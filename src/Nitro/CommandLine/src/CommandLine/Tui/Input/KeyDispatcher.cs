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
    /// The global key table every dispatch falls back to.
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

    /// <summary>
    /// Returns context hints followed by global hints that are neither suppressed
    /// nor already included. Context hints retain their original order and duplicates.
    /// </summary>
    public IReadOnlyList<KeyHint> CombineHints(
        IReadOnlyList<KeyHint> contextHints, IReadOnlyCollection<KeyHint> suppressedGlobalHints)
    {
        ArgumentNullException.ThrowIfNull(contextHints);
        ArgumentNullException.ThrowIfNull(suppressedGlobalHints);

        var globalHints = _globalKeyMap.Hints;

        if (globalHints.Count == 0)
        {
            return contextHints;
        }

        if (contextHints.Count == 0 && suppressedGlobalHints.Count == 0)
        {
            return globalHints;
        }

        var seen = new HashSet<KeyHint>(contextHints);
        var combined = new List<KeyHint>(contextHints.Count + globalHints.Count);
        combined.AddRange(contextHints);

        foreach (var hint in globalHints)
        {
            if (suppressedGlobalHints.Count > 0 && suppressedGlobalHints.Contains(hint))
            {
                continue;
            }

            if (seen.Add(hint))
            {
                combined.Add(hint);
            }
        }

        return combined;
    }
}
