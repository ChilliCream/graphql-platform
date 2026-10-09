namespace HotChocolate.Fusion.Aspire.Nitro;

internal interface INitroStageUpdateClient
{
    Task<NitroStageSubscription> SubscribeAsync(
        NitroConnection connection,
        string apiId,
        string stage,
        CancellationToken cancellationToken);

    Task<NitroStageSnapshot?> GetLatestSnapshotAsync(
        NitroConnection connection,
        string apiId,
        string stage,
        CancellationToken cancellationToken);
}

internal abstract class NitroStageSubscription : IAsyncDisposable
{
    public abstract IAsyncEnumerable<NitroStageChange> ReadChangesAsync(
        CancellationToken cancellationToken);

    public abstract ValueTask DisposeAsync();
}
