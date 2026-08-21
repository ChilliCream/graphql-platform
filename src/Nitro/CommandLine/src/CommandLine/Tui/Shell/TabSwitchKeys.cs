using ChilliCream.Nitro.CommandLine.Tui.Input;

namespace ChilliCream.Nitro.CommandLine.Tui.Shell;

/// <summary>
/// The key bindings a tabbed <see cref="TuiShell"/> uses to switch its
/// active tab: <c>[</c> for the previous tab, <c>]</c> for the next
/// (wrapping around at either end), and <c>Shift+&lt;letter&gt;</c> to jump
/// straight to whichever hosted tab's <see cref="TuiTab.Mnemonic"/> matches.
/// None of these collide with <see cref="KeyMap.CreateDefaultGlobal"/> or
/// <c>MailKeyMap.CreateDefault</c> (neither binds Shift+T, Shift+M, or
/// Shift+A, the mnemonics <c>AgentTuiLauncher</c> hosts today; a future
/// mnemonic must be checked against both tables the same way before it is
/// added). Checked ahead of the active tab's own dispatch, and never reached
/// while any shell-level or mode-level overlay is capturing input.
/// </summary>
internal static class TabSwitchKeys
{
    /// <summary>
    /// The footer hint for tab switching, appended after every other hint
    /// while more than one tab is hosted.
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
    /// Resolves <paramref name="chord"/> to the index of whichever hosted
    /// <paramref name="tabs"/> entry has it as its <see cref="TuiTab.Mnemonic"/>,
    /// or <see langword="null"/> when <paramref name="chord"/> is not an
    /// unmodified-by-anything-else Shift+letter chord or no hosted tab
    /// claims that letter.
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
