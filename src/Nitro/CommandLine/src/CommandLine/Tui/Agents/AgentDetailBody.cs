using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tui.Details;
using ChilliCream.Nitro.CommandLine.Tui.Theming;
using ChilliCream.Nitro.CommandLine.Tui.Widgets;

namespace ChilliCream.Nitro.CommandLine.Tui.Agents;

/// <summary>
/// Composes an <see cref="AgentDetailModel"/>'s body lines, in the order
/// Identity, Tasks, Sent mail, with a blank line between consecutive
/// non-empty sections. Every section is a styled header line followed by a
/// blank line and its rows, reusing <see cref="TaskDetailBodyLine"/> the same
/// way <c>TaskDetailBody</c> does. Empty sections are omitted entirely,
/// including their header. All three sections render read-only: there is no
/// per-row selection or drill-in.
/// </summary>
internal static class AgentDetailBody
{
    public static IReadOnlyList<TaskDetailBodyLine> Build(AgentDetailModel model, DateTimeOffset now, int width)
    {
        ArgumentNullException.ThrowIfNull(model);

        var sections = new List<IReadOnlyList<TaskDetailBodyLine>>
        {
            IdentitySection(model.Agent),
            TasksSection(model.Tasks, width),
            SentMailSection(model.SentMail, now, width)
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

    private static IReadOnlyList<TaskDetailBodyLine> IdentitySection(AgentRecord? agent)
    {
        if (agent is null)
        {
            return [];
        }

        var body = new List<TaskDetailBodyLine>
        {
            new($"Name: {agent.Name}", false),
            new($"Role: {(agent.Role.Length == 0 ? "-" : agent.Role)}", false),
            new($"Client: {(agent.Client.Length == 0 ? "-" : agent.Client)}", false),
            new($"Implicit: {(agent.Implicit ? "yes" : "no")}", false),
            new($"Registered: {TaskDates.Format(agent.RegisteredAt)}", false),
            new($"Last seen: {TaskDates.Format(agent.LastSeenAt)}", false)
        };

        return WithStyledHeader("Identity", body);
    }

    private static IReadOnlyList<TaskDetailBodyLine> TasksSection(IReadOnlyList<TaskItem> tasks, int width)
    {
        if (tasks.Count == 0)
        {
            return [];
        }

        var body = tasks
            .Select(task => new TaskDetailBodyLine(
                TaskBadge.Render(task.Id, task.Title, task.Status, task.Priority, task.Type, selected: false, width),
                IsMarkup: true))
            .ToList();

        return WithStyledHeader("Tasks", body);
    }

    private static IReadOnlyList<TaskDetailBodyLine> SentMailSection(
        IReadOnlyList<Services.Mail.MailMessage> messages, DateTimeOffset now, int width)
    {
        if (messages.Count == 0)
        {
            return [];
        }

        var body = messages
            .Select(message => new TaskDetailBodyLine(
                AgentSentMailRowBadge.Render(message, now, width),
                IsMarkup: true))
            .ToList();

        return WithStyledHeader("Sent mail", body);
    }

    /// <summary>
    /// Prefixes non-empty section content with a bold header line and a
    /// blank separator line. Empty content stays empty, so the caller omits
    /// the section entirely, header included.
    /// </summary>
    private static IReadOnlyList<TaskDetailBodyLine> WithStyledHeader(
        string header, IReadOnlyList<TaskDetailBodyLine> body)
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
