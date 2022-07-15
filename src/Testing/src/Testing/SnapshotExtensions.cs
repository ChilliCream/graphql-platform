using HotChocolate.Language;

namespace Testing;

public static class SnapshotExtensions
{
    public static void MatchSnapshot(
        this object? value,
        string? postFix = null,
        string? extension = null,
        ISnapshotValueSerializer? serializer = null)
        => Snapshot.Match(value, postFix, extension, serializer);

    public static void MatchSnapshot(
        this ISyntaxNode? value,
        string? postFix = null)
        => Snapshot.Match(
            value,
            postFix, 
            extension: ".graphql",
            serializer: SnapshotValueSerializers.GraphQL);
}
