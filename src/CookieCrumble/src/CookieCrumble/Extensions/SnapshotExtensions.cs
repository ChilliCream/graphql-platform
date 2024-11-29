using CookieCrumble.Formatters;

namespace CookieCrumble;

public static class SnapshotExtensions
{
    public static void MatchInlineSnapshot(
        this object? value,
        string snapshot,
        ISnapshotValueFormatter? formatter = null)
        => Snapshot.Create().Add(value, formatter: formatter).MatchInline(snapshot);

    public static void MatchSnapshot(this Snapshot value)
        => value.Match();

    public static void MatchSnapshot(
        this object? value,
        object? postFix = null,
        string? extension = null,
        ISnapshotValueFormatter? formatter = null)
        => Snapshot.Match(value, postFix?.ToString(), extension, formatter);

    public static void MatchMarkdownSnapshot(
        this object? value,
        object? postFix = null,
        string? extension = null,
        ISnapshotValueFormatter? formatter = null)
        => Snapshot.Create(postFix?.ToString(), extension).Add(value, formatter: formatter).MatchMarkdown();

    public static void MatchMarkdownSnapshot(this Snapshot value)
        => value.MatchMarkdown();
}
