using HotChocolate.Language;
using SnapshotValueFormatters = CookieCrumble.HotChocolate.Language.Formatters.SnapshotValueFormatters;

namespace CookieCrumble.HotChocolate;

public static class SnapshotExtensions
{
    public static void MatchSnapshot(
        this ISyntaxNode? value,
        string? postFix = null)
        => Snapshot.Match(
            value,
            postFix,
            extension: ".graphql",
            formatter: SnapshotValueFormatters.GraphQLSyntaxNode);
}
