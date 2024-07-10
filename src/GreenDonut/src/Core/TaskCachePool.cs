using System;
using Microsoft.Extensions.ObjectPool;

namespace GreenDonut;

/// <summary>
/// This helper class gives easy access to cache pool factories and the shared cache pool.
/// </summary>
public static class TaskCachePool
{
    /// <summary>
    /// The shared cache pool that is used when no cache was provided through the options.
    /// </summary>
    public static ObjectPool<TaskCache> Shared { get; } = Create(2560);

    /// <summary>
    /// Creates an instance of <see cref="DefaultObjectPool{TaskCache}"/>.
    /// </summary>
    /// <param name="cacheSize">
    /// The size of pooled caches.
    /// </param>
    /// <param name="maximumRetained">
    /// The maximum number of objects to retain in the pool.
    /// </param>
    /// <returns>
    /// Returns the newly created instance of <see cref="DefaultObjectPool{TaskCache}"/>.
    /// </returns>
    public static ObjectPool<TaskCache> Create(int cacheSize = 2560, int? maximumRetained = null)
        => new DefaultObjectPool<TaskCache>(
            new TaskCachePooledObjectPolicy(cacheSize),
            maximumRetained ?? Environment.ProcessorCount * 2);

    /// <summary>
    /// Creates an instance of <see cref="DefaultObjectPool{TaskCache}"/>.
    /// </summary>
    /// <param name="provider">
    /// The Provider to create the <see cref="DefaultObjectPool{TaskCache}"/> instance.
    /// </param>
    /// <param name="cacheSize">
    /// The size of pooled caches.
    /// </param>
    /// <returns>
    /// Returns the newly created instance of <see cref="DefaultObjectPool{TaskCache}"/>.
    /// </returns>
    public static ObjectPool<TaskCache> Create(ObjectPoolProvider provider, int cacheSize = 2560)
        => provider.Create(new TaskCachePooledObjectPolicy(cacheSize));
}