using ChilliCream.Nitro.CommandLine.Tui.Input;

namespace ChilliCream.Nitro.CommandLine.Tui.Shell;

/// <summary>
/// The key bindings a tabbed <see cref="TuiShell"/> uses to switch its
/// active tab: <c>[</c> for the previous tab and <c>]</c> for the next,
/// wrapping around at either end. Neither chord collides with
/// <see cref="KeyMap.CreateDefaultGlobal"/> or <c>MailKeyMap.CreateDefault</c>.
/// Checked ahead of the active tab's own dispatch, and never reached while
/// any shell-level or mode-level overlay is capturing input.
/// </summary>
internal static class TabSwitchKeys
{
    private static readonly KeyChord PreviousChord = new(ConsoleKey.Oem4, ConsoleModifiers.None, '[');
    private static readonly KeyChord NextChord = new(ConsoleKey.Oem6, ConsoleModifiers.None, ']');

    /// <summary>
    /// The footer hint for tab switching, appended after every other hint
    /// while more than one tab is hosted.
    /// </summary>
    public static readonly KeyHint Hint = new("[ ]", "tab");

    /// <summary>
    /// Resolves <paramref name="chord"/> to the tab-index delta it requests,
    /// or <see langword="null"/> when it is not a tab-switch chord.
    /// </summary>
    public static int? Resolve(KeyChord chord)
    {
        if (chord == PreviousChord)
        {
            return -1;
        }

        if (chord == NextChord)
        {
            return 1;
        }

        return null;
    }
}
