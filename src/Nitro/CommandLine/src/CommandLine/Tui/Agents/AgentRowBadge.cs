using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tui.Mail;
using ChilliCream.Nitro.CommandLine.Tui.Theming;

namespace ChilliCream.Nitro.CommandLine.Tui.Agents;

/// <summary>
/// Renders one agent row for the agents list pane as a single Spectre markup
/// line: selection prefix, implicit marker, name, role (a dash when empty),
/// and both timestamps formatted as relative ages via
/// <see cref="MailAges"/>, which is general enough to reuse as-is.
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
    /// Builds the markup line for one agent row, truncating the role with an
    /// ellipsis so the whole line fits within <paramref name="maxWidth"/>
    /// display columns. A <paramref name="maxWidth"/> of 0 or less produces
    /// an empty line.
    /// </summary>
    public static string Render(AgentRecord agent, DateTimeOffset now, bool selected, int maxWidth)
    {
        if (maxWidth <= 0)
        {
            return string.Empty;
        }

        var prefix = selected ? SelectedPrefix : UnselectedPrefix;
        var marker = agent.Implicit ? ImplicitMarker : ExplicitMarker;
        var registeredAge = MailAges.Format(agent.RegisteredAt, now);
        var lastSeenAge = MailAges.Format(agent.LastSeenAt, now);

        // Plain-text length of everything but the role, so the role can be
        // truncated to make the whole line fit maxWidth.
        var fixedPlainLength = prefix.Length + marker.Length + 1
            + agent.Name.Length + 1
            + "reg ".Length + registeredAge.Length + 1
            + "seen ".Length + lastSeenAge.Length;

        var roleText = agent.Role.Length == 0 ? EmptyRole : agent.Role;
        var roleBudget = Math.Max(0, maxWidth - fixedPlainLength);
        var truncatedRole = Truncate(roleText, roleBudget);
        var escapedRole = Markup.Escape(truncatedRole);

        var line =
            $"{Markup.Escape(prefix)}{Markup.Escape(marker)} "
            + $"{Markup.Escape(agent.Name)} "
            + $"{escapedRole} "
            + $"reg {Markup.Escape(registeredAge)} "
            + $"seen {Markup.Escape(lastSeenAge)}";

        if (selected)
        {
            var highlightStyle = ThemeTokens.GetStyle("selection.highlight").ToMarkup();
            line = Stylize(highlightStyle, line);
        }

        return line;
    }

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
