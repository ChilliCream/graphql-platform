using System.Collections;

namespace HotChocolate.Skimmed.Utilities;

internal sealed class TwoItemDictionary<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>
{
    private readonly KeyValuePair<TKey, TValue> _item1;
    private readonly KeyValuePair<TKey, TValue> _item2;
    private readonly EqualityComparer<TKey> _equalityComparer;

    public TwoItemDictionary(TKey key1, TValue value1, TKey key2, TValue value2)
    {
        _item1 = new KeyValuePair<TKey, TValue>(key1, value1);
        _item2 = new KeyValuePair<TKey, TValue>(key2, value2);
        _equalityComparer = EqualityComparer<TKey>.Default;

        if (_equalityComparer.Equals(_item1.Key, _item2.Key))
        {
            throw new ArgumentException("Keys must be unique");
        }
    }

    public TValue this[TKey key]
    {
        get
        {
            if (_equalityComparer.Equals(_item1.Key, key))
            {
                return _item1.Value;
            }

            if (_equalityComparer.Equals(_item2.Key, key))
            {
                return _item2.Value;
            }

            throw new KeyNotFoundException();
        }
    }

    public int Count => 2;

    public IEnumerable<TKey> Keys => [_item1.Key, _item2.Key];

    public IEnumerable<TValue> Values => [_item1.Value, _item2.Value];

    public bool ContainsKey(TKey key)
        => _equalityComparer.Equals(_item1.Key, key)
            || _equalityComparer.Equals(_item2.Key, key);

    public bool TryGetValue(TKey key, out TValue value)
    {
        if (_equalityComparer.Equals(_item1.Key, key))
        {
            value = _item1.Value;
            return true;
        }

        if (_equalityComparer.Equals(_item2.Key, key))
        {
            value = _item2.Value;
            return true;
        }

        value = default!;
        return false;
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        yield return _item1;
        yield return _item2;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
