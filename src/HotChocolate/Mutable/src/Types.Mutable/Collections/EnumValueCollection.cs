using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Types.Mutable;

public sealed class EnumValueCollection
    : IList<MutableEnumValue>
    , IReadOnlyEnumValueCollection
{
    private readonly OrderedDictionary<string, MutableEnumValue> _fields = [];

    public EnumValueCollection(MutableEnumTypeDefinition owner)
    {
        ArgumentNullException.ThrowIfNull(owner);
        DeclaringType = owner;
    }

    public MutableEnumTypeDefinition DeclaringType { get; }

    public int Count => _fields.Count;

    public bool IsReadOnly => false;

    public MutableEnumValue this[int index]
    {
        get
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);

            return _fields.GetAt(index).Value;
        }
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentNullException.ThrowIfNull(value);

            RemoveAt(index);
            Insert(index, value);
            value.DeclaringType = DeclaringType;
        }
    }

    IEnumValue IReadOnlyList<IEnumValue>.this[int index]
    {
        get
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            return _fields.GetAt(index).Value;
        }
    }

    public MutableEnumValue this[string name]
    {
        get
        {
            ArgumentException.ThrowIfNullOrEmpty(name);
            return _fields[name];
        }
    }

    IEnumValue IReadOnlyEnumValueCollection.this[string name]
        => this[name];

    public bool TryGetValue(string name, [NotNullWhen(true)] out MutableEnumValue? value)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        return _fields.TryGetValue(name, out value);
    }

    bool IReadOnlyEnumValueCollection.TryGetValue(string name, [NotNullWhen(true)] out IEnumValue? value)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

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
        ArgumentNullException.ThrowIfNull(value);
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        _fields.Insert(index, value.Name, value);
        value.DeclaringType = DeclaringType;
    }

    public bool Remove(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        return _fields.Remove(name);
    }

    public void RemoveAt(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        _fields.RemoveAt(index);
    }

    public void Add(MutableEnumValue value)
    {
        ArgumentNullException.ThrowIfNull(value);

        _fields.Add(value.Name, value);
        value.DeclaringType = DeclaringType;
    }

    public bool Remove(MutableEnumValue value)
    {
        ArgumentNullException.ThrowIfNull(value);

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
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        return _fields.ContainsKey(name);
    }

    public int IndexOf(MutableEnumValue value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return IndexOf(value.Name);
    }

    public int IndexOf(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        return _fields.IndexOf(name);
    }

    public bool Contains(MutableEnumValue value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return _fields.TryGetValue(value.Name, out var itemToDelete)
            && ReferenceEquals(value, itemToDelete);
    }

    public void CopyTo(MutableEnumValue[] array, int arrayIndex)
    {
        ArgumentNullException.ThrowIfNull(array);
        ArgumentOutOfRangeException.ThrowIfNegative(arrayIndex);

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
