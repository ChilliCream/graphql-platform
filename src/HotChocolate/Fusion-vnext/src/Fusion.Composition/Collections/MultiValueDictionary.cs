namespace HotChocolate.Fusion.Collections;

internal sealed class MultiValueDictionary<TKey, TValue>
    : Dictionary<TKey, IList<TValue>> where TKey : notnull
{
    public void Add(TKey key, TValue value)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (!TryGetValue(key, out var list))
        {
            list = [];
            Add(key, list);
        }

        list.Add(value);
    }

    public void Remove(TKey key, TValue value)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (TryGetValue(key, out var list))
        {
            list.Remove(value);
        }
    }
}
