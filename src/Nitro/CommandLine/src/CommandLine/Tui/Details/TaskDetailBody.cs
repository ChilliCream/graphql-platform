using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Tui.Theming;

namespace ChilliCream.Nitro.CommandLine.Tui.Details;

/// <summary>
/// One rendered line of a task detail body. <see cref="IsMarkup"/>
/// distinguishes section headers, section box borders, dependency rows, and
/// blocks rows, which already carry Spectre markup, from plain-text section
/// lines, including a section box's content rows, that still need
/// <c>Markup.Escape</c> before display. <see cref="IsSelectedRow"/> marks
/// the single dependency or blocks row the body's scroll position keeps
/// visible.
/// </summary>
internal readonly record struct TaskDetailBodyLine(string Content, bool IsMarkup, bool IsSelectedRow = false);

/// <summary>
/// Composes a task detail body's sections, in the order Description, Design,
/// Acceptance criteria, Notes, Dependencies, Blocks, Comments, with a blank
/// line between consecutive non-empty sections. Description, Design,
/// Acceptance criteria, and Notes each render as their own bordered box, see
/// <see cref="TaskDetailSectionBox"/>; Dependencies, Blocks, and Comments
/// stay a styled header line followed by a blank line and their rows. Empty
/// sections are omitted entirely, including their header.
/// </summary>
internal static class TaskDetailBody
{
    public static IReadOnlyList<TaskDetailBodyLine> Build(TaskDetailModel model, int width, bool focused)
    {
        ArgumentNullException.ThrowIfNull(model);

        if (model.Task is not { } task)
        {
            return [];
        }

        var dependencyRows = model.Rows.Where(r => r.Kind == TaskDetailRowKind.Dependency).ToList();
        var blockRows = model.Rows.Where(r => r.Kind == TaskDetailRowKind.Blocks).ToList();

        var sections = new List<IReadOnlyList<TaskDetailBodyLine>>
        {
            TextSection("Description", task.Description, width),
            TextSection("Design", task.Design, width),
            TextSection("Acceptance criteria", task.AcceptanceCriteria, width),
            TextSection("Notes", task.Notes, width),
            RowSection("Dependencies", dependencyRows, model.SelectedRowIndex, width, focused),
            RowSection("Blocks", blockRows, model.SelectedRowIndex, width, focused),
            CommentsSection(model.Comments, width)
        };

        var lines = new List<TaskDetailBodyLine>();
        var separatorPending = false;

        foreach (var section in sections)
        {
            if (section.Count == 0)
            {
                continue;
            }

            if (separatorPending)
            {
                lines.Add(new TaskDetailBodyLine(string.Empty, false));
            }

            lines.AddRange(section);
            separatorPending = true;
        }

        return lines;
    }

    private static IReadOnlyList<TaskDetailBodyLine> TextSection(string header, string text, int width)
        => TaskDetailSectionBox.Render(header, text, width);

    private static IReadOnlyList<TaskDetailBodyLine> CommentsSection(IReadOnlyList<TaskComment> comments, int width)
        => WithStyledHeader("Comments", PlainBody(TaskDetailSections.BuildCommentsSection(comments, width)));

    private static IReadOnlyList<TaskDetailBodyLine> RowSection(
        string header,
        IReadOnlyList<TaskDetailRow> rows,
        int selectedIndex,
        int width,
        bool focused)
    {
        if (rows.Count == 0)
        {
            return [];
        }

        var body = new List<TaskDetailBodyLine>(rows.Count);

        foreach (var row in rows)
        {
            var selected = focused && row.Index == selectedIndex;

            body.Add(new TaskDetailBodyLine(
                TaskDetailRowRenderer.Render(row, selected, width),
                IsMarkup: true,
                IsSelectedRow: selected));
        }

        return WithStyledHeader(header, body);
    }

    /// <summary>
    /// Drops a raw section's leading header line, since the caller re-adds
    /// it styled, and wraps the remaining lines as plain, unescaped body
    /// content.
    /// </summary>
    private static IReadOnlyList<TaskDetailBodyLine> PlainBody(IReadOnlyList<string> rawLines)
    {
        if (rawLines.Count == 0)
        {
            return [];
        }

        var body = new List<TaskDetailBodyLine>(rawLines.Count - 1);

        for (var i = 1; i < rawLines.Count; i++)
        {
            body.Add(new TaskDetailBodyLine(rawLines[i], false));
        }

        return body;
    }

    /// <summary>
    /// Prefixes non-empty section content with a bold header line and a
    /// blank separator line. Empty content stays empty, so the caller omits
    /// the section entirely, header included.
    /// </summary>
    private static IReadOnlyList<TaskDetailBodyLine> WithStyledHeader(string header, IReadOnlyList<TaskDetailBodyLine> body)
    {
        if (body.Count == 0)
        {
            return [];
        }

        var lines = new List<TaskDetailBodyLine>(body.Count + 2)
        {
            StyledHeader(header),
            new TaskDetailBodyLine(string.Empty, false)
        };

        lines.AddRange(body);
        return lines;
    }

    private static TaskDetailBodyLine StyledHeader(string header)
    {
        var style = ThemeTokens.GetStyle("detail.section.header").ToMarkup();
        var text = Markup.Escape(header);
        var content = style.Length == 0 ? $"[bold]{text}[/]" : $"[{style}]{text}[/]";
        return new TaskDetailBodyLine(content, IsMarkup: true);
    }
}
