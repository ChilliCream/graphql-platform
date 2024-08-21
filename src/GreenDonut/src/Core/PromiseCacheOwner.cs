using Microsoft.Extensions.ObjectPool;

namespace GreenDonut;

/// <summary>
/// Owner of <see cref="PromiseCache"/> that is responsible for returning the rented
/// <see cref="PromiseCache"/> appropriately to the <see cref="ObjectPool{TaskCache}"/>.
/// </summary>
public sealed class PromiseCacheOwner : IDisposable
{
    private readonly CancellationTokenSource _cts = new();
    private readonly ObjectPool<PromiseCache> _pool;
    private readonly PromiseCache _cache;
    private bool _disposed;

    /// <summary>
    /// Rents a new cache from <see cref="PromiseCachePool.Shared"/>.
    /// </summary>
    public PromiseCacheOwner()
    {
        _pool = PromiseCachePool.Shared;
        _cache = PromiseCachePool.Shared.Get();
        CancellationToken = _cts.Token;
    }

    /// <summary>
    /// Rents a new cache from the given <paramref name="pool"/>.
    /// </summary>
    public PromiseCacheOwner(ObjectPool<PromiseCache> pool)
    {
        _pool = pool ?? throw new ArgumentNullException(nameof(pool));
        _cache = pool.Get();
        CancellationToken = _cts.Token;
    }

    /// <summary>
    /// The task cancellation token that shall be used for a DataLoader session with this cache.
    /// The cancellation will be signaled when the cache is returned to its pool.
    /// </summary>
    public CancellationToken CancellationToken { get; }

    /// <summary>
    /// Gets the rented cache.
    /// </summary>
    public IPromiseCache Cache => _cache;

    /// <summary>
    /// Returns the rented cache back to the <see cref="ObjectPool{TaskCache}"/>.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _cts.Cancel();
            _cts.Dispose();
            _pool.Return(_cache);
            _disposed = true;
        }
    }
}
