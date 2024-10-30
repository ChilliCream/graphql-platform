using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Skimmed;

public sealed class ReadOnlyObjectTypeDefinitionCollection : IObjectTypeDefinitionCollection
{
    private readonly ObjectTypeDefinition[] _types;

    private ReadOnlyObjectTypeDefinitionCollection(IEnumerable<ObjectTypeDefinition> types)
    {
        ArgumentNullException.ThrowIfNull(types);
        _types = types.ToArray();
    }

    public ObjectTypeDefinition this[int index] => _types[index];

    public int Count => _types.Length;

    public bool IsReadOnly => true;

    public bool Contains(ObjectTypeDefinition item)
        => _types.Contains(item);

    public void Add(ObjectTypeDefinition item)
        => ThrowReadOnly();

    public bool Remove(ObjectTypeDefinition item)
    {
        ThrowReadOnly();
        return false;
    }

    public void RemoveAt(int index)
        => ThrowReadOnly();

    public void Clear()
        => ThrowReadOnly();

    [DoesNotReturn]
    private static void ThrowReadOnly()
        => throw new NotSupportedException("Collection is read-only.");

    public void CopyTo(ObjectTypeDefinition[] array, int arrayIndex)
        => _types.CopyTo(array, arrayIndex);

    public IEnumerator<ObjectTypeDefinition> GetEnumerator()
    {
        foreach (var item in _types)
        {
            yield return item;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public static ReadOnlyObjectTypeDefinitionCollection Empty { get; } = new([]);

    public static ReadOnlyObjectTypeDefinitionCollection From(IEnumerable<ObjectTypeDefinition> values)
        => new(values);
}
