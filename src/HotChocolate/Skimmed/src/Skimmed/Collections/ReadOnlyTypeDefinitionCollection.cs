using System.Collections;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Skimmed;

public sealed class ReadOnlyTypeDefinitionCollection : ITypeDefinitionCollection
{
    private readonly OrderedDictionary<string, INamedTypeDefinition> _types;

    private ReadOnlyTypeDefinitionCollection(IEnumerable<INamedTypeDefinition> types)
    {
        if (types is null)
        {
            throw new ArgumentNullException(nameof(types));
        }

        _types = types.ToOrderedDictionary(t => t.Name);
    }

    public int Count => _types.Count;

    public bool IsReadOnly => false;

    public INamedTypeDefinition this[string name] => _types[name];

    public bool TryGetType(string name, [NotNullWhen(true)] out INamedTypeDefinition? definition)
        => _types.TryGetValue(name, out definition);

    public bool TryGetType<T>(string name, [NotNullWhen(true)] out T? type) where T : INamedTypeDefinition
    {
        if (_types.TryGetValue(name, out var namedType) && namedType is T casted)
        {
            type = casted;
            return true;
        }

        type = default;
        return false;
    }

    public void Insert(int index, INamedTypeDefinition definition)
        => ThrowReadOnly();

    public bool Remove(string name)
    {
        ThrowReadOnly();
        return false;
    }

    public void RemoveAt(int index)
        => ThrowReadOnly();

    public void Add(INamedTypeDefinition item)
        => ThrowReadOnly();

    public bool Remove(INamedTypeDefinition item)
    {
        ThrowReadOnly();
        return false;
    }

    public void Clear()
        => ThrowReadOnly();

    [DoesNotReturn]
    private static void ThrowReadOnly()
        => throw new NotSupportedException("Collection is read-only.");

    public bool ContainsName(string name)
        => _types.ContainsKey(name);

    public int IndexOf(INamedTypeDefinition definition)
    {
        if (definition is null)
        {
            throw new ArgumentNullException(nameof(definition));
        }

        return IndexOf(definition.Name);
    }

    public int IndexOf(string name)
        => _types.IndexOf(name);

    public bool Contains(INamedTypeDefinition item)
    {
        if (item is null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        if (_types.TryGetValue(item.Name, out var itemToDelete) &&
            ReferenceEquals(item, itemToDelete))
        {
            return true;
        }

        return false;
    }

    public void CopyTo(INamedTypeDefinition[] array, int arrayIndex)
    {
        foreach (var item in _types)
        {
            array[arrayIndex++] = item.Value;
        }
    }

    public IEnumerator<INamedTypeDefinition> GetEnumerator()
        => _types.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public static ReadOnlyTypeDefinitionCollection Empty { get; } = new(Array.Empty<INamedTypeDefinition>());

    public static ReadOnlyTypeDefinitionCollection From(IEnumerable<INamedTypeDefinition> values)
        => new(values);
}
