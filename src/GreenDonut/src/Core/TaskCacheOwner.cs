using System;
using Microsoft.Extensions.ObjectPool;

namespace GreenDonut
{
    public sealed class TaskCacheOwner : IDisposable
    {
        private readonly ObjectPool<TaskCache> _pool;
        private readonly TaskCache _cache;
        private bool _disposed;

        public TaskCacheOwner()
        {
            _pool = TaskCachePool.Shared;
            _cache = TaskCachePool.Shared.Get();
        }

        public TaskCacheOwner(ObjectPool<TaskCache> pool)
        {
            _pool = pool ?? throw new ArgumentNullException(nameof(pool));
            _cache = pool.Get();
        }

        public ITaskCache Cache => _cache;

        public void Dispose()
        {
            if (!_disposed)
            {
                _pool.Return(_cache);
                _disposed = true;
            }
        }
    }
}
