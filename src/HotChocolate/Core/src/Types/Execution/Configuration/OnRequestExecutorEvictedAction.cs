namespace HotChocolate.Execution.Configuration;

public readonly struct OnRequestExecutorEvictedAction
{
    public OnRequestExecutorEvictedAction(OnRequestExecutorEvicted evicted)
    {
        Evicted = evicted ?? throw new ArgumentNullException(nameof(evicted));
        EvictedAsync = null;
    }

    public OnRequestExecutorEvictedAction(OnRequestExecutorEvictedAsync evictedAsync)
    {
        Evicted = null;
        EvictedAsync = evictedAsync ?? throw new ArgumentNullException(nameof(evictedAsync));
    }

    public OnRequestExecutorEvicted? Evicted { get; }

    public OnRequestExecutorEvictedAsync? EvictedAsync { get; }
}

public delegate void OnRequestExecutorEvicted(
    IRequestExecutor executor);

public delegate ValueTask OnRequestExecutorEvictedAsync(
    IRequestExecutor executor,
    CancellationToken cancellationToken);
