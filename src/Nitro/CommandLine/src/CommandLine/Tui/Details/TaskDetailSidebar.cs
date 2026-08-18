using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Tui.Widgets;

namespace ChilliCream.Nitro.CommandLine.Tui.Details;

/// <summary>
/// Builds the markup lines of a task detail view's metadata sidebar: status,
/// priority, type, assignee, due, deferred-until, estimate, created,
/// updated, closed, labels, and blocked-by, in that order. Fields with no
/// value are omitted.
/// </summary>
internal static class TaskDetailSidebar
{
    public static IReadOnlyList<string> Build(
        TaskItem task,
        IReadOnlyList<string> labels,
        IReadOnlyList<string> blockedBy)
    {
        var lines = new List<string>
        {
            $"{TaskGlyphs.StatusMarkup(task.Status)} {Markup.Escape(task.Status)}",
            $"Priority: {Markup.Escape(TaskPriorities.Format(task.Priority))}",
            $"Type: {Markup.Escape(task.Type)}"
        };

        if (!string.IsNullOrEmpty(task.Assignee))
        {
            lines.Add($"Assignee: {Markup.Escape(task.Assignee)}");
        }

        if (task.DueAt is { } dueAt)
        {
            lines.Add($"Due: {Markup.Escape(TaskDates.Format(dueAt))}");
        }

        if (task.DeferUntil is { } deferUntil)
        {
            lines.Add($"Deferred until: {Markup.Escape(TaskDates.Format(deferUntil))}");
        }

        if (task.EstimatedMinutes is { } estimatedMinutes)
        {
            lines.Add($"Estimate: {estimatedMinutes}m");
        }

        lines.Add($"Created: {Markup.Escape(TaskDates.Format(task.CreatedAt))} by {Markup.Escape(task.CreatedBy)}");
        lines.Add($"Updated: {Markup.Escape(TaskDates.Format(task.UpdatedAt))}");

        if (task.Status == TaskStates.Closed && task.ClosedAt is { } closedAt)
        {
            var closedLine = $"Closed: {Markup.Escape(TaskDates.Format(closedAt))}";

            if (!string.IsNullOrEmpty(task.CloseReason))
            {
                closedLine += $" ({Markup.Escape(task.CloseReason)})";
            }

            lines.Add(closedLine);
        }

        if (labels.Count > 0)
        {
            lines.Add($"Labels: {Markup.Escape(string.Join(", ", labels))}");
        }

        if (blockedBy.Count > 0)
        {
            lines.Add($"Blocked by: {Markup.Escape(string.Join(", ", blockedBy))}");
        }

        return lines;
    }
}
