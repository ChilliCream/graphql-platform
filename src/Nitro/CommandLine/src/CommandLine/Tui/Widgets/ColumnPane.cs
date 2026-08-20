using ChilliCream.Nitro.CommandLine.Tui.Theming;
using Spectre.Console.Rendering;

namespace ChilliCream.Nitro.CommandLine.Tui.Widgets;

/// <summary>
/// Wraps a list of already-rendered markup lines in a rounded-border panel
/// titled "Name (count)", with border style driven by focus state.
/// </summary>
internal static class ColumnPane
{
    /// <summary>
    /// Builds the panel for one column. <paramref name="lines"/> are markup
    /// strings, one per visible row, in top-to-bottom order.
    /// </summary>
    public static Panel Render(string name, int count, IReadOnlyList<string> lines, bool focused)
    {
        IRenderable content = lines.Count == 0
            ? new Markup(string.Empty)
            : new Rows(lines.Select(line => (IRenderable)new Markup(line)));

        var borderToken = focused ? "board.column.border.focused" : "board.column.border";

        return new Panel(content)
        {
            Header = new PanelHeader($"{name} ({count})"),
            Border = BoxBorder.Rounded,
            BorderStyle = ThemeTokens.GetStyle(borderToken)
        };
    }
}
