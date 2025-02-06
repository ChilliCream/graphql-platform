using System.Collections;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types;

namespace HotChocolate.Skimmed;

public sealed class ReadOnlyEnumValueCollection
    : IEnumValueCollection
    , IReadOnlyEnumValueCollection
{
    private readonly OrderedDictionary<string, EnumValue> _fields;

    private ReadOnlyEnumValueCollection(IEnumerable<EnumValue> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        _fields = values.ToOrderedDictionary(t => t.Name);
    }

    public int Count => _fields.Count;

    public bool IsReadOnly => true;

    public EnumValue this[string name] => _fields[name];

    IReadOnlyEnumValue IReadOnlyEnumValueCollection.this[string name]
        => this[name];

    public bool TryGetValue(string name, [NotNullWhen(true)] out EnumValue? value)
        => _fields.TryGetValue(name, out value);

    bool IReadOnlyEnumValueCollection.TryGetValue(
        string name,
        [NotNullWhen(true)] out IReadOnlyEnumValue? value)
    {
        if (_fields.TryGetValue(name, out var enumValue))
        {
            value = enumValue;
            return true;
        }

        value = null;
        return false;
    }

    public void Insert(int index, EnumValue value)
        => ThrowReadOnly();

    public bool Remove(string name)
    {
        ThrowReadOnly();
        return false;
    }

    public void RemoveAt(int index)
        => ThrowReadOnly();

    public void Add(EnumValue value) => ThrowReadOnly();

    public bool Remove(EnumValue value)
    {
        ThrowReadOnly();
        return false;
    }

    public void Clear() => ThrowReadOnly();

    [DoesNotReturn]
    private static void ThrowReadOnly()
        => throw new NotSupportedException("Collection is read-only.");

    public bool ContainsName(string name)
        => _fields.ContainsKey(name);

    public int IndexOf(EnumValue value)
    {
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        return IndexOf(value.Name);
    }

    public int IndexOf(string name)
        => _fields.IndexOf(name);

    public bool Contains(EnumValue value)
    {
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        if (_fields.TryGetValue(value.Name, out var itemToDelete)
            && ReferenceEquals(value, itemToDelete))
        {
            return true;
        }

        return false;
    }

    public void CopyTo(EnumValue[] array, int arrayIndex)
    {
        foreach (var item in _fields)
        {
            array[arrayIndex++] = item.Value;
        }
    }

    public IEnumerator<EnumValue> GetEnumerator()
        => _fields.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    IEnumerator<IReadOnlyEnumValue> IEnumerable<IReadOnlyEnumValue>.GetEnumerator()
        => GetEnumerator();

    public static ReadOnlyEnumValueCollection Empty { get; } = new(Array.Empty<EnumValue>());

    public static ReadOnlyEnumValueCollection From(IEnumerable<EnumValue> values)
        => new(values);
}
