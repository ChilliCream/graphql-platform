namespace ChilliCream.Nitro.CommandLine.Tui.Theming;

/// <summary>
/// Looks up styles and colors by dotted token name (for example
/// <c>board.column.border.focused</c> or <c>badge.priority.p0</c>). One
/// built-in theme is registered; unknown tokens resolve to
/// <see cref="Style.Plain"/> so callers never need to null-check.
/// </summary>
internal static class ThemeTokens
{
    private static readonly IReadOnlyDictionary<string, Style> Active = DefaultTheme.Tokens;

    /// <summary>
    /// Resolves <paramref name="token"/> to a style, or <see cref="Style.Plain"/>
    /// if the token is not registered.
    /// </summary>
    public static Style GetStyle(string token)
        => Active.TryGetValue(token, out var style) ? style : Style.Plain;

    /// <summary>
    /// Resolves <paramref name="token"/> to a foreground color, or
    /// <see cref="Color.Default"/> if the token is not registered.
    /// </summary>
    public static Color GetColor(string token)
        => GetStyle(token).Foreground;
}

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

        ["agents.list.name"] = new Style(Color.White),
        ["agents.list.role"] = new Style(Color.Grey70),
        ["agents.list.age"] = new Style(Color.Grey58, decoration: Decoration.Dim),
        ["agents.list.implicit"] = new Style(decoration: Decoration.Dim),

        ["mail.message.unread"] = new Style(decoration: Decoration.Bold),

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
        ["footer.identity"] = new Style(Color.Aqua, decoration: Decoration.Bold)
    };
}
