using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Tui.Theming;

namespace ChilliCream.Nitro.CommandLine.Tui.Widgets;

/// <summary>
/// Maps task status and type codes to their display glyph, independent of
/// any specific row layout, so the search results list, detail sidebar and
/// dependency sections, and tree explorer render the same glyphs as
/// <see cref="TaskBadge"/> without composing a full badge line.
/// </summary>
internal static class TaskGlyphs
{
    /// <summary>
    /// The plain-text status glyph: closed, in progress, open, deferred,
    /// blocked. Unknown or custom states fall back to the open glyph.
    /// </summary>
    public static string Status(string status) => status switch
    {
        TaskStates.Closed => "✓",
        TaskStates.Archived => "✓",
        TaskStates.InProgress => "●",
        TaskStates.Open => "○",
        TaskStates.Deferred => "⏸",
        TaskStates.Blocked => "⊘",
        _ => "○"
    };

    /// <summary>
    /// The status glyph as a Spectre markup fragment, styled per the
    /// <c>status.glyph.*</c> theme token for <paramref name="status"/>.
    /// </summary>
    public static string StatusMarkup(string status)
        => Stylize(ThemeTokens.GetStyle($"status.glyph.{status}").ToMarkup(), Status(status));

    /// <summary>
    /// The plain-text bracketed type code (for example <c>B</c> for bug,
    /// <c>F</c> for feature). Unknown types fall back to the first letter of
    /// <paramref name="type"/>, or <c>?</c> for an empty type.
    /// </summary>
    public static string TypeCode(string type) => type switch
    {
        TaskTypes.Bug => "B",
        TaskTypes.Feature => "F",
        TaskTypes.Task => "T",
        TaskTypes.Epic => "E",
        TaskTypes.Chore => "C",
        TaskTypes.Docs => "D",
        TaskTypes.Question => "Q",
        _ => type.Length > 0 ? type[..1].ToUpperInvariant() : "?"
    };

    /// <summary>
    /// The bracketed type code as a Spectre markup fragment, styled per the
    /// <c>badge.type.*</c> theme token for <paramref name="type"/>.
    /// </summary>
    public static string TypeCodeMarkup(string type)
        => Stylize(ThemeTokens.GetStyle($"badge.type.{type}").ToMarkup(), $"[[{TypeCode(type)}]]");

    private static string Stylize(string styleMarkup, string content) =>
        styleMarkup.Length == 0 ? content : $"[{styleMarkup}]{content}[/]";
}
