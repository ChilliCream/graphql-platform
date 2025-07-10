using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Types.Mutable;

public abstract class FieldDefinitionCollection<TField> : IList<TField> where TField : IFieldDefinition
{
    private readonly OrderedDictionary<string, TField> _fields = [];

    protected FieldDefinitionCollection(ITypeSystemMember declaringMember)
    {
        ArgumentNullException.ThrowIfNull(declaringMember);
        DeclaringMember = declaringMember;
    }

    public ITypeSystemMember DeclaringMember { get; }

    public int Count => _fields.Count;

    public bool IsReadOnly => false;

    public TField this[string name]
    {
        get
        {
            ArgumentException.ThrowIfNullOrEmpty(name);

            return _fields[name];
        }
    }

    public TField this[int index]
    {
        get
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);

            return _fields.GetAt(index).Value;
        }

        set
        {
            ArgumentNullException.ThrowIfNull(value);
            ArgumentOutOfRangeException.ThrowIfNegative(index);

            RemoveAt(index);
            Insert(index, value);
        }
    }

    public bool TryGetField(string name, [NotNullWhen(true)] out TField? field)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        return _fields.TryGetValue(name, out field);
    }

    public void Insert(int index, TField field)
    {
        ArgumentNullException.ThrowIfNull(field);
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        _fields.Insert(index, field.Name, field);
    }

    public void Add(TField item)
    {
        ArgumentNullException.ThrowIfNull(item);

        _fields.Add(item.Name, item);
    }

    public bool Remove(TField item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (_fields.TryGetValue(item.Name, out var itemToDelete) &&
            ReferenceEquals(item, itemToDelete))
        {
            _fields.Remove(item.Name);
            return true;
        }

        return false;
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

    public void Clear() => _fields.Clear();

    public bool ContainsName(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        return _fields.ContainsKey(name);
    }

    public bool Contains(TField item)
    {
        ArgumentNullException.ThrowIfNull(item);

        return _fields.TryGetValue(item.Name, out var itemToDelete)
            && ReferenceEquals(item, itemToDelete);
    }

    public int IndexOf(TField field)
    {
        ArgumentNullException.ThrowIfNull(field);

        return _fields.IndexOf(field.Name);
    }

    public int IndexOf(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        return _fields.IndexOf(name);
    }

    public void CopyTo(TField[] array, int arrayIndex)
    {
        ArgumentNullException.ThrowIfNull(array);
        ArgumentOutOfRangeException.ThrowIfNegative(arrayIndex);

        foreach (var item in _fields)
        {
            array[arrayIndex++] = item.Value;
        }
    }

    public IEnumerable<TField> AsEnumerable()
        => _fields.Values;

    public IEnumerator<TField> GetEnumerator()
        => _fields.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}
