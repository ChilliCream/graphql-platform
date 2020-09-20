using System.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;

namespace GreenDonut
{
    public class Batch<TKey, TValue>
        where TKey : notnull
    {
        private readonly object _sync = new object();
        private readonly Dictionary<TKey, TaskCompletionSource<TValue>> _items =
            new Dictionary<TKey, TaskCompletionSource<TValue>>();
        private bool _hasDispatched;

        public IReadOnlyList<TKey> Keys => _items.Keys.ToArray();

        public int Size => _items.Count;

        public bool TryGetOrCreate(
            TKey key,
            [NotNullWhen(true)] out TaskCompletionSource<TValue>? promise)
        {
            if (!_hasDispatched)
            {
                lock (_sync)
                {
                    if (!_hasDispatched)
                    {
                        if (_items.ContainsKey(key))
                        {
                            promise = _items[key];
                        }
                        else
                        {
                            promise = new TaskCompletionSource<TValue>(
                                TaskCreationOptions.RunContinuationsAsynchronously);
                            _items.Add(key, promise);
                        }

                        return true;
                    }
                }
            }

            promise = null;
            return false;
        }

        public TaskCompletionSource<TValue> Get(TKey key)
        {
            return _items[key];
        }

        public ValueTask StartDispatchingAsync(Func<ValueTask> dispatch)
        {
            bool execute = false;

            if (!_hasDispatched)
            {
                lock (_sync)
                {
                    if (!_hasDispatched)
                    {
                        execute = _hasDispatched = true;
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
