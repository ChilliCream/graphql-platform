using System.Collections;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Skimmed;

public sealed class ReadOnlyEnumValueCollection : IEnumValueCollection
{
    private readonly FrozenDictionary<string, EnumValue> _fields;

    private ReadOnlyEnumValueCollection(IEnumerable<EnumValue> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        _fields = values.ToFrozenDictionary(t => t.Name, StringComparer.Ordinal);
    }

    public int Count => _fields.Count;

    public bool IsReadOnly => true;

    public EnumValue this[string name] => _fields[name];

    public bool TryGetValue(string name, [NotNullWhen(true)] out EnumValue? field)
        => _fields.TryGetValue(name, out field);

    public void Add(EnumValue item) => ThrowReadOnly();

    public bool Remove(EnumValue item)
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

    public bool Contains(EnumValue item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (_fields.TryGetValue(item.Name, out var itemToDelete)
            && ReferenceEquals(item, itemToDelete))
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
        => _fields.Values.OrderBy(t => t.Name, StringComparer.Ordinal).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public static ReadOnlyEnumValueCollection Empty { get; } = new(Array.Empty<EnumValue>());

    public static ReadOnlyEnumValueCollection From(IEnumerable<EnumValue> values)
        => new(values);
}
