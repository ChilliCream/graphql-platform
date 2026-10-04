using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Tui.Theming;
using ChilliCream.Nitro.CommandLine.Tui.Widgets;

namespace ChilliCream.Nitro.CommandLine.Tui.Details;

/// <summary>
/// Builds the markup lines of a task detail view's metadata sidebar, grouped
/// into blocks separated by a blank line: state (status, priority, type),
/// dates and people (assignee, due, deferred-until, estimate, created,
/// updated, closed), labels, and blocked-by. Fields with no value are
/// omitted, and each key is dimmed relative to its value.
/// </summary>
internal static class TaskDetailSidebar
{
    public static IReadOnlyList<string> Build(
        TaskItem task,
        IReadOnlyList<string> labels,
        IReadOnlyList<string> blockedBy)
    {
        var groups = new List<IReadOnlyList<string>>
        {
            BuildStateGroup(task),
            BuildDatesGroup(task),
            labels.Count > 0
                ? [Field("Labels", Markup.Escape(string.Join(", ", labels)))]
                : [],
            blockedBy.Count > 0
                ? [Field("Blocked by", Markup.Escape(string.Join(", ", blockedBy)))]
                : []
        };

        var lines = new List<string>();
        var separatorPending = false;

        foreach (var group in groups)
        {
            if (group.Count == 0)
            {
                continue;
            }

            if (separatorPending)
            {
                lines.Add(string.Empty);
            }

            lines.AddRange(group);
            separatorPending = true;
        }

        return lines;
    }

    private static IReadOnlyList<string> BuildStateGroup(TaskItem task)
    {
        var statusValue = Stylize(
            ThemeTokens.GetStyle($"status.glyph.{task.Status}").ToMarkup(), Markup.Escape(task.Status));
        var priorityValue = Stylize(
            ThemeTokens.GetStyle($"badge.priority.p{task.Priority}").ToMarkup(),
            Markup.Escape(TaskPriorities.Format(task.Priority)));
        var typeValue = Stylize(
            ThemeTokens.GetStyle($"badge.type.{task.Type}").ToMarkup(), Markup.Escape(task.Type));

        return
        [
            $"{TaskGlyphs.StatusMarkup(task.Status)} {statusValue}",
            Field("Priority", priorityValue),
            Field("Type", typeValue)
        ];
    }

    private static IReadOnlyList<string> BuildDatesGroup(TaskItem task)
    {
        var lines = new List<string>();

        if (!string.IsNullOrEmpty(task.Assignee))
        {
            lines.Add(Field("Assignee", Markup.Escape(task.Assignee)));
        }

        if (task.DueAt is { } dueAt)
        {
            lines.Add(Field("Due", Markup.Escape(TaskDates.Format(dueAt))));
        }

        if (task.DeferUntil is { } deferUntil)
        {
            lines.Add(Field("Deferred until", Markup.Escape(TaskDates.Format(deferUntil))));
        }

        if (task.EstimatedMinutes is { } estimatedMinutes)
        {
            lines.Add(Field("Estimate", $"{estimatedMinutes}m"));
        }

        lines.Add(Field("Created", $"{Markup.Escape(TaskDates.Format(task.CreatedAt))} by {Markup.Escape(task.CreatedBy)}"));
        lines.Add(Field("Updated", Markup.Escape(TaskDates.Format(task.UpdatedAt))));

        if (task.Status is TaskStates.Closed or TaskStates.Archived && task.ClosedAt is { } closedAt)
        {
            var closedValue = Markup.Escape(TaskDates.Format(closedAt));

            if (!string.IsNullOrEmpty(task.CloseReason))
            {
                closedValue += $" ({Markup.Escape(task.CloseReason)})";
            }

            lines.Add(Field("Closed", closedValue));
        }

        return lines;
    }

    private static string Field(string key, string value) => $"[dim]{Markup.Escape(key)}[/]: {value}";

    private static string Stylize(string styleMarkup, string content) =>
        styleMarkup.Length == 0 ? content : $"[{styleMarkup}]{content}[/]";
}
