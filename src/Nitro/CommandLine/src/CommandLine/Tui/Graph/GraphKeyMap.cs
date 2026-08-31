using ChilliCream.Nitro.CommandLine.Tui.Input;

namespace ChilliCream.Nitro.CommandLine.Tui.Graph;

/// <summary>
/// Builds the graph projection and presentation bindings layered ahead of
/// the Graph tab's default global key table.
/// </summary>
internal static class GraphKeyMap
{
    private static readonly KeyBinding[] s_bindings =
    [
        new KeyBinding(
            new KeyChord(ConsoleKey.V, ConsoleModifiers.None, 'v'),
            () => new TuiMessage.ToggleGraphProjection(),
            new KeyHint("v", "tree/canvas")),
        new KeyBinding(
            new KeyChord(ConsoleKey.B, ConsoleModifiers.None, 'b'),
            () => new TuiMessage.ToggleGraphCompact(),
            new KeyHint("b", "boxed/compact")),
        new KeyBinding(
            new KeyChord(ConsoleKey.O, ConsoleModifiers.None, 'o'),
            () => new TuiMessage.ToggleGraphParentChild(),
            new KeyHint("o", "parent edges")),
        new KeyBinding(
            new KeyChord(ConsoleKey.D, ConsoleModifiers.None, 'd'),
            () => new TuiMessage.ToggleGraphClosed(),
            new KeyHint("d", "closed")),
        new KeyBinding(
            new KeyChord(ConsoleKey.U, ConsoleModifiers.None, 'u'),
            () => new TuiMessage.CollapseSelectedGraphEpic(),
            new KeyHint("u/i", "collapse/expand")),
        new KeyBinding(
            new KeyChord(ConsoleKey.I, ConsoleModifiers.None, 'i'),
            () => new TuiMessage.ExpandSelectedGraphEpic()),
        new KeyBinding(
            new KeyChord(ConsoleKey.OemComma, ConsoleModifiers.None, ','),
            () => new TuiMessage.CollapseAllGraphEpics(),
            new KeyHint(",/.", "collapse/expand all")),
        new KeyBinding(
            new KeyChord(ConsoleKey.OemPeriod, ConsoleModifiers.None, '.'),
            () => new TuiMessage.ExpandAllGraphEpics())
    ];

    /// <summary>
    /// The graph-specific chords, exposed for the tab collision guard.
    /// </summary>
    internal static IReadOnlyList<KeyChord> Chords { get; } =
        s_bindings.Select(t => t.Chord).ToArray();

    public static KeyMap CreateDefault() => new(s_bindings);
}
