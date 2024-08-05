namespace HotChocolate.Subscriptions.Postgres;

internal sealed class CopyOnWriteList<T> where T : class
{
    private readonly object _sync = new();

    private T[] _items = [];

    public void Add(T item)
    {
        lock (_sync)
        {
            var items = new T[_items.Length + 1];
            Array.Copy(_items, items, _items.Length);
            items[^1] = item;
            _items = items;
        }
    }

    public void Remove(T item)
    {
        lock (_sync)
        {
            var index = Array.IndexOf(_items, item);
            if (index < 0)
            {
                // Item not found
                return;
            }

            var items = new T[_items.Length - 1];
            // Copy items before the one to remove
            Array.Copy(_items, 0, items, 0, index);
            // Copy items after the one to remove
            Array.Copy(_items, index + 1, items, index, _items.Length - index - 1);
            _items = items;
        }
    }

    // we do not return a IReadOnlyList<T> because array access is faster and it's an internal
    // collection
    public T[] Items => _items;
}
