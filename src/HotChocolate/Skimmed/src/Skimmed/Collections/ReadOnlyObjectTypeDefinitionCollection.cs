using System.Collections;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types;

namespace HotChocolate.Skimmed;

public sealed class ReadOnlyObjectTypeDefinitionCollection
    : IObjectTypeDefinitionCollection
    , IReadOnlyObjectTypeDefinitionCollection
{
    private readonly ObjectTypeDefinition[] _types;

    private ReadOnlyObjectTypeDefinitionCollection(IEnumerable<ObjectTypeDefinition> definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        _types = definitions.ToArray();
    }

    public ObjectTypeDefinition this[int index] => _types[index];

    public int Count => _types.Length;

    public bool IsReadOnly => true;

    public bool Contains(ObjectTypeDefinition definition)
        => _types.Contains(definition);

    public void Add(ObjectTypeDefinition definition)
        => ThrowReadOnly();

    public bool Remove(ObjectTypeDefinition definition)
    {
        ThrowReadOnly();
        return false;
    }

    public bool ContainsName(string name)
    {
        foreach (var item in _types)
        {
            if (item.Name.Equals(name))
            {
                return true;
            }
        }
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
        => ((IEnumerable<ObjectTypeDefinition>)_types).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    IEnumerator<IReadOnlyObjectTypeDefinition> IEnumerable<IReadOnlyObjectTypeDefinition>.GetEnumerator()
        => GetEnumerator();

    public static ReadOnlyObjectTypeDefinitionCollection Empty { get; } = new([]);

    public static ReadOnlyObjectTypeDefinitionCollection From(IEnumerable<ObjectTypeDefinition> values)
        => new(values);
}
