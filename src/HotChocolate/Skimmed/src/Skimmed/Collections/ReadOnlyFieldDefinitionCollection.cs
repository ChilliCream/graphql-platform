using System.Collections;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Skimmed;

public abstract class ReadOnlyFieldDefinitionCollection<TField>
    : IFieldDefinitionCollection<TField>
    where TField : IFieldDefinition
{
    private readonly FrozenDictionary<string, TField> _fields;

    protected ReadOnlyFieldDefinitionCollection(IEnumerable<TField> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        _fields = values.ToFrozenDictionary(t => t.Name, StringComparer.Ordinal);
    }

    public int Count => _fields.Count;

    public bool IsReadOnly => true;

    public TField this[string name] => _fields[name];

    public bool TryGetField(string name, [NotNullWhen(true)] out TField? field)
        => _fields.TryGetValue(name, out field);

    public void Add(TField item) => ThrowReadOnly();

    public bool Remove(TField item)
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
