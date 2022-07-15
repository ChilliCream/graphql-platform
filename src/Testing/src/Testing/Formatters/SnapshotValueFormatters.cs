namespace Testing;

/// <summary>
/// Provides access to well-known snapshot value formatters.
/// </summary>
public static class SnapshotValueFormatters
{
    public static ISnapshotValueFormatter GraphQL { get; } =
        new GraphQLSnapshotValueFormatter();

    public static ISnapshotValueFormatter Json { get; } =
        new JsonSnapshotValueFormatter();

    public static ISnapshotValueFormatter PlainText { get; } =
        new PlainTextSnapshotValueFormatter();
}
