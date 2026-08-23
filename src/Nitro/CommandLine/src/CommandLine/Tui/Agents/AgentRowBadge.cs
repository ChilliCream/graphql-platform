using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tui.Mail;
using ChilliCream.Nitro.CommandLine.Tui.Theming;

namespace ChilliCream.Nitro.CommandLine.Tui.Agents;

/// <summary>
/// Renders one agent row for the agents list pane as a single Spectre markup
/// line: selection prefix, implicit marker, name, presence badge, client (a
/// dash when empty), role (a dash when empty), and both timestamps formatted
/// as relative ages via <see cref="MailAges"/>, which is general enough to
/// reuse as-is. Each field lands in a fixed-width column, computed across
/// the currently visible rows by <see cref="ComputeWidths"/>, so
/// name/presence/client/role/age line up vertically instead of being
/// clubbed into one run-on line. Name, presence, client, role, and age each
/// carry their own <see cref="ThemeTokens"/> color; implicit agents render
/// their whole row dimmed on top of that.
/// </summary>
internal static class AgentRowBadge
{
    private const string Ellipsis = "…";
    private const string SelectedPrefix = "> ";
    private const string UnselectedPrefix = "  ";
    private const string ImplicitMarker = "i";
    private const string ExplicitMarker = " ";
    private const string EmptyRole = "-";
    private const string EmptyClient = "-";

    /// <summary>
    /// The column widths a set of rows agree on: each column padded to the
    /// widest value among those rows.
    /// </summary>
    public readonly record struct Widths(
        int Name, int Presence, int Client, int Role, int RegisteredAge, int LastSeenAge);

    /// <summary>
    /// Computes <see cref="Widths"/> across <paramref name="rows"/> (the
    /// rows about to be rendered, typically just the visible slice), so
    /// every row's columns are padded to the widest value actually on
    /// screen rather than to every agent in the list. <paramref name="presence"/>
    /// looks an agent up by name; an agent with no entry (should not happen
    /// once <see cref="AgentsState"/> has loaded) is treated as offline.
    /// </summary>
    public static Widths ComputeWidths(
        IReadOnlyList<AgentRecord> rows,
        IReadOnlyDictionary<string, AgentPresence> presence,
        DateTimeOffset now)
    {
        var nameWidth = 0;
        var presenceWidth = 0;
        var clientWidth = 0;
        var roleWidth = 0;
        var registeredWidth = 0;
        var lastSeenWidth = 0;

        foreach (var agent in rows)
        {
            nameWidth = Math.Max(nameWidth, agent.Name.Length);
            presenceWidth = Math.Max(presenceWidth, PresenceText(ResolvePresence(agent, presence)).Length);
            clientWidth = Math.Max(clientWidth, ClientText(agent).Length);
            roleWidth = Math.Max(roleWidth, RoleText(agent).Length);
            registeredWidth = Math.Max(registeredWidth, MailAges.Format(agent.RegisteredAt, now).Length);
            lastSeenWidth = Math.Max(lastSeenWidth, MailAges.Format(agent.LastSeenAt, now).Length);
        }

        return new Widths(nameWidth, presenceWidth, clientWidth, roleWidth, registeredWidth, lastSeenWidth);
    }

    /// <summary>
    /// Builds the markup line for one agent row, padding name/presence/role/ages
    /// to <paramref name="widths"/> and then truncating the role with an
    /// ellipsis so the whole line still fits within
    /// <paramref name="maxWidth"/> display columns on narrow terminals. A
    /// <paramref name="maxWidth"/> of 0 or less produces an empty line.
    /// <paramref name="presence"/> looks the agent's presence up by name; see
    /// <see cref="ComputeWidths"/>.
    /// </summary>
    public static string Render(
        AgentRecord agent,
        IReadOnlyDictionary<string, AgentPresence> presence,
        DateTimeOffset now,
        bool selected,
        int maxWidth,
        Widths widths)
    {
        if (maxWidth <= 0)
        {
            return string.Empty;
        }

        var agentPresence = ResolvePresence(agent, presence);
        var prefix = selected ? SelectedPrefix : UnselectedPrefix;
        var marker = agent.Implicit ? ImplicitMarker : ExplicitMarker;
        var name = agent.Name.PadRight(widths.Name);
        var presenceBadge = PresenceText(agentPresence).PadRight(widths.Presence);
        var client = ClientText(agent).PadRight(widths.Client);
        var registeredAge = MailAges.Format(agent.RegisteredAt, now).PadRight(widths.RegisteredAge);
        var lastSeenAge = MailAges.Format(agent.LastSeenAt, now).PadRight(widths.LastSeenAge);

        // Plain-text length of everything but the role, so the role can be
        // truncated to make the whole line fit maxWidth.
        var fixedPlainLength = prefix.Length + marker.Length + 1
            + name.Length + 1
            + presenceBadge.Length + 1
            + client.Length + 1
            + "reg ".Length + registeredAge.Length + 1
            + "seen ".Length + lastSeenAge.Length;

        var roleText = RoleText(agent).PadRight(widths.Role);
        var roleBudget = Math.Max(0, maxWidth - fixedPlainLength);
        var truncatedRole = Truncate(roleText, roleBudget);

        var nameStyle = ThemeTokens.GetStyle("agents.list.name").ToMarkup();
        var presenceStyle = PresenceStyle(agentPresence).ToMarkup();
        var clientStyle = ThemeTokens.GetStyle("agents.list.client").ToMarkup();
        var roleStyle = RoleStyle(agent.Role).ToMarkup();
        var ageStyle = ThemeTokens.GetStyle("agents.list.age").ToMarkup();

        var line =
            $"{Markup.Escape(prefix)}{Markup.Escape(marker)} "
            + $"{Stylize(nameStyle, Markup.Escape(name))} "
            + $"{Stylize(presenceStyle, Markup.Escape(presenceBadge))} "
            + $"{Stylize(clientStyle, Markup.Escape(client))} "
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

    private static AgentPresence ResolvePresence(
        AgentRecord agent, IReadOnlyDictionary<string, AgentPresence> presence)
        => presence.GetValueOrDefault(agent.Name, AgentPresence.Offline);

    /// <summary>
    /// Conflict marker glyph: an agent whose live sessions disagree on state
    /// (the plan's "same-actor multi-session conflicts surfaced, not
    /// hidden"). Deliberately distinct from every single-state glyph so a
    /// conflict cannot be mistaken for one of the states it is conflicting
    /// between; the exact states and counts are one <c>agent list</c> or
    /// <c>agent session list</c> away, which is enough real estate for the
    /// narrow list row not to need to spell them out.
    /// </summary>
    private const string ConflictGlyph = "⚠";

    /// <summary>
    /// The presence badge text: a single glyph for the state (kept to one
    /// or two characters so the badge does not crowd out the role column on
    /// a narrow terminal - full state names are what <c>agent list</c>'s
    /// plain-text output is for), plus the live session count when the
    /// agent's sessions disagree on state, or the Claude activity
    /// read-through's first letter in parentheses when known (mutually
    /// exclusive - see <see cref="AgentPresence.Compute"/>, which only sets
    /// activity for a single, unconflicted session).
    /// </summary>
    private static string PresenceText(AgentPresence presence)
    {
        if (presence.Conflicted)
        {
            return ConflictGlyph + presence.SessionCount;
        }

        var glyph = PresenceGlyph(presence.State);

        return presence.Activity is { Length: > 0 } activity
            ? $"{glyph}({char.ToLowerInvariant(activity[0])})"
            : glyph;
    }

    private static string PresenceGlyph(string state) => state switch
    {
        AgentSessionState.Online => "●",
        AgentSessionState.Unreachable => "◐",
        AgentSessionState.Remote => "◇",
        _ => "○" // AgentPresenceState.Offline, and any unrecognized state.
    };

    /// <summary>
    /// Resolves the theme style for a presence state: a dedicated
    /// <c>agents.list.presence.&lt;state&gt;</c> token (the conflict marker
    /// uses its own <c>agents.list.presence.conflict</c> token instead),
    /// falling back to the base <c>agents.list.presence</c> token.
    /// </summary>
    private static Style PresenceStyle(AgentPresence presence)
    {
        var token = presence.Conflicted ? "agents.list.presence.conflict" : $"agents.list.presence.{presence.State}";
        var perState = ThemeTokens.GetStyle(token);

        return perState != Style.Plain ? perState : ThemeTokens.GetStyle("agents.list.presence");
    }

    /// <summary>
    /// Resolves the theme style for <paramref name="role"/>: a per-role
    /// <c>agents.list.role.&lt;role&gt;</c> token keyed by the lowercased
    /// role text, falling back to the base <c>agents.list.role</c> token
    /// when no dedicated color is registered (including for the empty
    /// role). Shared with <see cref="AgentDetailBody"/> so the list and the
    /// Identity section agree on a role's color.
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

    private static string RoleText(AgentRecord agent) => agent.Role.Length == 0 ? EmptyRole : agent.Role;

    private static string ClientText(AgentRecord agent) => agent.Client.Length == 0 ? EmptyClient : agent.Client;

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
