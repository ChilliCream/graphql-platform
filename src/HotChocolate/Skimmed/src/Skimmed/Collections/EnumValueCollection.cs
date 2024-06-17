using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Skimmed;

public sealed class EnumValueCollection : IEnumValueCollection
{
    private readonly Dictionary<string, EnumValue> _fields = new(StringComparer.Ordinal);

    public int Count => _fields.Count;

    public bool IsReadOnly => false;

    public EnumValue this[string name] => _fields[name];

    public bool TryGetValue(string name, [NotNullWhen(true)] out EnumValue? field)
        => _fields.TryGetValue(name, out field);

    public void Add(EnumValue item)
    {
        ArgumentNullException.ThrowIfNull(item);
        _fields.Add(item.Name, item);
    }

    public bool Remove(EnumValue item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (_fields.TryGetValue(item.Name, out var itemToDelete)
            && ReferenceEquals(item, itemToDelete))
        {
            _fields.Remove(item.Name);
            return true;
        }

        return false;
    }

    public void Clear() => _fields.Clear();

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
}
