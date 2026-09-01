using ChilliCream.Nitro.CommandLine.Tui.Theming;
using Spectre.Console;

namespace ChilliCream.Nitro.CommandLine.Tui.Graph.Render;

internal static class GraphEdgeStyles
{
    private const string LineToken = "board.column.border";

    public static Style Line => ThemeTokens.GetStyle(LineToken);

    public static Style Dim(Style style)
        => new(style.Foreground, style.Background, style.Decoration | Decoration.Dim);

    public static Style Selected()
    {
        var selection = ThemeTokens.GetStyle("selection.highlight");

        return new Style(
            Line.Foreground,
            selection.Background,
            Line.Decoration | selection.Decoration);
    }
}
