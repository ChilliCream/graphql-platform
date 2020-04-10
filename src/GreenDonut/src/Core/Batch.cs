using System.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GreenDonut
{
    public class Batch<TKey, TValue>
        where TKey : notnull
    {
        private Dictionary<TKey, TaskCompletionSource<TValue>> _items =
            new Dictionary<TKey, TaskCompletionSource<TValue>>();

        public bool HasDispatched { get; private set; }

        public IReadOnlyList<TKey> Keys => _items.Keys.ToArray();

        public int Size => _items.Count;

        public void Add(TKey key, TaskCompletionSource<TValue> value)
        {
            ThrowIfDispatched();

            _items.Add(key, value);
        }

        public TaskCompletionSource<TValue> Get(TKey key)
        {
            return _items[key];
        }

        public void StartDispatching()
        {
            ThrowIfDispatched();

            HasDispatched = true;
        }

        private void ThrowIfDispatched()
        {
            if (HasDispatched)
            {
                throw new InvalidOperationException("This batch has already been dispatched.");
            }
        }
    }
}
