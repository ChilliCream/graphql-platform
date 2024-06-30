using System.Collections;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Skimmed;

public sealed class ReadOnlyTypeDefinitionCollection : ITypeDefinitionCollection
{
    private readonly FrozenDictionary<string, INamedTypeDefinition> _types;

    private ReadOnlyTypeDefinitionCollection(IEnumerable<INamedTypeDefinition> types)
    {
        ArgumentNullException.ThrowIfNull(types);
        _types = types.ToFrozenDictionary(t => t.Name, StringComparer.Ordinal);
    }

    public int Count => _types.Count;

    public bool IsReadOnly => false;

    public INamedTypeDefinition this[string name] => _types[name];

    public bool TryGetType(string name, [NotNullWhen(true)] out INamedTypeDefinition? type)
        => _types.TryGetValue(name, out type);

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

    public void Add(INamedTypeDefinition item) => ThrowReadOnly();

    public bool Remove(INamedTypeDefinition item)
    {
        ThrowReadOnly();
        return false;
    }

    public void Clear() => ThrowReadOnly();

    [DoesNotReturn]
    private static void ThrowReadOnly()
        => throw new NotSupportedException("Collection is read-only.");

    public bool ContainsName(string name)
        => _types.ContainsKey(name);

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
    {
        foreach (var entry in _types)
        {
            yield return entry.Value;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public static ReadOnlyTypeDefinitionCollection Empty { get; } = new(Array.Empty<INamedTypeDefinition>());

    public static ReadOnlyTypeDefinitionCollection From(IEnumerable<INamedTypeDefinition> values)
        => new(values);
}
