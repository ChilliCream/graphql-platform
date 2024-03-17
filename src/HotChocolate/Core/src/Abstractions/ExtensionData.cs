using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace HotChocolate;

public sealed class ExtensionData
    : IDictionary<string, object?>
    , IReadOnlyDictionary<string, object?>
{
    private Dictionary<string, object?>? _dict;

    public ExtensionData() { }

    public ExtensionData(ExtensionData extensionData)
    {
        _dict = extensionData._dict;
    }

    public ExtensionData(IReadOnlyDictionary<string, object?> extensionData)
    {
#if NET6_0_OR_GREATER
        _dict = new Dictionary<string, object?>(extensionData);
#else
        _dict = new Dictionary<string, object?>();

        foreach (var item in extensionData)
        {
            _dict.Add(item.Key, item.Value);
        }
#endif
    }

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
        set => Dict()[key] = value;
    }

    object? IReadOnlyDictionary<string, object?>.this[string key] => this[key];

    public ICollection<string> Keys
        => _dict?.Keys ?? (ICollection<string>)ImmutableList<string>.Empty;

    IEnumerable<string> IReadOnlyDictionary<string, object?>.Keys
        => Keys;

    public ICollection<object?> Values
        => _dict?.Values ?? (ICollection<object?>)ImmutableList<object?>.Empty;

    IEnumerable<object?> IReadOnlyDictionary<string, object?>.Values
        => Values;

    public int Count => _dict?.Count ?? 0;

    public bool IsReadOnly => false;

    public void Add(string key, object? value)
        => Dict().Add(key, value);

    public void Add(KeyValuePair<string, object?> item)
        => Dict().Add(item.Key, item.Value);

    public void AddRange(IEnumerable<KeyValuePair<string, object?>> pairs)
    {
        foreach (var pair in pairs)
        {
            Dict().Add(pair.Key, pair.Value);
        }
    }

    public bool Remove(string key)
        => _dict?.Remove(key) ?? false;

    public bool Remove(KeyValuePair<string, object?> item)
        => _dict?.Remove(item.Key) ?? false;

    public bool TryGetValue(string key, out object? value)
    {
        if (_dict?.TryGetValue(key, out value) ?? false)
        {
            return true;
        }

        value = null;
        return false;
    }

    bool IReadOnlyDictionary<string, object?>.TryGetValue(string key, out object? value)
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

    bool IReadOnlyDictionary<string, object?>.ContainsKey(string key)
        => _dict?.ContainsKey(key) ?? false;

    public void CopyTo(KeyValuePair<string, object?>[] array, int arrayIndex)
    {
        if (_dict is not null)
        {
            ((ICollection<KeyValuePair<string, object?>>)_dict).CopyTo(array, arrayIndex);
        }
    }

    public void Clear()
        => _dict?.Clear();

    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
        => _dict?.GetEnumerator() ??
            (IEnumerator<KeyValuePair<string, object?>>)
                ImmutableDictionary<string, object?>.Empty.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Dictionary<string, object?> Dict()
        => _dict ??= new Dictionary<string, object?>();

    internal bool TryGetInnerDictionary(
        [NotNullWhen(true)] out Dictionary<string, object?>? dictionary)
    {
        if (_dict is null)
        {
            dictionary = null;
            return false;
        }

        dictionary = _dict;
        return true;
    }
}
