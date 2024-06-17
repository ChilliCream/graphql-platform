using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Skimmed;

public abstract class FieldDefinitionCollection<TField>
    : IFieldDefinitionCollection<TField>
    where TField : IFieldDefinition
{
    private readonly Dictionary<string, TField> _fields = new(StringComparer.Ordinal);

    public int Count => _fields.Count;

    public bool IsReadOnly => false;

    public TField this[string name] => _fields[name];

    public bool TryGetField(string name, [NotNullWhen(true)] out TField? field)
        => _fields.TryGetValue(name, out field);

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

    public void CopyTo(TField[] array, int arrayIndex)
    {
        foreach (var item in _fields)
        {
            array[arrayIndex++] = item.Value;
        }
    }

    public IEnumerator<TField> GetEnumerator()
        => _fields.Values.OrderBy(t => t.Name, StringComparer.Ordinal).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}
