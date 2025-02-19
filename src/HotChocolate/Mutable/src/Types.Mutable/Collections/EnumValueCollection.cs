using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Types.Mutable;

public sealed class EnumValueCollection
    : IList<MutableEnumValue>
    , IReadOnlyEnumValueCollection
{
    private readonly OrderedDictionary<string, MutableEnumValue> _fields = new();

    public int Count => _fields.Count;

    public bool IsReadOnly => false;

    public MutableEnumValue this[string name] => _fields[name];

    IEnumValue IReadOnlyEnumValueCollection.this[string name]
        => this[name];

    public bool TryGetValue(string name, [NotNullWhen(true)] out MutableEnumValue? value)
        => _fields.TryGetValue(name, out value);

    bool IReadOnlyEnumValueCollection.TryGetValue(string name, [NotNullWhen(true)] out IEnumValue? value)
    {
        if (_fields.TryGetValue(name, out var enumValue))
        {
            value = enumValue;
            return true;
        }

        value = null;
        return false;
    }

    public void Insert(int index, MutableEnumValue value)
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

    public MutableEnumValue this[int index]
    {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }

    public void Add(MutableEnumValue value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        _fields.Add(value.Name, value);
    }

    public bool Remove(MutableEnumValue value)
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

    public int IndexOf(MutableEnumValue value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        return IndexOf(value.Name);
    }

    public int IndexOf(string name)
        => _fields.IndexOf(name);

    public bool Contains(MutableEnumValue value)
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

    public void CopyTo(MutableEnumValue[] array, int arrayIndex)
    {
        foreach (var item in _fields)
        {
            array[arrayIndex++] = item.Value;
        }
    }

    public IEnumerable<MutableEnumValue> AsEnumerable()
        => _fields.Values;

    public IEnumerator<MutableEnumValue> GetEnumerator()
        => _fields.Values.GetEnumerator();

    IEnumerator<IEnumValue> IEnumerable<IEnumValue>.GetEnumerator()
        => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}
