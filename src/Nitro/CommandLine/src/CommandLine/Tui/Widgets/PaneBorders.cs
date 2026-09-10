namespace ChilliCream.Nitro.CommandLine.Tui.Widgets;

/// <summary>
/// The shell's single focus language for bordered panes: a focused pane
/// draws a heavy box with a bold header; an unfocused pane draws a rounded
/// box with a plain header, in the pane's ordinary accent color either way.
/// Terminals draw bold text with a distinct (or synthesized) font face whose
/// box-drawing glyph metrics can drift from the regular face, so focus is
/// shown by box weight, not by bolding the border itself.
/// </summary>
internal static class PaneBorders
{
    /// <summary>
    /// The border box for a pane, given whether it currently has focus.
    /// </summary>
    public static BoxBorder For(bool focused) => focused ? BoxBorder.Heavy : BoxBorder.Rounded;

    /// <summary>
    /// Wraps a panel header's already-built markup in <c>[bold]</c> when
    /// <paramref name="focused"/> is true, otherwise returns it unchanged.
    /// <para>
    /// This has to happen in the header text itself, not via a style: Spectre
    /// draws a panel's header through an internal <c>Rule</c> whose style is
    /// the panel's own <c>BorderStyle</c>, and <c>PanelHeader.SetStyle</c> is
    /// a no-op stub in this Spectre.Console version - there is no header
    /// style independent of the border's. Bolding <c>BorderStyle</c> itself
    /// to bold the header would bold the border's box-drawing glyphs too,
    /// which is the misaligned-frame bug this focus language exists to fix.
    /// </para>
    /// </summary>
    public static string HeaderText(string markupText, bool focused) =>
        focused ? $"[bold]{markupText}[/]" : markupText;
}
