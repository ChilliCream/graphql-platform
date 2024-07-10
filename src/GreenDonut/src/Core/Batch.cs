using System.Collections.Generic;
using System.Threading.Tasks;

namespace GreenDonut;

internal class Batch<TKey> where TKey : notnull
{
    private readonly List<TKey> _keys = [];
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