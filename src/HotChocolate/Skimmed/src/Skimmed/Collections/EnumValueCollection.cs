using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Skimmed;

public sealed class EnumValueCollection : IEnumValueCollection
{
    private readonly OrderedDictionary<string, EnumValue> _fields = new();

    public int Count => _fields.Count;

    public bool IsReadOnly => false;

    public EnumValue this[string name] => _fields[name];

    public bool TryGetValue(string name, [NotNullWhen(true)] out EnumValue? value)
        => _fields.TryGetValue(name, out value);

    public void Insert(int index, EnumValue value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        _fields.Insert(index, value.Name, value);
    }

    public bool Remove(string name)
        => _fields.Remove(name);

    public void RemoveAt(int index)
        => _fields.RemoveAt(index);

    public void Add(EnumValue value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        _fields.Add(value.Name, value);
    }

    public bool Remove(EnumValue value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        if (_fields.TryGetValue(value.Name, out var itemToDelete)
            && ReferenceEquals(value, itemToDelete))
        {
            _fields.Remove(value.Name);
            return true;
        }

        return false;
    }

    public void Clear() => _fields.Clear();

    public bool ContainsName(string name)
        => _fields.ContainsKey(name);

    public int IndexOf(EnumValue value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        return IndexOf(value.Name);
    }

    public int IndexOf(string name)
        => _fields.IndexOf(name);

    public bool Contains(EnumValue value)
    {
        if (value is null)
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
}
