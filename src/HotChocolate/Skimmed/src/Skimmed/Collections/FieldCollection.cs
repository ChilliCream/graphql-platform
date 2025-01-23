using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Skimmed;

public abstract class FieldDefinitionCollection<TField>
    : IFieldDefinitionCollection<TField>
    where TField : IFieldDefinition
{
    private readonly OrderedDictionary<string, TField> _fields = new();

    public int Count => _fields.Count;

    public bool IsReadOnly => false;

    public TField this[string name] => _fields[name];

    public bool TryGetField(string name, [NotNullWhen(true)] out TField? field)
        => _fields.TryGetValue(name, out field);

    public void Insert(int index, TField field)
    {
        if (field is null)
        {
            throw new ArgumentNullException(nameof(field));
        }

        _fields.Insert(index, field.Name, field);
    }

    public void Add(TField item)
    {
        if (item is null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        _fields.Add(item.Name, item);
    }

    public bool Remove(TField item)
    {
        if (item is null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        if (_fields.TryGetValue(item.Name, out var itemToDelete) &&
            ReferenceEquals(item, itemToDelete))
        {
            _fields.Remove(item.Name);
            return true;
        }

        return false;
    }

    public bool Remove(string name)
        => _fields.Remove(name);

    public void RemoveAt(int index)
    {
        if (index < 0 || index >= _fields.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        _fields.RemoveAt(index);
    }

    public void Clear() => _fields.Clear();

    public bool ContainsName(string name)
        => _fields.ContainsKey(name);

    public bool Contains(TField item)
    {
        if (item is null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        if (_fields.TryGetValue(item.Name, out var itemToDelete) &&
            ReferenceEquals(item, itemToDelete))
        {
            return true;
        }

        return false;
    }

    public int IndexOf(TField field)
    {
        if (field is null)
        {
            throw new ArgumentNullException(nameof(field));
        }

        return _fields.IndexOf(field.Name);
    }

    public int IndexOf(string name)
    {
        if (name is null)
        {
            throw new ArgumentNullException(nameof(name));
        }

        return _fields.IndexOf(name);
    }

    public void CopyTo(TField[] array, int arrayIndex)
    {
        foreach (var item in _fields)
        {
            array[arrayIndex++] = item.Value;
        }
    }

    public IEnumerator<TField> GetEnumerator()
        => _fields.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}
