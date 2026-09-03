using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Tui.Graph.Layout;
using ChilliCream.Nitro.CommandLine.Tui.Graph.Render;
using ChilliCream.Nitro.CommandLine.Tui.Theming;
using ChilliCream.Nitro.CommandLine.Tui.Widgets;

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
        bool matched,
        int containedMatchCount)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentNullException.ThrowIfNull(node);

        if (compact)
        {
            RenderCompact(buffer, layoutNode, node, selected, matched, containedMatchCount);
            return;
        }

        var baseStyle = GetBaseStyle(node, selected, matched);
        var borderStyle = Compose(GetStatusStyle(node.Status), node, selected, matched);
        var horizontal = new string('─', Math.Max(0, layoutNode.Width - 2));
        WriteSpan(buffer, layoutNode.X, layoutNode.Y, $"┌{horizontal}┐", borderStyle, layoutNode.Width);
        WriteSpan(buffer, layoutNode.X, layoutNode.Y + layoutNode.Height - 1, $"└{horizontal}┘", borderStyle, layoutNode.Width);
        WriteSpan(buffer, layoutNode.X, layoutNode.Y + 1, "│", borderStyle, 1);
        WriteSpan(buffer, layoutNode.X + layoutNode.Width - 1, layoutNode.Y + 1, "│", borderStyle, 1);
        WriteSpan(buffer, layoutNode.X, layoutNode.Y + 2, "│", borderStyle, 1);
        WriteSpan(buffer, layoutNode.X + layoutNode.Width - 1, layoutNode.Y + 2, "│", borderStyle, 1);

        RenderMetadata(
            buffer,
            layoutNode.X + 1,
            layoutNode.Y + 1,
            layoutNode.Width - 2,
            node,
            selected,
            matched,
            containedMatchCount,
            appendTitle: false);
        var contentWidth = Math.Max(0, layoutNode.Width - 2);
        var title = GraphCanvasText.PadRight(
            GraphCanvasText.Truncate(GetTitle(node, containedMatchCount), Math.Min(TitleWidth, contentWidth)),
            contentWidth);
        WriteSpan(
            buffer,
            layoutNode.X + 1,
            layoutNode.Y + 2,
            title,
            baseStyle,
            contentWidth);
    }

    private static void RenderCompact(
        CellBuffer buffer,
        GraphLayoutNode layoutNode,
        GraphNode node,
        bool selected,
        bool matched,
        int containedMatchCount)
    {
        var width = layoutNode.Width;
        RenderMetadata(
            buffer,
            layoutNode.X,
            layoutNode.Y,
            width,
            node,
            selected,
            matched,
            containedMatchCount,
            appendTitle: true);
    }

    private static void RenderMetadata(
        CellBuffer buffer,
        int x,
        int y,
        int width,
        GraphNode node,
        bool selected,
        bool matched,
        int containedMatchCount,
        bool appendTitle)
    {
        width = Math.Max(0, width);
        var baseStyle = GetBaseStyle(node, selected, matched);
        var status = TaskGlyphs.Status(node.Status);
        var statusStyle = Compose(GetStatusStyle(node.Status), node, selected, matched);
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
            statusStyle,
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
            appendTitle
                ? statusStyle
                : Compose(ThemeTokens.GetStyle("footer.key"), node, selected, matched, dim: true),
            end - cursor);

        if (appendTitle && cursor < end)
        {
            cursor = WriteSpan(buffer, cursor, y, " ", baseStyle, end - cursor);
            _ = WriteSpan(buffer, cursor, y, GetTitle(node, containedMatchCount), baseStyle, end - cursor);
        }
    }

    private static string GetTitle(GraphNode node, int containedMatchCount)
    {
        if (node.HiddenChildCount > 0)
        {
            return containedMatchCount > 0
                ? $"[epic +{node.HiddenChildCount}, hits {containedMatchCount}]"
                : $"[epic +{node.HiddenChildCount}]";
        }

        return containedMatchCount > 0 ? $"{node.Title} [hits {containedMatchCount}]" : node.Title;
    }

    private static Style GetBaseStyle(GraphNode node, bool selected, bool matched)
        => Compose(Style.Plain, node, selected, matched);

    private static Style GetStatusStyle(string status)
        => status switch
        {
            TaskStates.Blocked => ThemeTokens.GetStyle("board.column.status.blocked"),
            TaskStates.Deferred => ThemeTokens.GetStyle("board.column.status.deferred"),
            TaskStates.InProgress => ThemeTokens.GetStyle("board.column.status.inprogress"),
            TaskStates.Closed or TaskStates.Archived or TaskStates.Tombstone
                => ThemeTokens.GetStyle("board.column.status.closed"),
            _ => ThemeTokens.GetStyle("board.column.status.ready")
        };

    private static Style Compose(Style style, GraphNode node, bool selected, bool matched, bool dim = false)
    {
        var selection = selected ? ThemeTokens.GetStyle("selection.highlight") : Style.Plain;
        var match = matched ? ThemeTokens.GetStyle("badge.type.question") : Style.Plain;
        var decoration = style.Decoration | selection.Decoration | match.Decoration;

        if (dim || TaskStates.IsTerminal(node.Status))
        {
            decoration |= Decoration.Dim;
        }

        var foreground = selected
            ? selection.Foreground
            : matched
                ? match.Foreground
                : style.Foreground;
        return new Style(foreground, selection.Background, decoration);
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
