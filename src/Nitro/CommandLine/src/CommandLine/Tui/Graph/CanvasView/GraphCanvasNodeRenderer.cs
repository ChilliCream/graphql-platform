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
        bool selected,
        bool matched)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentNullException.ThrowIfNull(node);

        if (compact)
        {
            RenderCompact(buffer, layoutNode, node, selected, matched);
            return;
        }

        var baseStyle = GetBaseStyle(node, selected, matched);
        var horizontal = new string('─', Math.Max(0, layoutNode.Width - 2));
        WriteSpan(buffer, layoutNode.X, layoutNode.Y, $"┌{horizontal}┐", baseStyle, layoutNode.Width);
        WriteSpan(buffer, layoutNode.X, layoutNode.Y + layoutNode.Height - 1, $"└{horizontal}┘", baseStyle, layoutNode.Width);
        WriteSpan(buffer, layoutNode.X, layoutNode.Y + 1, "│", baseStyle, 1);
        WriteSpan(buffer, layoutNode.X + layoutNode.Width - 1, layoutNode.Y + 1, "│", baseStyle, 1);
        WriteSpan(buffer, layoutNode.X, layoutNode.Y + 2, "│", baseStyle, 1);
        WriteSpan(buffer, layoutNode.X + layoutNode.Width - 1, layoutNode.Y + 2, "│", baseStyle, 1);

        RenderMetadata(buffer, layoutNode.X + 1, layoutNode.Y + 1, layoutNode.Width - 2, node, selected, matched, appendTitle: false);
        var contentWidth = Math.Max(0, layoutNode.Width - 2);
        var title = GraphCanvasText.PadRight(
            GraphCanvasText.Truncate(GetTitle(node), Math.Min(TitleWidth, contentWidth)),
            contentWidth);
        WriteSpan(
            buffer,
            layoutNode.X + 1,
            layoutNode.Y + 2,
            title,
            baseStyle,
            contentWidth);
    }

    private static void RenderCompact(CellBuffer buffer, GraphLayoutNode layoutNode, GraphNode node, bool selected, bool matched)
    {
        var width = layoutNode.Width;
        RenderMetadata(buffer, layoutNode.X, layoutNode.Y, width, node, selected, matched, appendTitle: true);
    }

    private static void RenderMetadata(
        CellBuffer buffer,
        int x,
        int y,
        int width,
        GraphNode node,
        bool selected,
        bool matched,
        bool appendTitle)
    {
        width = Math.Max(0, width);
        var baseStyle = GetBaseStyle(node, selected, matched);
        var status = TaskGlyphs.Status(node.Status);
        var type = $"[{TaskGlyphs.TypeCode(node.Type)}]";
        var identity = node.Status == TaskStates.InProgress && !string.IsNullOrWhiteSpace(node.Assignee)
            ? $"{node.Id} @{node.Assignee}"
            : node.Id;
        var end = x + width;
        var cursor = x;

        WriteSpan(buffer, cursor, y, new string(' ', width), baseStyle, width);
        cursor = WriteSpan(
            buffer,
            cursor,
            y,
            status,
            Compose(ThemeTokens.GetStyle($"status.glyph.{node.Status}"), node, selected, matched),
            end - cursor);
        cursor = WriteSpan(buffer, cursor, y, " ", baseStyle, end - cursor);
        cursor = WriteSpan(
            buffer,
            cursor,
            y,
            type,
            Compose(ThemeTokens.GetStyle($"badge.type.{node.Type}"), node, selected, matched),
            end - cursor);
        cursor = WriteSpan(buffer, cursor, y, " ", baseStyle, end - cursor);
        cursor = WriteSpan(
            buffer,
            cursor,
            y,
            identity,
            Compose(ThemeTokens.GetStyle("footer.key"), node, selected, matched, dim: true),
            end - cursor);

        if (appendTitle && cursor < end)
        {
            cursor = WriteSpan(buffer, cursor, y, " ", baseStyle, end - cursor);
            _ = WriteSpan(buffer, cursor, y, GetTitle(node), baseStyle, end - cursor);
        }
    }

    private static string GetTitle(GraphNode node)
        => node.HiddenChildCount > 0 ? $"[epic +{node.HiddenChildCount}]" : node.Title;

    private static Style GetBaseStyle(GraphNode node, bool selected, bool matched)
        => Compose(Style.Plain, node, selected, matched);

    private static Style Compose(Style style, GraphNode node, bool selected, bool matched, bool dim = false)
    {
        var selection = selected ? ThemeTokens.GetStyle("selection.highlight") : Style.Plain;
        var match = matched ? ThemeTokens.GetStyle("badge.type.question") : Style.Plain;
        var decoration = style.Decoration | selection.Decoration | match.Decoration;

        if (dim || TaskStates.IsTerminal(node.Status))
        {
            decoration |= Decoration.Dim;
        }

        return new Style(matched ? match.Foreground : style.Foreground, selection.Background, decoration);
    }

    private static int WriteSpan(CellBuffer buffer, int x, int y, string value, Style style, int width)
    {
        var encoded = GraphCanvasText.Truncate(value, width);
        var cursor = x;
        foreach (var character in encoded)
        {
            buffer.Set(cursor, y, character, style);
            cursor++;
        }

        return cursor;
    }
}
