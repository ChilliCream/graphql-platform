namespace HotChocolate.Execution;

public static class SnapshotHelpers
{
    public static DisposableSnapshot StartResultSnapshot(string? postFix = null)
        => Snapshot.Start(postFix: postFix, extension: ".json");
}
