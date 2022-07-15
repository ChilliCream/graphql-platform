namespace Testing;

public static class SnapshotValueSerializers
{
    public static ISnapshotValueSerializer GraphQL { get; } =
        new GraphQLSnapshotValueSerializer();

    public static ISnapshotValueSerializer Json { get; } =
        new JsonSnapshotValueSerializer();

    public static ISnapshotValueSerializer PlainText { get; } =
        new PlainTextSnapshotValueSerializer();
}
