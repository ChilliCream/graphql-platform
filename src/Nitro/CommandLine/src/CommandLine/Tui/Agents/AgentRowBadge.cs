using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tui.Mail;
using ChilliCream.Nitro.CommandLine.Tui.Theming;

namespace ChilliCream.Nitro.CommandLine.Tui.Agents;

/// <summary>
/// Renders one agent row for the agents list pane as a single Spectre markup
/// line: selection prefix, implicit marker, name, role (a dash when empty),
/// and both timestamps formatted as relative ages via
/// <see cref="MailAges"/>, which is general enough to reuse as-is. Each
/// field lands in a fixed-width column, computed across the currently
/// visible rows by <see cref="ComputeWidths"/>, so name/role/age line up
/// vertically instead of being clubbed into one run-on line. Name, role, and
/// age each carry their own <see cref="ThemeTokens"/> color; implicit agents
/// render their whole row dimmed on top of that.
/// </summary>
internal static class AgentRowBadge
{
    private const string Ellipsis = "…";
    private const string SelectedPrefix = "> ";
    private const string UnselectedPrefix = "  ";
    private const string ImplicitMarker = "i";
    private const string ExplicitMarker = " ";
    private const string EmptyRole = "-";

    /// <summary>
    /// The column widths a set of rows agree on: each column padded to the
    /// widest value among those rows.
    /// </summary>
    public readonly record struct Widths(int Name, int Role, int RegisteredAge, int LastSeenAge);

    /// <summary>
    /// Computes <see cref="Widths"/> across <paramref name="rows"/> (the
    /// rows about to be rendered, typically just the visible slice), so
    /// every row's columns are padded to the widest value actually on
    /// screen rather than to every agent in the list.
    /// </summary>
    public static Widths ComputeWidths(IReadOnlyList<AgentRecord> rows, DateTimeOffset now)
    {
        var nameWidth = 0;
        var roleWidth = 0;
        var registeredWidth = 0;
        var lastSeenWidth = 0;

        foreach (var agent in rows)
        {
            nameWidth = Math.Max(nameWidth, agent.Name.Length);
            roleWidth = Math.Max(roleWidth, RoleText(agent).Length);
            registeredWidth = Math.Max(registeredWidth, MailAges.Format(agent.RegisteredAt, now).Length);
            lastSeenWidth = Math.Max(lastSeenWidth, MailAges.Format(agent.LastSeenAt, now).Length);
        }

        return new Widths(nameWidth, roleWidth, registeredWidth, lastSeenWidth);
    }

    /// <summary>
    /// Builds the markup line for one agent row, padding name/role/ages to
    /// <paramref name="widths"/> and then truncating the role with an
    /// ellipsis so the whole line still fits within
    /// <paramref name="maxWidth"/> display columns on narrow terminals. A
    /// <paramref name="maxWidth"/> of 0 or less produces an empty line.
    /// </summary>
    public static string Render(AgentRecord agent, DateTimeOffset now, bool selected, int maxWidth, Widths widths)
    {
        if (maxWidth <= 0)
        {
            return string.Empty;
        }

        var prefix = selected ? SelectedPrefix : UnselectedPrefix;
        var marker = agent.Implicit ? ImplicitMarker : ExplicitMarker;
        var name = agent.Name.PadRight(widths.Name);
        var registeredAge = MailAges.Format(agent.RegisteredAt, now).PadRight(widths.RegisteredAge);
        var lastSeenAge = MailAges.Format(agent.LastSeenAt, now).PadRight(widths.LastSeenAge);

        // Plain-text length of everything but the role, so the role can be
        // truncated to make the whole line fit maxWidth.
        var fixedPlainLength = prefix.Length + marker.Length + 1
            + name.Length + 1
            + "reg ".Length + registeredAge.Length + 1
            + "seen ".Length + lastSeenAge.Length;

        var roleText = RoleText(agent).PadRight(widths.Role);
        var roleBudget = Math.Max(0, maxWidth - fixedPlainLength);
        var truncatedRole = Truncate(roleText, roleBudget);

        var nameStyle = ThemeTokens.GetStyle("agents.list.name").ToMarkup();
        var roleStyle = ThemeTokens.GetStyle("agents.list.role").ToMarkup();
        var ageStyle = ThemeTokens.GetStyle("agents.list.age").ToMarkup();

        var line =
            $"{Markup.Escape(prefix)}{Markup.Escape(marker)} "
            + $"{Stylize(nameStyle, Markup.Escape(name))} "
            + $"{Stylize(roleStyle, Markup.Escape(truncatedRole))} "
            + $"{Stylize(ageStyle, $"reg {Markup.Escape(registeredAge)}")} "
            + $"{Stylize(ageStyle, $"seen {Markup.Escape(lastSeenAge)}")}";

        if (agent.Implicit)
        {
            var implicitStyle = ThemeTokens.GetStyle("agents.list.implicit").ToMarkup();
            line = Stylize(implicitStyle, line);
        }

        if (selected)
        {
            var highlightStyle = ThemeTokens.GetStyle("selection.highlight").ToMarkup();
            line = Stylize(highlightStyle, line);
        }

        return line;
    }

    private static string RoleText(AgentRecord agent) => agent.Role.Length == 0 ? EmptyRole : agent.Role;

    private static string Stylize(string styleMarkup, string content) =>
        styleMarkup.Length == 0 ? content : $"[{styleMarkup}]{content}[/]";

    private static string Truncate(string value, int width)
    {
        if (width <= 0)
        {
            return string.Empty;
        }

        if (value.Length <= width)
        {
            return value;
        }

        if (width == 1)
        {
            return Ellipsis;
        }

        return string.Concat(value.AsSpan(0, width - 1), Ellipsis);
    }
}
