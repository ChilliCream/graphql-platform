using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Fusion.Types.Collections;

internal sealed class OneItemDictionary<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>
{
    private readonly KeyValuePair<TKey, TValue> _item;
    private readonly EqualityComparer<TKey> _equalityComparer;

    public OneItemDictionary(
        TKey key,
        TValue value,
        EqualityComparer<TKey> equalityComparer)
    {
        _item = new KeyValuePair<TKey, TValue>(key, value);
        _equalityComparer = equalityComparer;
    }

    public OneItemDictionary(TKey key, TValue value)
    {
        _item = new KeyValuePair<TKey, TValue>(key, value);
        _equalityComparer = EqualityComparer<TKey>.Default;
    }

    public TValue this[TKey key]
    {
        get
        {
            if (_equalityComparer.Equals(_item.Key, key))
            {
                return _item.Value;
            }

            throw new KeyNotFoundException();
        }
    }

    public int Count => 1;

    public IEnumerable<TKey> Keys => [_item.Key];

    public IEnumerable<TValue> Values => [_item.Value];

    public bool ContainsKey(TKey key) => _equalityComparer.Equals(_item.Key, key);

    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        if (_equalityComparer.Equals(_item.Key, key))
        {
            value = _item.Value;
            return true;
        }

        value = default!;
        return false;
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        yield return _item;
    }

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}
