using ChilliCream.Nitro.CommandLine.Tui.Theming;

namespace ChilliCream.Nitro.CommandLine.Tui.Details;

/// <summary>
/// Renders one long-text detail section, Description, Design, Acceptance
/// criteria, or Notes, as a rounded-border box with the section name as the
/// border title and a padded interior, matching the bordered field sections
/// the task edit form renders. Produces a flat list of plain-text display
/// rows, one per line, so the body's viewport can scroll over the box the
/// same way it scrolls over any other section.
/// </summary>
internal static class TaskDetailSectionBox
{
    /// <summary>
    /// Border and padding columns the box spends on either side of its
    /// content: the rounded border and one padding column, each side.
    /// </summary>
    private const int ChromeWidth = 4;

    /// <summary>
    /// Builds the box for <paramref name="text"/> under <paramref name="title"/>,
    /// at most <paramref name="width"/> display columns wide. Returns an empty
    /// list when <paramref name="text"/> is empty, so the caller omits the
    /// section entirely.
    /// </summary>
    public static IReadOnlyList<TaskDetailBodyLine> Render(string title, string text, int width)
    {
        if (string.IsNullOrEmpty(text))
        {
            return [];
        }

        var boxWidth = Math.Max(width, ChromeWidth + 1);
        var interiorWidth = boxWidth - ChromeWidth;
        var contentLines = TaskDetailSections.WrapText(text, interiorWidth);

        var lines = new List<TaskDetailBodyLine>(contentLines.Count + 2) { TopBorder(title, boxWidth) };

        foreach (var line in contentLines)
        {
            lines.Add(new TaskDetailBodyLine($"│ {line.PadRight(interiorWidth)} │", IsMarkup: false));
        }

        lines.Add(new TaskDetailBodyLine($"╰{new string('─', Math.Max(0, boxWidth - 2))}╯", IsMarkup: true));
        return lines;
    }

    private static TaskDetailBodyLine TopBorder(string title, int width)
    {
        var borderStyle = ThemeTokens.GetStyle("detail.section.border").ToMarkup();
        var titleStyle = ThemeTokens.GetStyle("detail.section.header").ToMarkup();
        var escapedTitle = Markup.Escape(title);
        var fill = Math.Max(0, width - 3 - title.Length);

        var content =
            Styled(borderStyle, "╭─")
            + (titleStyle.Length == 0 ? $"[bold]{escapedTitle}[/]" : Styled(titleStyle, escapedTitle))
            + Styled(borderStyle, $"{new string('─', fill)}╮");

        return new TaskDetailBodyLine(content, IsMarkup: true);
    }

    private static string Styled(string style, string content) => style.Length == 0 ? content : $"[{style}]{content}[/]";
}
