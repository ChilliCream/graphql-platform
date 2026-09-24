using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tui.Theming;

namespace ChilliCream.Nitro.CommandLine.Tui.Agents;

/// <summary>
/// Renders the Agents table's header row and each agent row: a presence bubble and name,
/// role, harness, and age columns. Narrow widths drop the Started column first, then Role,
/// identically for the header and the rows so the two always agree.
/// </summary>
internal static class AgentRowBadge
{
    private const string SelectedPrefix = "> ";
    private const string UnselectedPrefix = "  ";
    private const string EmptyRole = "-";
    private const string BubbleGlyph = "●";
    private const string ColumnGutter = "    ";
    private const char RuleGlyph = '─';

    private const string NameHeader = "NAME";
    private const string RoleHeader = "ROLE";
    private const string HarnessHeader = "HARNESS";
    private const string StartedHeader = "STARTED";
    private const string LastSeenHeader = "LAST SEEN";

    private const int MinNameWidth = 12;
    private const int MinRoleWidth = 12;
    private const int MinHarnessWidth = 12;
    private const int MinStartedWidth = 10;
    private const int MinLastSeenWidth = 10;

    private enum ColumnPlan
    {
        Full,
        WithoutStarted,
        WithoutStartedAndRole,
        NameAndLastSeenOnly
    }

    /// <summary>
    /// The column widths a set of rows agree on: each column padded to at least its minimum
    /// width, and wider still when its header title or a row's value needs more room.
    /// </summary>
    public readonly record struct Widths(int Name, int Role, int Harness, int Started, int LastSeen);

    /// <summary>
    /// Computes <see cref="Widths"/> across <paramref name="rows"/>, with each column no
    /// narrower than its minimum width or its header title.
    /// </summary>
    public static Widths ComputeWidths(IReadOnlyList<AgentRow> rows, DateTimeOffset now)
    {
        var name = Math.Max(MinNameWidth, DisplayWidth.Measure(NameHeader));
        var role = Math.Max(MinRoleWidth, DisplayWidth.Measure(RoleHeader));
        var harness = Math.Max(MinHarnessWidth, DisplayWidth.Measure(HarnessHeader));
        var started = Math.Max(MinStartedWidth, DisplayWidth.Measure(StartedHeader));
        var lastSeen = Math.Max(MinLastSeenWidth, DisplayWidth.Measure(LastSeenHeader));

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

        var budget = maxWidth - PrefixWidth(prefix);
        var plan = DecideColumns(budget, widths);
        var (nameCell, roleCell, harnessCell, startedCell) =
            SelectCells(plan, budget, widths, row.Name, name, role, harness, started);

        var line = BuildLine(
            prefix, BubbleGlyph, bubbleStyle,
            nameCell, nameStyle,
            roleCell, roleStyle,
            harnessCell, harnessStyle,
            startedCell, ageStyle,
            lastSeen, ageStyle);

        if (selected)
        {
            var highlightStyle = ThemeTokens.GetStyle("selection.highlight").ToMarkup();
            line = Stylize(highlightStyle, line);
        }

        return line;
    }

    /// <summary>
    /// Builds the header title line shown above the rows: NAME, ROLE, HARNESS, STARTED, LAST
    /// SEEN, aligned to <paramref name="widths"/> with the cursor and bubble cells left blank.
    /// Columns are dropped using the same thresholds as <see cref="Render"/>, so the header
    /// always agrees with the rows below it. A <paramref name="maxWidth"/> of 0 or less
    /// produces an empty line.
    /// </summary>
    public static string RenderHeader(int maxWidth, Widths widths)
    {
        if (maxWidth <= 0)
        {
            return string.Empty;
        }

        var headerStyle = ThemeTokens.GetStyle("agents.list.header").ToMarkup();
        var blankBubble = new string(' ', DisplayWidth.Measure(BubbleGlyph));

        var name = DisplayWidth.PadRight(NameHeader, widths.Name);
        var role = DisplayWidth.PadRight(RoleHeader, widths.Role);
        var harness = DisplayWidth.PadRight(HarnessHeader, widths.Harness);
        var started = DisplayWidth.PadRight(StartedHeader, widths.Started);
        var lastSeen = DisplayWidth.PadRight(LastSeenHeader, widths.LastSeen);

        var budget = maxWidth - PrefixWidth(UnselectedPrefix);
        var plan = DecideColumns(budget, widths);
        var (nameCell, roleCell, harnessCell, startedCell) =
            SelectCells(plan, budget, widths, NameHeader, name, role, harness, started);

        return BuildLine(
            UnselectedPrefix, blankBubble, string.Empty,
            nameCell, headerStyle,
            roleCell, headerStyle,
            harnessCell, headerStyle,
            startedCell, headerStyle,
            lastSeen, headerStyle);
    }

    /// <summary>
    /// Builds the dashed rule line under the header, filling <paramref name="maxWidth"/>
    /// display columns. A <paramref name="maxWidth"/> of 0 or less produces an empty line.
    /// </summary>
    public static string RenderRule(int maxWidth)
    {
        if (maxWidth <= 0)
        {
            return string.Empty;
        }

        var ruleStyle = ThemeTokens.GetStyle("agents.list.age").ToMarkup();
        return Stylize(ruleStyle, new string(RuleGlyph, maxWidth));
    }

    private static int PrefixWidth(string prefix) =>
        DisplayWidth.Measure(prefix) + DisplayWidth.Measure(BubbleGlyph) + 1;

    /// <summary>
    /// Decides which optional columns fit <paramref name="budget"/> display columns given
    /// <paramref name="widths"/>: Started is dropped first, then Role, with Name and Last Seen
    /// always kept (Name truncated as a last resort).
    /// </summary>
    private static ColumnPlan DecideColumns(int budget, Widths widths)
    {
        var gutter = DisplayWidth.Measure(ColumnGutter);

        if (budget >= widths.Name + gutter + widths.Role + gutter + widths.Harness + gutter
            + widths.Started + gutter + widths.LastSeen)
        {
            return ColumnPlan.Full;
        }

        if (budget >= widths.Name + gutter + widths.Role + gutter + widths.Harness + gutter + widths.LastSeen)
        {
            return ColumnPlan.WithoutStarted;
        }

        if (budget >= widths.Name + gutter + widths.Harness + gutter + widths.LastSeen)
        {
            return ColumnPlan.WithoutStartedAndRole;
        }

        return ColumnPlan.NameAndLastSeenOnly;
    }

    /// <summary>
    /// Resolves the Name, Role, Harness, and Started cells for <paramref name="plan"/>: the
    /// dropped columns become null, and the narrowest plan truncates <paramref name="rawName"/>
    /// to what remains of <paramref name="budget"/> after Last Seen.
    /// </summary>
    private static (string Name, string? Role, string? Harness, string? Started) SelectCells(
        ColumnPlan plan,
        int budget,
        Widths widths,
        string rawName,
        string paddedName,
        string paddedRole,
        string paddedHarness,
        string paddedStarted)
    {
        if (plan == ColumnPlan.NameAndLastSeenOnly)
        {
            var truncatedNameBudget = Math.Max(0, budget - DisplayWidth.Measure(ColumnGutter) - widths.LastSeen);
            return (DisplayWidth.Truncate(rawName, truncatedNameBudget), null, null, null);
        }

        var role = plan is ColumnPlan.Full or ColumnPlan.WithoutStarted ? paddedRole : null;
        var started = plan == ColumnPlan.Full ? paddedStarted : null;

        return (paddedName, role, paddedHarness, started);
    }

    private static string BuildLine(
        string prefix,
        string bubble,
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
        var line = $"{Markup.Escape(prefix)}{Stylize(bubbleStyle, Markup.Escape(bubble))} "
            + $"{Stylize(nameStyle, Markup.Escape(name))}";

        if (role is not null)
        {
            line += ColumnGutter + Stylize(roleStyle, Markup.Escape(role));
        }

        if (harness is not null)
        {
            line += ColumnGutter + Stylize(harnessStyle, Markup.Escape(harness));
        }

        if (started is not null)
        {
            line += ColumnGutter + Stylize(startedStyle, Markup.Escape(started));
        }

        line += ColumnGutter + Stylize(lastSeenStyle, Markup.Escape(lastSeen));

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
