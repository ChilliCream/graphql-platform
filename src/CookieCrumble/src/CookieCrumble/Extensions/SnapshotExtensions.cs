using CookieCrumble.Formatters;
using HotChocolate.Language;

namespace CookieCrumble;

public static class SnapshotExtensions
{
    public static void MatchSnapshot(
        this object? value,
        string? postFix = null,
        string? extension = null,
        ISnapshotValueFormatter? formatter = null)
        => Snapshot.Match(value, postFix, extension, formatter);

    public static void MatchSnapshot(
        this ISyntaxNode? value,
        string? postFix = null)
        => Snapshot.Match(
            value,
            postFix,
            extension: ".graphql",
            formatter: SnapshotValueFormatters.GraphQL);
}
