using ChilliCream.Nitro.CommandLine.Tui.Theming;

namespace ChilliCream.Nitro.CommandLine.Tui.Details;

/// <summary>
/// Renders a titled, bordered long-text section as body lines.
/// Content lines contain unescaped plain text; border lines contain markup.
/// </summary>
internal static class TaskDetailSectionBox
{
    /// <summary>
    /// Border and padding columns the box spends on either side of its
    /// content: the rounded border and one padding column, each side.
    /// </summary>
    private const int ChromeWidth = 4;

    /// <summary>
    /// Builds the box for <paramref name="text"/> under <paramref name="title"/>, at most
    /// <paramref name="width"/> display columns wide. Returns an empty list when
    /// <paramref name="text"/> is empty.
    /// </summary>
    public static IReadOnlyList<TaskDetailBodyLine> Render(string title, string text, int width)
    {
        if (string.IsNullOrEmpty(text) || width <= 0)
        {
            return [];
        }

        var boxWidth = width;
        var interiorWidth = Math.Max(0, boxWidth - ChromeWidth);
        var contentLines = interiorWidth > 0
            ? TaskDetailSections.WrapText(text, interiorWidth)
            : new[] { string.Empty };

        var lines = new List<TaskDetailBodyLine>(contentLines.Count + 2) { TopBorder(title, boxWidth) };

        foreach (var line in contentLines)
        {
            lines.Add(new TaskDetailBodyLine(ContentLine(line, boxWidth, interiorWidth), IsMarkup: false));
        }

        lines.Add(new TaskDetailBodyLine(BottomBorder(boxWidth), IsMarkup: true));
        return lines;
    }

    private static TaskDetailBodyLine TopBorder(string title, int width)
    {
        var borderStyle = ThemeTokens.GetStyle("detail.section.border").ToMarkup();
        var titleStyle = ThemeTokens.GetStyle("detail.section.header").ToMarkup();

        if (width == 1)
        {
            return new TaskDetailBodyLine(Styled(borderStyle, "╭"), IsMarkup: true);
        }

        if (width == 2)
        {
            return new TaskDetailBodyLine(Styled(borderStyle, "╭╮"), IsMarkup: true);
        }

        var truncatedTitle = DisplayWidth.Truncate(title, width - 3);
        var escapedTitle = Markup.Escape(truncatedTitle);
        var fill = Math.Max(0, width - 3 - DisplayWidth.Measure(truncatedTitle));

        var content =
            Styled(borderStyle, "╭─")
            + (titleStyle.Length == 0 ? $"[bold]{escapedTitle}[/]" : Styled(titleStyle, escapedTitle))
            + Styled(borderStyle, $"{new string('─', fill)}╮");

        return new TaskDetailBodyLine(content, IsMarkup: true);
    }

    private static string ContentLine(string line, int width, int interiorWidth) => width switch
    {
        1 => "│",
        2 => "││",
        3 => "│ │",
        _ => $"│ {DisplayWidth.PadRight(line, interiorWidth)} │"
    };

    private static string BottomBorder(int width) => width switch
    {
        1 => "╰",
        2 => "╰╯",
        _ => $"╰{new string('─', width - 2)}╯"
    };

    private static string Styled(string style, string content) => style.Length == 0 ? content : $"[{style}]{content}[/]";
}
