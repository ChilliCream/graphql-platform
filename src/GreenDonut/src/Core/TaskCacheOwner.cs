using System;
using System.Threading;
using Microsoft.Extensions.ObjectPool;

namespace GreenDonut;

/// <summary>
/// Owner of <see cref="TaskCache"/> that is responsible for returning the rented
/// <see cref="TaskCache"/> appropriately to the <see cref="ObjectPool{TaskCache}"/>.
/// </summary>
public sealed class TaskCacheOwner : IDisposable
{
    private readonly CancellationTokenSource _cts = new();
    private readonly ObjectPool<TaskCache> _pool;
    private readonly TaskCache _cache;
    private bool _disposed;

    /// <summary>
    /// Rents a new cache from <see cref="TaskCachePool.Shared"/>.
    /// </summary>
    public TaskCacheOwner()
    {
        _pool = TaskCachePool.Shared;
        _cache = TaskCachePool.Shared.Get();
        CancellationToken = _cts.Token;
    }

    /// <summary>
    /// Rents a new cache from the given <paramref name="pool"/>.
    /// </summary>
    public TaskCacheOwner(ObjectPool<TaskCache> pool)
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
    public ITaskCache Cache => _cache;

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
