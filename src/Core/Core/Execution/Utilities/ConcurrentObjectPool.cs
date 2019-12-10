using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution
{
    internal sealed class ConcurrentObjectPool<T> where T : class, new()
    {
        private readonly Microsoft.Extensions.ObjectPool.DefaultObjectPool<T> _concurrentBag =
            new Microsoft.Extensions.ObjectPool.DefaultObjectPool<T>(
                new DefaultPooledObjectPolicy<T>());
        private readonly Func<T> _create;
        private readonly Action<T> _clean;
        private readonly int _size;

        public ConcurrentObjectPool(Func<T> create, Action<T> clean, int size)
        {
            _create = create;
            _clean = clean;
            _size = size;
        }

        public T Rent()
        {
            return _concurrentBag.Get();
        }

        public void Return(T rented)
        {
            // note: we do not mind if there are a view more objects in our cache.
            _clean(rented);
            _concurrentBag.Return(rented);
        }
    }
}
