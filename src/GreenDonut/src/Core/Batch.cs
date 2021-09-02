using System.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;

namespace GreenDonut
{
    public class Batch<TKey> where TKey : notnull
    {
        private readonly object _sync = new();
        private readonly Dictionary<TKey, object> _items = new();
        private List<TKey> _keys = new();
        private bool _dispatched;

        public int Size => _keys.Count;

        public IReadOnlyList<TKey> Keys => _keys;

        public bool TryGetOrCreate<TValue>(
            TKey key,
            [NotNullWhen(true)] out TaskCompletionSource<TValue>? promise)
        {
            if (!_dispatched)
            {
                lock (_sync)
                {
                    if (!_dispatched)
                    {
                        if (_items.ContainsKey(key))
                        {
                            promise = (TaskCompletionSource<TValue>)_items[key];
                        }
                        else
                        {
                            promise = new TaskCompletionSource<TValue>(
                                TaskCreationOptions.RunContinuationsAsynchronously);

                            _keys.Add(key);
                            _items.Add(key, promise);
                        }

                        return true;
                    }
                }
            }

            promise = null;
            return false;
        }
        
        public TaskCompletionSource<TValue> GetUnsafe<TValue>(TKey key)
            => (TaskCompletionSource<TValue>)_items[key];

        public ValueTask StartDispatchingAsync(Func<ValueTask> dispatch)
        {
            var execute = false;

            if (!_dispatched)
            {
                lock (_sync)
                {
                    if (!_dispatched)
                    {
                        execute = _dispatched = true;
                    }
                }
            }

            if (execute)
            {
                return dispatch();
            }

            return default;
        }
    }
}
