using System.Threading.Channels;

namespace StrawberryShake;

/// <summary>
/// The entity store can be used to access and mutate entities.
/// </summary>
public sealed partial class EntityStore : IEntityStore
{
#if NET9_0_OR_GREATER
    private readonly Lock _sync = new();
#else
    private readonly object _sync = new();
#endif
    private readonly CancellationTokenSource _cts = new();
    private readonly Channel<EntityUpdate> _updates = Channel.CreateUnbounded<EntityUpdate>();
    private readonly EntityUpdateObservable _entityUpdateObservable = new();
    private EntityStoreSnapshot _snapshot = new();
    private bool _disposed;

    public EntityStore()
    {
        BeginProcessEntityUpdates();
    }

    /// <inheritdoc />
    public IEntityStoreSnapshot CurrentSnapshot => _snapshot;

    /// <inheritdoc />
    public void Update(Action<IEntityStoreUpdateSession> action)
    {
        lock (_sync)
        {
            var session = new EntityStoreUpdateSession(_snapshot);

            action(session);

            _snapshot = session.CurrentSnapshot;
            _updates.Writer.TryWrite(new EntityUpdate(_snapshot, session.UpdatedEntityIds));
        }
    }

    /// <inheritdoc />
    public IObservable<EntityUpdate> Watch() => _entityUpdateObservable;

    public void Dispose()
    {
        if (!_disposed)
        {
            _updates.Writer.TryComplete();
            _cts.Cancel();
            _cts.Dispose();
            _disposed = true;
        }
    }
}
