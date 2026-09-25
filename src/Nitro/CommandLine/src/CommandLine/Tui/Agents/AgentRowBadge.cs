using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tui.Theming;
using ChilliCream.Nitro.CommandLine.Tui.Widgets;

namespace ChilliCream.Nitro.CommandLine.Tui.Agents;

/// <summary>
/// Renders the Agents table's header row and each agent row: a presence bubble and name,
/// role, harness, and age columns. Name and Role are never dropped; narrow widths drop the
/// Started column first, then Harness, then Last Seen, identically for the header and the
/// rows so the two always agree. When even Name and Role no longer fit, Role is truncated
/// first, then Name, with an ellipsis. Built on the shared <see cref="TableLayout"/> and
/// <see cref="TableRenderer"/> table widget.
/// </summary>
internal static class AgentRowBadge
{
    private const string SelectedPrefix = "> ";
    private const string UnselectedPrefix = "  ";
    private const string EmptyRole = "-";
    private const string BubbleGlyph = "●";

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

    // Started drops first, then Harness, then Last Seen; Name and Role have no drop priority
    // so they are never dropped. This is the display order the columns render in.
    private static readonly IReadOnlyList<TableColumnSpec> s_columns =
    [
        new TableColumnSpec(NameHeader, MinNameWidth),
        new TableColumnSpec(RoleHeader, MinRoleWidth),
        new TableColumnSpec(HarnessHeader, MinHarnessWidth, DropPriority: 1),
        new TableColumnSpec(StartedHeader, MinStartedWidth, DropPriority: 0),
        new TableColumnSpec(LastSeenHeader, MinLastSeenWidth, DropPriority: 2)
    ];

    // Same columns, with Role ordered before Name, used only to plan widths: TableLayout.Plan
    // truncates the first never-dropped column that is still visible once every droppable
    // column is gone, so putting Role first here makes Role truncate before Name while the
    // rendered column order above stays Name, then Role.
    private static readonly IReadOnlyList<TableColumnSpec> s_widthPlanColumns =
    [
        new TableColumnSpec(RoleHeader, MinRoleWidth),
        new TableColumnSpec(NameHeader, MinNameWidth),
        new TableColumnSpec(HarnessHeader, MinHarnessWidth, DropPriority: 1),
        new TableColumnSpec(StartedHeader, MinStartedWidth, DropPriority: 0),
        new TableColumnSpec(LastSeenHeader, MinLastSeenWidth, DropPriority: 2)
    ];

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
        var rowValues = new List<IReadOnlyList<string>>(rows.Count);

        foreach (var row in rows)
        {
            rowValues.Add(
            [
                row.Name,
                RoleText(row),
                AgentHarnessDisplay.Name(row.Harness),
                FormatAge(row.StartedAt, now),
                FormatAge(row.LastSeenAt, now)
            ]);
        }

        var widths = TableLayout.ComputeWidths(s_columns, rowValues);
        return new Widths(widths[0], widths[1], widths[2], widths[3], widths[4]);
    }

    /// <summary>
    /// Builds the markup line for one agent row. Columns are padded to <paramref name="widths"/>.
    /// When the full set of columns does not fit within <paramref name="maxWidth"/> display
    /// columns, Started is dropped first, then Harness, then Last Seen; Name and Role always
    /// remain, with Role truncated first and then Name as a last resort. A
    /// <paramref name="maxWidth"/> of 0 or less produces an empty line.
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

        var cells = new TableCellSpec[]
        {
            new(row.Name, nameStyle),
            new(RoleText(row), roleStyle),
            new(AgentHarnessDisplay.Name(row.Harness), harnessStyle),
            new(FormatAge(row.StartedAt, now), ageStyle),
            new(FormatAge(row.LastSeenAt, now), ageStyle)
        };

        var budget = maxWidth - PrefixWidth(prefix);
        var layout = PlanLayout(budget, widths);
        var line = TableRenderer.RenderRow(
            prefix, new TableCellSpec(BubbleGlyph, bubbleStyle), cells, s_columns, layout);

        if (selected)
        {
            var highlightStyle = ThemeTokens.GetStyle("selection.highlight").ToMarkup();
            line = TableRenderer.Stylize(highlightStyle, line);
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
        var budget = maxWidth - PrefixWidth(UnselectedPrefix);
        var layout = PlanLayout(budget, widths);

        return TableRenderer.RenderRow(
            UnselectedPrefix, new TableCellSpec(blankBubble), BuildHeaderCells(headerStyle), s_columns, layout);
    }

    /// <summary>
    /// Builds the dashed rule line under the header, filling <paramref name="maxWidth"/>
    /// display columns. A <paramref name="maxWidth"/> of 0 or less produces an empty line.
    /// </summary>
    public static string RenderRule(int maxWidth)
    {
        var ruleStyle = ThemeTokens.GetStyle("agents.list.age").ToMarkup();
        return TableRenderer.RenderRule(maxWidth, ruleStyle);
    }

    /// <summary>
    /// Appends the Agents table's fixed top block to <paramref name="lines"/>: a blank line, the
    /// header row and the rule, keeping only the first <paramref name="headerLineCount"/> of the
    /// three. A <paramref name="maxWidth"/> of 0 or less appends nothing.
    /// </summary>
    public static void AddHeaderLines(List<string> lines, int headerLineCount, int maxWidth, Widths widths)
    {
        if (maxWidth <= 0)
        {
            return;
        }

        var headerStyle = ThemeTokens.GetStyle("agents.list.header").ToMarkup();
        var ruleStyle = ThemeTokens.GetStyle("agents.list.age").ToMarkup();
        var blankBubble = new string(' ', DisplayWidth.Measure(BubbleGlyph));
        var budget = maxWidth - PrefixWidth(UnselectedPrefix);
        var layout = PlanLayout(budget, widths);

        TableRenderer.RenderTopBlock(
            lines,
            headerLineCount,
            maxWidth,
            UnselectedPrefix,
            new TableCellSpec(blankBubble),
            s_columns,
            layout,
            headerStyle,
            ruleStyle);
    }

    private static TableCellSpec[] BuildHeaderCells(string headerStyle) =>
    [
        new(NameHeader, headerStyle),
        new(RoleHeader, headerStyle),
        new(HarnessHeader, headerStyle),
        new(StartedHeader, headerStyle),
        new(LastSeenHeader, headerStyle)
    ];

    private static int PrefixWidth(string prefix) =>
        DisplayWidth.Measure(prefix) + DisplayWidth.Measure(BubbleGlyph) + 1;

    /// <summary>
    /// Plans column visibility and widths for <paramref name="budget"/> display columns, in the
    /// display order Name, Role, Harness, Started, Last Seen.
    /// </summary>
    private static IReadOnlyList<ColumnLayout> PlanLayout(int budget, Widths widths)
    {
        IReadOnlyList<int> planWidths = [widths.Role, widths.Name, widths.Harness, widths.Started, widths.LastSeen];
        var planLayout = TableLayout.Plan(budget, s_widthPlanColumns, planWidths).ToArray();
        var role = planLayout[0];
        var name = planLayout[1];

        // TableLayout.Plan only truncates the first still-visible column once every droppable
        // column is gone, which is Role here. When Role alone does not free enough room, also
        // shrink Name to what is left so the row still fits within budget.
        if (role.Visible && name.Visible)
        {
            var used = role.Width + name.Width + DisplayWidth.Measure(TableLayout.Gutter);

            if (used > budget)
            {
                var nameWidth = Math.Max(0, budget - role.Width - DisplayWidth.Measure(TableLayout.Gutter));
                planLayout[1] = new ColumnLayout(true, nameWidth);
            }
        }

        return [planLayout[1], planLayout[0], planLayout[2], planLayout[3], planLayout[4]];
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
}
