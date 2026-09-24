using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tui.Theming;

namespace ChilliCream.Nitro.CommandLine.Tui.Agents;

/// <summary>
/// Renders one agent row: a presence bubble and name, role, harness, and age columns.
/// Narrow widths drop the Started column first, then Role; Name and Last Seen always remain.
/// </summary>
internal static class AgentRowBadge
{
    private const string SelectedPrefix = "> ";
    private const string UnselectedPrefix = "  ";
    private const string EmptyRole = "-";
    private const string BubbleGlyph = "●";

    /// <summary>
    /// The column widths a set of rows agree on: each column padded to the
    /// widest value among those rows.
    /// </summary>
    public readonly record struct Widths(int Name, int Role, int Harness, int Started, int LastSeen);

    /// <summary>
    /// Computes <see cref="Widths"/> across <paramref name="rows"/>.
    /// </summary>
    public static Widths ComputeWidths(IReadOnlyList<AgentRow> rows, DateTimeOffset now)
    {
        var name = 0;
        var role = 0;
        var harness = 0;
        var started = 0;
        var lastSeen = 0;

        foreach (var row in rows)
        {
            name = Math.Max(name, DisplayWidth.Measure(row.Name));
            role = Math.Max(role, DisplayWidth.Measure(RoleText(row)));
            harness = Math.Max(harness, DisplayWidth.Measure(AgentHarnessDisplay.Name(row.Harness)));
            started = Math.Max(started, DisplayWidth.Measure(FormatAge(row.StartedAt, now)));
            lastSeen = Math.Max(lastSeen, DisplayWidth.Measure(FormatAge(row.LastSeenAt, now)));
        }

        return new Widths(name, role, harness, started, lastSeen);
    }

    /// <summary>
    /// Builds the markup line for one agent row. Columns are padded to <paramref name="widths"/>.
    /// When the full set of columns does not fit within <paramref name="maxWidth"/> display
    /// columns, Started is dropped first, then Role; Name and Last Seen always remain, with
    /// Name truncated as a last resort. A <paramref name="maxWidth"/> of 0 or less produces an
    /// empty line.
    /// </summary>
    public static string Render(
        AgentRow row, DateTimeOffset now, bool selected, int maxWidth, Widths widths)
    {
        if (maxWidth <= 0)
        {
            return string.Empty;
        }

        var prefix = selected ? SelectedPrefix : UnselectedPrefix;
        var state = AgentStateResolver.Resolve(row, now);
        var bubbleStyle = PresenceStyle(state).ToMarkup();
        var nameStyle = ThemeTokens.GetStyle("agents.list.name").ToMarkup();
        var roleStyle = RoleStyle(row.Role).ToMarkup();
        var harnessStyle = ThemeTokens.GetStyle("agents.list.harness").ToMarkup();
        var ageStyle = ThemeTokens.GetStyle("agents.list.age").ToMarkup();

        var name = DisplayWidth.PadRight(row.Name, widths.Name);
        var role = DisplayWidth.PadRight(RoleText(row), widths.Role);
        var harness = DisplayWidth.PadRight(AgentHarnessDisplay.Name(row.Harness), widths.Harness);
        var started = DisplayWidth.PadRight(FormatAge(row.StartedAt, now), widths.Started);
        var lastSeen = DisplayWidth.PadRight(FormatAge(row.LastSeenAt, now), widths.LastSeen);

        var prefixWidth = DisplayWidth.Measure(prefix) + DisplayWidth.Measure(BubbleGlyph) + 1;
        var budget = maxWidth - prefixWidth;

        var nameWidth = DisplayWidth.Measure(name);
        var roleWidth = DisplayWidth.Measure(role);
        var harnessWidth = DisplayWidth.Measure(harness);
        var startedWidth = DisplayWidth.Measure(started);
        var lastSeenWidth = DisplayWidth.Measure(lastSeen);

        string line;

        if (budget >= nameWidth + 1 + roleWidth + 1 + harnessWidth + 1 + startedWidth + 1 + lastSeenWidth)
        {
            line = BuildLine(
                prefix, bubbleStyle,
                name, nameStyle,
                role, roleStyle,
                harness, harnessStyle,
                started, ageStyle,
                lastSeen, ageStyle);
        }
        else if (budget >= nameWidth + 1 + roleWidth + 1 + harnessWidth + 1 + lastSeenWidth)
        {
            line = BuildLine(
                prefix, bubbleStyle,
                name, nameStyle,
                role, roleStyle,
                harness, harnessStyle,
                started: null, ageStyle,
                lastSeen, ageStyle);
        }
        else if (budget >= nameWidth + 1 + harnessWidth + 1 + lastSeenWidth)
        {
            line = BuildLine(
                prefix, bubbleStyle,
                name, nameStyle,
                role: null, roleStyle,
                harness, harnessStyle,
                started: null, ageStyle,
                lastSeen, ageStyle);
        }
        else
        {
            var truncatedNameBudget = Math.Max(0, budget - 1 - lastSeenWidth);
            var truncatedName = DisplayWidth.Truncate(row.Name, truncatedNameBudget);

            line = BuildLine(
                prefix, bubbleStyle,
                truncatedName, nameStyle,
                role: null, roleStyle,
                harness: null, harnessStyle,
                started: null, ageStyle,
                lastSeen, ageStyle);
        }

        if (selected)
        {
            var highlightStyle = ThemeTokens.GetStyle("selection.highlight").ToMarkup();
            line = Stylize(highlightStyle, line);
        }

        return line;
    }

    private static string BuildLine(
        string prefix,
        string bubbleStyle,
        string name,
        string nameStyle,
        string? role,
        string roleStyle,
        string? harness,
        string harnessStyle,
        string? started,
        string startedStyle,
        string lastSeen,
        string lastSeenStyle)
    {
        var line = $"{Markup.Escape(prefix)}{Stylize(bubbleStyle, BubbleGlyph)} "
            + $"{Stylize(nameStyle, Markup.Escape(name))}";

        if (role is not null)
        {
            line += $" {Stylize(roleStyle, Markup.Escape(role))}";
        }

        if (harness is not null)
        {
            line += $" {Stylize(harnessStyle, Markup.Escape(harness))}";
        }

        if (started is not null)
        {
            line += $" {Stylize(startedStyle, Markup.Escape(started))}";
        }

        line += $" {Stylize(lastSeenStyle, Markup.Escape(lastSeen))}";

        return line;
    }

    /// <summary>
    /// Resolves the theme style for an agent's presence state: a dedicated
    /// <c>agents.list.presence.&lt;state&gt;</c> token, falling back to the
    /// base <c>agents.list.presence</c> token.
    /// </summary>
    public static Style PresenceStyle(AgentState state)
    {
        var token = state switch
        {
            AgentState.Online => "agents.list.presence.online",
            AgentState.Unreachable => "agents.list.presence.unreachable",
            _ => "agents.list.presence.offline"
        };

        var perState = ThemeTokens.GetStyle(token);

        return perState != Style.Plain ? perState : ThemeTokens.GetStyle("agents.list.presence");
    }

    /// <summary>
    /// Returns the style for the lowercased role, or the base role style when the
    /// role is empty or its dedicated style is plain.
    /// </summary>
    public static Style RoleStyle(string role)
    {
        if (role.Length > 0)
        {
            var perRole = ThemeTokens.GetStyle($"agents.list.role.{role.ToLowerInvariant()}");

            if (perRole != Style.Plain)
            {
                return perRole;
            }
        }

        return ThemeTokens.GetStyle("agents.list.role");
    }

    private static string RoleText(AgentRow row) => row.Role.Length == 0 ? EmptyRole : row.Role;

    private static string FormatAge(DateTimeOffset value, DateTimeOffset now) => AgentAges.Format(value, now);

    private static string Stylize(string styleMarkup, string content) =>
        styleMarkup.Length == 0 ? content : $"[{styleMarkup}]{content}[/]";
}
