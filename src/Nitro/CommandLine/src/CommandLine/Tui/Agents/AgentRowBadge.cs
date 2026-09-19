using ChilliCream.Nitro.CommandLine.Services.Workspace;
using ChilliCream.Nitro.CommandLine.Tui.Mail;
using ChilliCream.Nitro.CommandLine.Tui.Theming;

namespace ChilliCream.Nitro.CommandLine.Tui.Agents;

/// <summary>
/// Renders one live participant row for the agents list pane as a single Spectre markup line:
/// selection prefix, implicit marker, actor, presence badge, harness, role, and started/last-heard
/// ages, each padded to the column widths from <see cref="ComputeWidths"/>. A row bound to an implicit
/// durable identity renders dimmed.
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
    public readonly record struct Widths(
        int Actor, int Presence, int Harness, int Role, int StartedAge, int LastHeardAge);

    /// <summary>
    /// Computes <see cref="Widths"/> across <paramref name="rows"/>.
    /// </summary>
    public static Widths ComputeWidths(IReadOnlyList<AgentParticipantRow> rows, DateTimeOffset now)
    {
        var actorWidth = 0;
        var presenceWidth = 0;
        var harnessWidth = 0;
        var roleWidth = 0;
        var startedWidth = 0;
        var lastHeardWidth = 0;

        foreach (var row in rows)
        {
            var session = row.Participant.Session;
            actorWidth = Math.Max(actorWidth, ActorText(session).Length);
            presenceWidth = Math.Max(presenceWidth, PresenceText(row).Length);
            harnessWidth = Math.Max(harnessWidth, session.Harness.Length);
            roleWidth = Math.Max(roleWidth, RoleText(session).Length);
            startedWidth = Math.Max(startedWidth, MailAges.Format(session.StartedAt, now).Length);
            lastHeardWidth = Math.Max(lastHeardWidth, MailAges.Format(session.LastBeatAt, now).Length);
        }

        return new Widths(actorWidth, presenceWidth, harnessWidth, roleWidth, startedWidth, lastHeardWidth);
    }

    /// <summary>
    /// Builds the markup line for one participant row, padding actor/presence/role/ages to
    /// <paramref name="widths"/> and truncating the role with an ellipsis so the line fits within
    /// <paramref name="maxWidth"/> display columns. A <paramref name="maxWidth"/> of 0 or less produces
    /// an empty line.
    /// </summary>
    public static string Render(
        AgentParticipantRow row, DateTimeOffset now, bool selected, int maxWidth, Widths widths)
    {
        if (maxWidth <= 0)
        {
            return string.Empty;
        }

        var session = row.Participant.Session;
        var prefix = selected ? SelectedPrefix : UnselectedPrefix;
        var isImplicit = row.Participant.Agent?.Implicit ?? false;
        var marker = isImplicit ? ImplicitMarker : ExplicitMarker;
        var actor = ActorText(session).PadRight(widths.Actor);
        var presenceBadge = PresenceText(row).PadRight(widths.Presence);
        var harness = session.Harness.PadRight(widths.Harness);
        var startedAge = MailAges.Format(session.StartedAt, now).PadRight(widths.StartedAge);
        var lastHeardAge = MailAges.Format(session.LastBeatAt, now).PadRight(widths.LastHeardAge);

        // Plain-text length of everything but the role.
        var fixedPlainLength = prefix.Length + marker.Length + 1
            + actor.Length + 1
            + presenceBadge.Length + 1
            + harness.Length + 1
            + "started ".Length + startedAge.Length + 1
            + "heard ".Length + lastHeardAge.Length;

        var roleText = RoleText(session).PadRight(widths.Role);
        var roleBudget = Math.Max(0, maxWidth - fixedPlainLength);
        var truncatedRole = Truncate(roleText, roleBudget);

        var actorStyle = ThemeTokens.GetStyle("agents.list.name").ToMarkup();
        var presenceStyle = PresenceStyle(row.Participant.State).ToMarkup();
        var harnessStyle = ThemeTokens.GetStyle("agents.list.harness").ToMarkup();
        var roleStyle = RoleStyle(session.Role).ToMarkup();
        var ageStyle = ThemeTokens.GetStyle("agents.list.age").ToMarkup();

        var line =
            $"{Markup.Escape(prefix)}{Markup.Escape(marker)} "
            + $"{Stylize(actorStyle, Markup.Escape(actor))} "
            + $"{Stylize(presenceStyle, Markup.Escape(presenceBadge))} "
            + $"{Stylize(harnessStyle, Markup.Escape(harness))} "
            + $"{Stylize(roleStyle, Markup.Escape(truncatedRole))} "
            + $"{Stylize(ageStyle, $"started {Markup.Escape(startedAge)}")} "
            + $"{Stylize(ageStyle, $"heard {Markup.Escape(lastHeardAge)}")}";

        if (isImplicit)
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

    private static string ActorText(AgentSessionRecord session)
        => session.AgentName is { Length: > 0 } actor ? actor : AgentParticipantRow.UnboundLabel;

    /// <summary>
    /// The presence badge text: a single glyph for the state, plus the Claude activity read-through's
    /// first letter in parentheses when known.
    /// </summary>
    private static string PresenceText(AgentParticipantRow row)
    {
        var glyph = PresenceGlyph(row.Participant.State);

        return row.Activity is { Length: > 0 } activity
            ? $"{glyph}({char.ToLowerInvariant(activity[0])})"
            : glyph;
    }

    private static string PresenceGlyph(string state) => state switch
    {
        AgentSessionState.Online => "●",
        AgentSessionState.Unreachable => "◐",
        AgentSessionState.Unobservable => "◌",
        AgentSessionState.Remote => "◇",
        _ => "○" // Any unrecognized state.
    };

    /// <summary>
    /// Resolves the theme style for a presence state: a dedicated
    /// <c>agents.list.presence.&lt;state&gt;</c> token, falling back to the
    /// base <c>agents.list.presence</c> token.
    /// </summary>
    public static Style PresenceStyle(string state)
    {
        var perState = ThemeTokens.GetStyle($"agents.list.presence.{state}");

        return perState != Style.Plain ? perState : ThemeTokens.GetStyle("agents.list.presence");
    }

    /// <summary>
    /// Resolves the theme style for <paramref name="role"/>: a per-role
    /// <c>agents.list.role.&lt;role&gt;</c> token keyed by the lowercased role text, falling back to
    /// the base <c>agents.list.role</c> token when no dedicated color is registered, including for the
    /// empty role.
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

    private static string RoleText(AgentSessionRecord session) => session.Role.Length == 0 ? EmptyRole : session.Role;

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
