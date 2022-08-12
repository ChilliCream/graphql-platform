using System;
using Microsoft.Extensions.ObjectPool;

namespace GreenDonut;

/// <summary>
/// Owner of <see cref="TaskCache"/> that is responsible for returning the rented
/// <see cref="TaskCache"/> appropriately to the <see cref="ObjectPool{TaskCache}"/>.
/// </summary>
public sealed class TaskCacheOwner : IDisposable
{
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
    }

    /// <summary>
    /// Rents a new cache from the given <paramref name="pool"/>.
    /// </summary>
    public TaskCacheOwner(ObjectPool<TaskCache> pool)
    {
        _pool = pool ?? throw new ArgumentNullException(nameof(pool));
        _cache = pool.Get();
    }

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
            _pool.Return(_cache);
            _disposed = true;
        }
    }
}
