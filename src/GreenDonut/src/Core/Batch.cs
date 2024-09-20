namespace GreenDonut;

internal class Batch<TKey> where TKey : notnull
{
    private readonly List<TKey> _keys = [];
    private readonly Dictionary<TKey, IPromise> _items = new();

    public bool IsScheduled { get; set; }

    public int Size => _keys.Count;

    public IReadOnlyList<TKey> Keys => _keys;

    public Promise<TValue> GetOrCreatePromise<TValue>(TKey key, bool allowCachePropagation)
    {
        if(_items.TryGetValue(key, out var value))
        {
            return (Promise<TValue>)value;
        }

        var promise = Promise<TValue>.Create(!allowCachePropagation);

        _keys.Add(key);
        _items.Add(key, promise);

        return promise;
    }

    public Promise<TValue> GetPromise<TValue>(TKey key)
        => (Promise<TValue>)_items[key];

    internal void ClearUnsafe()
    {
        _keys.Clear();
        _items.Clear();
        IsScheduled = false;
    }
}
