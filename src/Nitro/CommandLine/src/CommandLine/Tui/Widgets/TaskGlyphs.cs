using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Tui.Theming;

namespace ChilliCream.Nitro.CommandLine.Tui.Widgets;

/// <summary>
/// Maps task status and type codes to their display glyph, independent of any specific row layout.
/// </summary>
internal static class TaskGlyphs
{
    /// <summary>
    /// Returns the status glyph, using a check mark for closed and archived tasks.
    /// Unknown states use the open-task glyph.
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
    /// Returns the unbracketed type code. Unknown types use their uppercase first
    /// character, or <c>?</c> for an empty type.
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
    /// Returns the full type name, exactly as stored: lower case for the well-known types,
    /// unchanged for a custom type.
    /// </summary>
    public static string TypeName(string type) => type;

    /// <summary>
    /// The bracketed type code as a Spectre markup fragment, styled per the
    /// <c>badge.type.*</c> theme token for <paramref name="type"/>.
    /// </summary>
    public static string TypeCodeMarkup(string type)
        => Stylize(ThemeTokens.GetStyle($"badge.type.{type}").ToMarkup(), $"[[{TypeCode(type)}]]");

    private static string Stylize(string styleMarkup, string content) =>
        styleMarkup.Length == 0 ? content : $"[{styleMarkup}]{content}[/]";
}
