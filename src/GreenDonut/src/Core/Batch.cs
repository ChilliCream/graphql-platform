using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.ObjectPool;

namespace GreenDonut
{
    internal class Batch<TKey> where TKey : notnull
    {
        private readonly List<TKey> _keys = new();
        private readonly Dictionary<TKey, object> _items = new();

        public int Size => _keys.Count;

        public IReadOnlyList<TKey> Keys => _keys;

        public TaskCompletionSource<TValue> GetOrCreatePromise<TValue>(TKey key)
        {
            if(_items.TryGetValue(key, out var value))
            {
                return (TaskCompletionSource<TValue>)value;
            }

            var promise = new TaskCompletionSource<TValue>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            _keys.Add(key);
            _items.Add(key, promise);

            return promise;
        }

        public TaskCompletionSource<TValue> GetPromise<TValue>(TKey key)
            => (TaskCompletionSource<TValue>)_items[key];

        internal void ClearUnsafe()
        {
            _keys.Clear();
            _items.Clear();
        }
    }

    internal static class BatchPool<TKey> where TKey : notnull
    {
        public static ObjectPool<Batch<TKey>> Shared { get; } = Create();

        private static ObjectPool<Batch<TKey>> Create()
            => new DefaultObjectPool<Batch<TKey>>(
                new BatchPooledObjectPolicy<TKey>(),
                Environment.ProcessorCount * 2);
    }

    internal class BatchPooledObjectPolicy<TKey>
        : PooledObjectPolicy<Batch<TKey>>
        where TKey : notnull
    {
        public override Batch<TKey> Create() => new();

        public override bool Return(Batch<TKey> obj)
        {
            obj.ClearUnsafe();
            return true;
        }
    }
}
