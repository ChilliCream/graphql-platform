using ChilliCream.Nitro.CommandLine.Tui.Input;

namespace ChilliCream.Nitro.CommandLine.Tui.Shell;

/// <summary>
/// Resolves bracket keys for relative tab switching and Shift plus a tab
/// mnemonic for direct selection.
/// </summary>
internal static class TabSwitchKeys
{
    /// <summary>
    /// The tab-switching hint shown after other hints when multiple tabs are hosted
    /// and input is not captured.
    /// </summary>
    public static readonly KeyHint Hint = new("[ ] shift+letter", "tab");

    /// <summary>
    /// Resolves <paramref name="chord"/> to the tab-index delta it requests,
    /// or <see langword="null"/> when it is not a tab-switch chord.
    /// </summary>
    public static int? Resolve(KeyChord chord)
    {
        if (chord.Modifiers != ConsoleModifiers.None)
        {
            return null;
        }

        return chord.KeyChar switch
        {
            '[' => -1,
            ']' => 1,
            _ => null
        };
    }

    /// <summary>
    /// Returns the first tab whose mnemonic matches a Shift-only chord, ignoring
    /// character case, or null when none matches.
    /// </summary>
    public static int? ResolveMnemonic(KeyChord chord, IReadOnlyList<TuiTab> tabs)
    {
        if (chord.Modifiers != ConsoleModifiers.Shift)
        {
            return null;
        }

        for (var i = 0; i < tabs.Count; i++)
        {
            if (char.ToUpperInvariant(tabs[i].Mnemonic) == char.ToUpperInvariant(chord.KeyChar))
            {
                return i;
            }
        }

        return null;
    }
}
