namespace GreenDonut;

/// <summary>
/// The task cache observer allows to subscribe to a task cache
/// and create additional lookups for already cached tasks.
/// </summary>
/// <typeparam name="TValue">
/// The type of the cached value.
/// </typeparam>
public abstract class PromiseCacheObserver<TValue> : IPromiseCacheObserver
{
    private IDisposable? _session;
    private bool _disposed;

    public void Accept(IPromiseCache cache, string? cacheKeyType)
    {
        _session?.Dispose();
        _session = cache.Subscribe<TValue>(OnNext, cacheKeyType);
    }

    /// <summary>
    /// The method is called when a new task is added to the cache.
    /// </summary>
    /// <param name="cache"></param>
    /// <param name="promise"></param>
    public abstract void OnNext(IPromiseCache cache, Promise<TValue> promise);

    public void Dispose()
    {
        if (!_disposed)
        {
            _session?.Dispose();
            _disposed = true;
        }
    }
}
