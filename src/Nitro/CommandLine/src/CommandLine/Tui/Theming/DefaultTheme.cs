namespace ChilliCream.Nitro.CommandLine.Tui.Theming;

/// <summary>
/// The single built-in token set backing <see cref="ThemeTokens"/>.
/// </summary>
internal static class DefaultTheme
{
    public static readonly IReadOnlyDictionary<string, Style> Tokens = new Dictionary<string, Style>
    {
        ["board.column.border"] = new Style(Color.Grey),
        ["board.column.border.focused"] = new Style(Color.Aqua),

        ["board.column.status.blocked"] = new Style(Color.Red),
        ["board.column.status.blocked.focused"] = new Style(Color.Red, decoration: Decoration.Bold),
        ["board.column.status.deferred"] = new Style(Color.Yellow),
        ["board.column.status.deferred.focused"] = new Style(Color.Yellow, decoration: Decoration.Bold),
        ["board.column.status.ready"] = new Style(Color.Green),
        ["board.column.status.ready.focused"] = new Style(Color.Green, decoration: Decoration.Bold),
        ["board.column.status.inprogress"] = new Style(Color.Blue),
        ["board.column.status.inprogress.focused"] = new Style(Color.Blue, decoration: Decoration.Bold),
        ["board.column.status.closed"] = new Style(Color.Grey, decoration: Decoration.Dim),
        ["board.column.status.closed.focused"] = new Style(Color.Grey, decoration: Decoration.Bold),

        ["badge.priority.p0"] = new Style(Color.Red),
        ["badge.priority.p1"] = new Style(Color.Orange1),
        ["badge.priority.p2"] = new Style(Color.Yellow),
        ["badge.priority.p3"] = new Style(Color.Grey70),
        ["badge.priority.p4"] = new Style(Color.Grey50),

        ["badge.type.bug"] = new Style(Color.Red3),
        ["badge.type.feature"] = new Style(Color.SkyBlue1),
        ["badge.type.task"] = new Style(Color.Grey70),
        ["badge.type.epic"] = new Style(Color.MediumPurple1),
        ["badge.type.chore"] = new Style(Color.Grey50),
        ["badge.type.docs"] = new Style(Color.Grey58),
        ["badge.type.question"] = new Style(Color.Gold1),

        ["status.glyph.closed"] = new Style(Color.Green),
        ["status.glyph.in_progress"] = new Style(Color.Yellow),
        ["status.glyph.open"] = new Style(Color.Grey70),
        ["status.glyph.deferred"] = new Style(Color.Grey50),
        ["status.glyph.blocked"] = new Style(Color.Red),

        ["selection.highlight"] = new Style(Color.Default, Color.Grey35),

        ["agents.list.header"] = new Style(decoration: Decoration.Bold | Decoration.Dim),
        ["agents.list.name"] = new Style(Color.Aqua),
        ["agents.list.harness"] = new Style(Color.SkyBlue1, decoration: Decoration.Dim),
        ["agents.list.role"] = new Style(Color.Grey70),
        ["agents.list.role.orchestrator"] = new Style(Color.MediumPurple1),
        ["agents.list.role.planner"] = new Style(Color.Gold1),
        ["agents.list.role.implementer"] = new Style(Color.Green),
        ["agents.list.role.reviewer"] = new Style(Color.Orange1),
        ["agents.list.role.researcher"] = new Style(Color.Blue),
        ["agents.list.age"] = new Style(Color.Grey58, decoration: Decoration.Dim),
        ["agents.list.presence"] = new Style(Color.Grey70),
        ["agents.list.presence.online"] = new Style(Color.Green),
        ["agents.list.presence.unreachable"] = new Style(Color.Yellow),
        ["agents.list.presence.offline"] = new Style(Color.Grey50, decoration: Decoration.Dim),

        ["agents.popover.label"] = new Style(Color.Grey58, decoration: Decoration.Dim),
        ["agents.popover.section.title"] = new Style(decoration: Decoration.Bold),
        ["agents.popover.row.empty"] = new Style(Color.Grey58, decoration: Decoration.Dim),
        ["agents.popover.row.unread"] = new Style(Color.White, decoration: Decoration.Bold),
        ["agents.popover.row.dimmed"] = new Style(Color.Grey, decoration: Decoration.Dim),
        ["agents.popover.show-more"] = new Style(Color.Grey58, decoration: Decoration.Dim),

        ["mail.list.header"] = new Style(Color.Grey70, decoration: Decoration.Bold),
        ["mail.list.subject"] = new Style(Color.White),
        ["mail.list.from"] = new Style(Color.Grey70),
        ["mail.list.to"] = new Style(Color.Grey70),
        ["mail.list.messages"] = new Style(Color.Grey58, decoration: Decoration.Dim),
        ["mail.list.age"] = new Style(Color.Grey58, decoration: Decoration.Dim),

        ["memory.list.header"] = new Style(Color.Grey70, decoration: Decoration.Bold),
        ["memory.list.kind"] = new Style(Color.MediumPurple1),
        ["memory.list.type"] = new Style(Color.SkyBlue1),
        ["memory.list.tags"] = new Style(Color.Grey70),
        ["memory.list.age"] = new Style(Color.Grey58, decoration: Decoration.Dim),

        ["detail.section.header"] = new Style(decoration: Decoration.Bold),
        ["detail.section.border"] = new Style(Color.Grey),

        ["toast.info.border"] = new Style(Color.SkyBlue1),
        ["toast.success.border"] = new Style(Color.Green),
        ["toast.warn.border"] = new Style(Color.Orange1),
        ["toast.error.border"] = new Style(Color.Red),

        ["form.button.primary"] = new Style(Color.Black, Color.Green),
        ["form.button.primary.focused"] = new Style(Color.Black, Color.Green, Decoration.Bold),
        ["form.button.secondary"] = new Style(Color.Black, Color.Grey70),
        ["form.button.secondary.focused"] = new Style(Color.Black, Color.Grey70, Decoration.Bold),
        ["form.button.danger"] = new Style(Color.White, Color.Red),
        ["form.button.danger.focused"] = new Style(Color.White, Color.Red, Decoration.Bold),

        ["footer.key"] = new Style(Color.Grey58, decoration: Decoration.Dim),
        ["footer.action"] = new Style(Color.Grey70),
        ["footer.identity"] = new Style(Color.Aqua, decoration: Decoration.Bold),

        ["footer.daemon.ready"] = new Style(Color.Green),
        ["footer.daemon.standby"] = new Style(Color.Grey58, decoration: Decoration.Dim),
        ["footer.daemon.degraded"] = new Style(Color.Red, decoration: Decoration.Bold)
    };
}
