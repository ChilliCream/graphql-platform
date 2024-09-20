using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Skimmed;

public sealed class TypeDefinitionCollection : ITypeDefinitionCollection
{
    private readonly Dictionary<string, INamedTypeDefinition> _types = new(StringComparer.Ordinal);

    public int Count => _types.Count;

    public bool IsReadOnly => false;

    public INamedTypeDefinition this[string name] => _types[name];

    public bool TryGetType(string name, [NotNullWhen(true)] out INamedTypeDefinition? type)
        => _types.TryGetValue(name, out type);

    public bool TryGetType<T>(string name, [NotNullWhen(true)] out T? type) where T : INamedTypeDefinition
    {
        if (_types.TryGetValue(name, out var namedType)
            && namedType is T casted)
        {
            type = casted;
            return true;
        }

        type = default;
        return false;
    }

    public void Add(INamedTypeDefinition item)
    {
        if (item is null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        if(_types.TryGetValue(item.Name, out var existing))
        {
            if (ReferenceEquals(existing, item))
            {
                return;
            }

            throw new ArgumentException(
                $"The type `{item.Name}` is already defined.",
                nameof(item));
        }

        _types.Add(item.Name, item);
    }

    public bool Remove(INamedTypeDefinition item)
    {
        if (item is null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        if (_types.TryGetValue(item.Name, out var itemToDelete)
            && ReferenceEquals(item, itemToDelete))
        {
            _types.Remove(item.Name);
            return true;
        }

        return false;
    }

    public void Clear() => _types.Clear();

    public bool ContainsName(string name)
        => _types.ContainsKey(name);

    public bool Contains(INamedTypeDefinition item)
    {
        if (item is null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        if (_types.TryGetValue(item.Name, out var itemToDelete)
            && ReferenceEquals(item, itemToDelete))
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
}
