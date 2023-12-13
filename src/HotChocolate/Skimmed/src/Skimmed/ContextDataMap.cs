using System.Collections;
using System.Collections.Immutable;

namespace HotChocolate.Skimmed;

internal sealed class ContextDataMap : IDictionary<string, object?>
{
    private static readonly ICollection<string> _emptyKeys = ImmutableList<string>.Empty;
    private static readonly ICollection<object?> _emptyValues = ImmutableList<object?>.Empty;
    private Dictionary<string, object?>? _dict;
    private int _count;

    public object? this[string key]
    {
        get
        {
            if (_dict is null)
            {
                throw new KeyNotFoundException($"The key {key} does not exist.");
            }

            return _dict[key];
        }
        set
        {
            var dict = _dict ??= new Dictionary<string, object?>();
            dict[key] = value;
            _count = dict.Count;
        }
    }

    public ICollection<string> Keys => _dict?.Keys ?? _emptyKeys;

    public ICollection<object?> Values => _dict?.Values ?? _emptyValues;

    public int Count => _count;

    public bool IsReadOnly => false;

    public void Add(string key, object? value)
    {
        var dict = _dict ??= new Dictionary<string, object?>();
        dict.Add(key, value);
        _count = dict.Count;
    }

    void ICollection<KeyValuePair<string, object?>>.Add(KeyValuePair<string, object?> item)
    {
        var dict = _dict ??= new Dictionary<string, object?>();
        dict.Add(item.Key, item.Value);
        _count = dict.Count;
    }

    public bool Remove(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return _dict?.Remove(key) ?? false;
    }

    bool ICollection<KeyValuePair<string, object?>>.Remove(KeyValuePair<string, object?> item)
    {
        ArgumentNullException.ThrowIfNull(item.Key);
        return _dict?.Remove(item.Key) ?? false;
    }

    public bool TryGetValue(string key, out object? value)
    {
        if (_dict?.TryGetValue(key, out value) ?? false)
        {
            return true;
        }

        value = null;
        return false;
    }

    public bool Contains(KeyValuePair<string, object?> item)
        => (_dict?.TryGetValue(item.Key, out var value) ?? false) &&
            Equals(item.Value, value);

    public bool ContainsKey(string key)
        => _dict?.ContainsKey(key) ?? false;

    void ICollection<KeyValuePair<string, object?>>.CopyTo(KeyValuePair<string, object?>[] array, int arrayIndex)
        => ((ICollection<KeyValuePair<string, object?>>?)_dict)?.CopyTo(array, arrayIndex);

    public void Clear()
        => _dict?.Clear();

    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
        => _dict?.GetEnumerator() ??
            (IEnumerator<KeyValuePair<string, object?>>)
            ImmutableDictionary<string, object?>.Empty.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}