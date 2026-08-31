namespace ChilliCream.Nitro.CommandLine.Tui.Theming;

/// <summary>
/// Looks up styles and colors by dotted token name (for example
/// <c>board.column.border.focused</c> or <c>badge.priority.p0</c>). One
/// built-in theme is registered; unknown tokens resolve to
/// <see cref="Style.Plain"/> so callers never need to null-check.
/// </summary>
internal static class ThemeTokens
{
    private static readonly IReadOnlyDictionary<string, Style> s_active = DefaultTheme.Tokens;

    /// <summary>
    /// Resolves <paramref name="token"/> to a style, or <see cref="Style.Plain"/>
    /// if the token is not registered.
    /// </summary>
    public static Style GetStyle(string token)
        => s_active.TryGetValue(token, out var style) ? style : Style.Plain;

    /// <summary>
    /// Resolves <paramref name="token"/> to a foreground color, or
    /// <see cref="Color.Default"/> if the token is not registered.
    /// </summary>
    public static Color GetColor(string token)
        => GetStyle(token).Foreground;
}
