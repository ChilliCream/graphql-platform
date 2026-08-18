namespace ChilliCream.Nitro.CommandLine.Tui.Details;

/// <summary>
/// One rendered line of a task detail body. <see cref="IsMarkup"/>
/// distinguishes dependency and blocks rows, which already carry Spectre
/// markup, from plain-text section lines that still need <c>Markup.Escape</c>
/// before display.
/// </summary>
internal readonly record struct TaskDetailBodyLine(string Content, bool IsMarkup);

/// <summary>
/// Composes a task detail body's sections, in the order Description, Design,
/// Acceptance criteria, Notes, Dependencies, Blocks, Comments, with a blank
/// line between consecutive non-empty sections. Empty sections are omitted
/// entirely, including their header.
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
            Plain(TaskDetailSections.BuildTextSection("Description:", task.Description, width)),
            Plain(TaskDetailSections.BuildTextSection("Design:", task.Design, width)),
            Plain(TaskDetailSections.BuildTextSection("Acceptance criteria:", task.AcceptanceCriteria, width)),
            Plain(TaskDetailSections.BuildTextSection("Notes:", task.Notes, width)),
            BuildRowSection("Dependencies:", dependencyRows, model.SelectedRowIndex, width, focused),
            BuildRowSection("Blocks:", blockRows, model.SelectedRowIndex, width, focused),
            Plain(TaskDetailSections.BuildCommentsSection(model.Comments, width))
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

    private static IReadOnlyList<TaskDetailBodyLine> Plain(IReadOnlyList<string> lines)
    {
        var result = new List<TaskDetailBodyLine>(lines.Count);

        foreach (var line in lines)
        {
            result.Add(new TaskDetailBodyLine(line, false));
        }

        return result;
    }

    private static IReadOnlyList<TaskDetailBodyLine> BuildRowSection(
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

        var lines = new List<TaskDetailBodyLine>(rows.Count + 1) { new(header, false) };

        foreach (var row in rows)
        {
            lines.Add(new TaskDetailBodyLine(
                TaskDetailRowRenderer.Render(row, selected: focused && row.Index == selectedIndex, width),
                IsMarkup: true));
        }

        return lines;
    }
}
