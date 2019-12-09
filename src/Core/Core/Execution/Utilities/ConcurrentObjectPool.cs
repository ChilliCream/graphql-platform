using System;
using System.Collections.Concurrent;

namespace HotChocolate.Execution
{
    internal sealed class ConcurrentObjectPool<T>
    {
        private readonly ConcurrentBag<T> _concurrentBag = new ConcurrentBag<T>();
        private readonly Func<T> _create;
        private readonly Action<T> _clean;
        private readonly int _capacity;

        public ConcurrentObjectPool(Func<T> create, Action<T> clean, int capacity)
        {
            _clean = clean;
            _capacity = capacity;
        }

        public T Rent()
        {
            if (_concurrentBag.TryTake(out T item))
            {
                return item;
            }
            return _create();
        }

        public void Return(T rented)
        {
            // note: we do not mind if there are a view more objects in our cache.
            if (_concurrentBag.Count < _capacity)
            {
                _concurrentBag.Add(rented);
            }
        }
    }
}
