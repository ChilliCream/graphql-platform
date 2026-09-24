using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Memory;
using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tui.Mail;
using ChilliCream.Nitro.CommandLine.Tui.Theming;

namespace ChilliCream.Nitro.CommandLine.Tui.Agents;

/// <summary>
/// The result of <see cref="AgentPopoverView.BuildLines"/>: every markup line the popover's
/// summary view shows, and the index of the line matching the current cursor.
/// </summary>
internal readonly record struct AgentPopoverBuiltLines(IReadOnlyList<string> Lines, int SelectedLineIndex);

/// <summary>
/// Pure formatting for the agent detail popover: the header block, the Mail, Tickets, and
/// Memory sections, and the row formats a section's show-more list reuses verbatim.
/// </summary>
internal static class AgentPopoverView
{
    private const string ShowMoreLabel = "› show more";
    private const string BubbleGlyph = "●";

    /// <summary>
    /// Builds every line of the popover's summary view: the header block, a blank line, and
    /// the Mail, Tickets, and Memory sections in order, each ending in a show-more row.
    /// <paramref name="selected"/> marks the row that should render highlighted, and its
    /// line index is returned so the caller can keep it in view.
    /// </summary>
    public static AgentPopoverBuiltLines BuildLines(
        AgentRow? agent,
        IReadOnlyList<MailThreadSummary> mail,
        IReadOnlyList<TaskItem> tickets,
        IReadOnlyList<MemoryParticipationEntry> memory,
        DateTimeOffset now,
        int width,
        (AgentPopoverSection Section, bool IsShowMore, int ItemIndex) selected)
    {
        var lines = new List<string>();

        if (agent is null)
        {
            lines.Add(DisplayWidth.Truncate("Agent not found.", Math.Max(0, width)));
            return new AgentPopoverBuiltLines(lines, 0);
        }

        AppendHeader(lines, agent, now);
        lines.Add(string.Empty);

        var selectedLineIndex = 0;

        AppendSection(
            lines, "Mail", AgentPopoverSection.Mail, mail.Count, "No mail yet",
            i => FormatMailRow(mail[i], now, width), selected, ref selectedLineIndex);
        lines.Add(string.Empty);

        AppendSection(
            lines, "Tickets", AgentPopoverSection.Tickets, tickets.Count, "No tickets yet",
            i => FormatTicketRow(tickets[i], width), selected, ref selectedLineIndex);
        lines.Add(string.Empty);

        AppendSection(
            lines, "Memory", AgentPopoverSection.Memory, memory.Count, "No memory yet",
            i => FormatMemoryRow(memory[i], now, width), selected, ref selectedLineIndex);

        return new AgentPopoverBuiltLines(lines, selectedLineIndex);
    }

    private static void AppendHeader(List<string> lines, AgentRow agent, DateTimeOffset now)
    {
        var state = AgentStateResolver.Resolve(agent, now);
        var stateStyle = AgentRowBadge.PresenceStyle(state).ToMarkup();
        var stateText = state switch
        {
            AgentState.Online => "Online",
            AgentState.Unreachable => "Unreachable",
            _ => "Offline"
        };

        lines.Add(FormatHeaderLine("State", Stylize(stateStyle, $"{BubbleGlyph} {stateText}")));
        lines.Add(FormatHeaderLine("Role", agent.Role.Length == 0 ? "-" : Markup.Escape(agent.Role)));
        lines.Add(FormatHeaderLine("Harness", Markup.Escape(FormatHarness(agent))));
        lines.Add(FormatHeaderLine(
            "Session id", agent.SessionId is { Length: > 0 } sessionId ? Markup.Escape(sessionId) : "-"));
        lines.Add(FormatHeaderLine("Started", Markup.Escape(FormatAge(agent.StartedAt, now))));
        lines.Add(FormatHeaderLine("Last Seen", Markup.Escape(FormatAge(agent.LastSeenAt, now))));

        if (agent.EndedAt is { } endedAt)
        {
            lines.Add(FormatHeaderLine("Ended", Markup.Escape(FormatAge(endedAt, now))));
        }
    }

    private static string FormatHarness(AgentRow agent)
    {
        if (agent.Harness is not { Length: > 0 } harness)
        {
            return "-";
        }

        return agent.HarnessVersion.Length == 0 ? harness : $"{harness} {agent.HarnessVersion}";
    }

    private static string FormatAge(DateTimeOffset value, DateTimeOffset now) => $"{MailAges.Format(value, now)} ago";

    private static string FormatHeaderLine(string label, string valueMarkup)
    {
        var labelStyle = ThemeTokens.GetStyle("agents.popover.label").ToMarkup();
        return $"{Stylize(labelStyle, Markup.Escape(label) + ":")} {valueMarkup}";
    }

    private static void AppendSection(
        List<string> lines,
        string title,
        AgentPopoverSection section,
        int itemCount,
        string emptyMessage,
        Func<int, string> formatRow,
        (AgentPopoverSection Section, bool IsShowMore, int ItemIndex) selected,
        ref int selectedLineIndex)
    {
        var titleStyle = ThemeTokens.GetStyle("agents.popover.section.title").ToMarkup();
        lines.Add(Stylize(titleStyle, Markup.Escape(title)));

        if (itemCount == 0)
        {
            var emptyStyle = ThemeTokens.GetStyle("agents.popover.row.empty").ToMarkup();
            lines.Add(Stylize(emptyStyle, Markup.Escape(emptyMessage)));
        }
        else
        {
            for (var i = 0; i < itemCount; i++)
            {
                var line = formatRow(i);

                if (selected is { IsShowMore: false } && selected.Section == section && selected.ItemIndex == i)
                {
                    selectedLineIndex = lines.Count;
                    line = Highlight(line);
                }

                lines.Add(line);
            }
        }

        var showMoreStyle = ThemeTokens.GetStyle("agents.popover.show-more").ToMarkup();
        var showMoreLine = Stylize(showMoreStyle, ShowMoreLabel);

        if (selected is { IsShowMore: true } && selected.Section == section)
        {
            selectedLineIndex = lines.Count;
            showMoreLine = Highlight(showMoreLine);
        }

        lines.Add(showMoreLine);
    }

    /// <summary>
    /// Formats one mail participation row: age, subject, last sender and recipients, and
    /// message count, bold when unread for this agent. Truncated to <paramref name="width"/>.
    /// </summary>
    public static string FormatMailRow(MailThreadSummary summary, DateTimeOffset now, int width)
    {
        var age = MailAges.Format(summary.LastMessageAt, now);
        var recipients = string.Join(", ", summary.LastRecipients);
        var content =
            $"{age}  {summary.Subject}  {summary.LastSender} -> {recipients} ({summary.MessageCount})";
        var escaped = Markup.Escape(DisplayWidth.Truncate(content, Math.Max(0, width)));

        return summary.UnreadCount is > 0
            ? Stylize(ThemeTokens.GetStyle("agents.popover.row.unread").ToMarkup(), escaped)
            : escaped;
    }

    /// <summary>
    /// Formats one ticket participation row: id, status, and title, dimmed when closed or
    /// archived. Truncated to <paramref name="width"/>.
    /// </summary>
    public static string FormatTicketRow(TaskItem task, int width)
    {
        var content = $"{task.Id}  {task.Status}  {task.Title}";
        var escaped = Markup.Escape(DisplayWidth.Truncate(content, Math.Max(0, width)));

        return task.Status is TaskStates.Closed or TaskStates.Archived
            ? Stylize(ThemeTokens.GetStyle("agents.popover.row.dimmed").ToMarkup(), escaped)
            : escaped;
    }

    /// <summary>
    /// Formats one memory participation row: kind, the type for a curated memory, age, and
    /// the body's first line. Truncated to <paramref name="width"/>.
    /// </summary>
    public static string FormatMemoryRow(MemoryParticipationEntry entry, DateTimeOffset now, int width)
    {
        var kind = entry.Kind == MemoryParticipationKind.Curated ? "curated" : "journal";
        var typeSuffix = entry.Kind == MemoryParticipationKind.Curated && entry.Type is { Length: > 0 } type
            ? $" {type}"
            : string.Empty;
        var age = MailAges.Format(entry.CreatedAt, now);
        var content = $"{kind}{typeSuffix}  {age}  {FirstLine(entry.Body)}";

        return Markup.Escape(DisplayWidth.Truncate(content, Math.Max(0, width)));
    }

    private static string FirstLine(string body)
    {
        var index = body.IndexOf('\n');
        return index < 0 ? body : body[..index];
    }

    /// <summary>
    /// Wraps <paramref name="line"/>, which may already carry its own style tags, in the
    /// shared selection highlight style.
    /// </summary>
    public static string Highlight(string line) =>
        Stylize(ThemeTokens.GetStyle("selection.highlight").ToMarkup(), line);

    private static string Stylize(string styleMarkup, string content) =>
        styleMarkup.Length == 0 ? content : $"[{styleMarkup}]{content}[/]";
}
