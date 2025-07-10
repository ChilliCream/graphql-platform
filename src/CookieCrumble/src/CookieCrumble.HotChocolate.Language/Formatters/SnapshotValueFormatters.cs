using CookieCrumble.Formatters;

namespace CookieCrumble.HotChocolate.Language.Formatters;

/// <summary>
/// Provides access to well-known snapshot value formatters.
/// </summary>
public static class SnapshotValueFormatters
{
    public static ISnapshotValueFormatter GraphQLSyntaxNode { get; } =
        new GraphQLSyntaxNodeSnapshotValueFormatter();
}
