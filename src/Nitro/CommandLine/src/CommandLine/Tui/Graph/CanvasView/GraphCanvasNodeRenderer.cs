using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Tui.Graph.Layout;
using ChilliCream.Nitro.CommandLine.Tui.Graph.Render;
using ChilliCream.Nitro.CommandLine.Tui.Theming;
using ChilliCream.Nitro.CommandLine.Tui.Widgets;
using Spectre.Console;

namespace ChilliCream.Nitro.CommandLine.Tui.Graph.CanvasView;

/// <summary>
/// Measures and paints graph task nodes onto a canvas buffer.
/// </summary>
internal static class GraphCanvasNodeRenderer
{
    private const int ContentWidth = 28;
    private const int TitleWidth = 26;
    private const int BoxWidth = ContentWidth + 2;
    private const int BoxHeight = 4;

    public static GraphNodeSize Measure(GraphNode node, bool compact)
    {
        ArgumentNullException.ThrowIfNull(node);

        return new GraphNodeSize(BoxWidth, compact ? 1 : BoxHeight);
    }

    public static void Render(
        CellBuffer buffer,
        GraphLayoutNode layoutNode,
        GraphNode node,
        bool compact,
        bool selected)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentNullException.ThrowIfNull(node);

        if (compact)
        {
            RenderCompact(buffer, layoutNode, node, selected);
            return;
        }

        var baseStyle = GetBaseStyle(node, selected);
        var horizontal = new string('─', Math.Max(0, layoutNode.Width - 2));
        Write(buffer, layoutNode.X, layoutNode.Y, $"┌{horizontal}┐", baseStyle);
        Write(buffer, layoutNode.X, layoutNode.Y + layoutNode.Height - 1, $"└{horizontal}┘", baseStyle);
        Write(buffer, layoutNode.X, layoutNode.Y + 1, "│", baseStyle);
        Write(buffer, layoutNode.X + layoutNode.Width - 1, layoutNode.Y + 1, "│", baseStyle);
        Write(buffer, layoutNode.X, layoutNode.Y + 2, "│", baseStyle);
        Write(buffer, layoutNode.X + layoutNode.Width - 1, layoutNode.Y + 2, "│", baseStyle);

        RenderMetadata(buffer, layoutNode.X + 1, layoutNode.Y + 1, layoutNode.Width - 2, node, selected);
        Write(
            buffer,
            layoutNode.X + 1,
            layoutNode.Y + 2,
            Truncate(GetTitle(node), Math.Min(TitleWidth, layoutNode.Width - 2))
                .PadRight(Math.Max(0, layoutNode.Width - 2)),
            baseStyle);
    }

    private static void RenderCompact(CellBuffer buffer, GraphLayoutNode layoutNode, GraphNode node, bool selected)
    {
        var width = layoutNode.Width;
        var baseStyle = GetBaseStyle(node, selected);
        var prefix = $"{TaskGlyphs.Status(node.Status)} [{TaskGlyphs.TypeCode(node.Type)}] {node.Id} ";
        var text = Truncate(prefix + GetTitle(node), width).PadRight(Math.Max(0, width));
        Write(buffer, layoutNode.X, layoutNode.Y, text, baseStyle);

        var statusStyle = Compose(ThemeTokens.GetStyle($"status.glyph.{node.Status}"), node, selected);
        var typeStyle = Compose(ThemeTokens.GetStyle($"badge.type.{node.Type}"), node, selected);
        Write(buffer, layoutNode.X, layoutNode.Y, TaskGlyphs.Status(node.Status), statusStyle);
        Write(buffer, layoutNode.X + 2, layoutNode.Y, $"[{TaskGlyphs.TypeCode(node.Type)}]", typeStyle);
    }

    private static void RenderMetadata(CellBuffer buffer, int x, int y, int width, GraphNode node, bool selected)
    {
        var baseStyle = GetBaseStyle(node, selected);
        var status = TaskGlyphs.Status(node.Status);
        var type = $"[{TaskGlyphs.TypeCode(node.Type)}]";
        var prefixWidth = status.Length + type.Length + 2;
        var identity = node.Status == TaskStates.InProgress && !string.IsNullOrWhiteSpace(node.Assignee)
            ? $"{node.Id} @{node.Assignee}"
            : node.Id;

        Write(buffer, x, y, new string(' ', Math.Max(0, width)), baseStyle);
        Write(buffer, x, y, status, Compose(ThemeTokens.GetStyle($"status.glyph.{node.Status}"), node, selected));
        Write(buffer, x + 2, y, type, Compose(ThemeTokens.GetStyle($"badge.type.{node.Type}"), node, selected));
        Write(
            buffer,
            x + prefixWidth,
            y,
            Truncate(identity, Math.Max(0, width - prefixWidth)),
            Compose(ThemeTokens.GetStyle("footer.key"), node, selected));
    }

    private static string GetTitle(GraphNode node)
        => node.HiddenChildCount > 0 ? $"[epic +{node.HiddenChildCount}]" : node.Title;

    private static Style GetBaseStyle(GraphNode node, bool selected)
        => Compose(Style.Plain, node, selected);

    private static Style Compose(Style style, GraphNode node, bool selected)
    {
        var selection = selected ? ThemeTokens.GetStyle("selection.highlight") : Style.Plain;
        var decoration = style.Decoration | selection.Decoration;

        if (TaskStates.IsTerminal(node.Status))
        {
            decoration |= Decoration.Dim;
        }

        return new Style(style.Foreground, selection.Background, decoration);
    }

    private static void Write(CellBuffer buffer, int x, int y, string value, Style style)
    {
        for (var index = 0; index < value.Length; index++)
        {
            buffer.Set(x + index, y, value[index], style);
        }
    }

    private static string Truncate(string value, int width)
    {
        if (width <= 0)
        {
            return string.Empty;
        }

        if (value.Length <= width)
        {
            return value;
        }

        return width == 1 ? "…" : value[..(width - 1)] + "…";
    }
}
