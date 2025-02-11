using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Types.Mutable;

public sealed class TypeDefinitionCollection
    : IList<INamedTypeDefinition>
    , IReadOnlyTypeDefinitionCollection
{
    private readonly List<SchemaCoordinate> _schemaDefinitions;
    private readonly OrderedDictionary<string, INamedTypeDefinition> _types = new();

    internal TypeDefinitionCollection(List<SchemaCoordinate> schemaDefinitions)
    {
        _schemaDefinitions = schemaDefinitions
            ?? throw new ArgumentNullException(nameof(schemaDefinitions));
    }

    public int Count => _types.Count;

    public bool IsReadOnly => false;

    public INamedTypeDefinition this[string name] => _types[name];

    public INamedTypeDefinition this[int index]
    {
        get => _types.GetAt(index).Value;
        set
        {
            RemoveAt(index);
            Insert(index, value);
        }
    }

    public bool TryGetType(string name, [NotNullWhen(true)] out INamedTypeDefinition? definition)
        => _types.TryGetValue(name, out definition);

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

    public void Insert(int index, INamedTypeDefinition definition)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, _types.Count);

        var type = _types.GetAt(index);
        var definitionIndex = _schemaDefinitions.IndexOf(new SchemaCoordinate(type.Key));
        _schemaDefinitions.Insert(definitionIndex, new SchemaCoordinate(definition.Name));
        _types.Insert(index, definition.Name, definition);
    }

    public bool Remove(string name)
    {
        if (_types.Remove(name))
        {
            _schemaDefinitions.Remove(new SchemaCoordinate(name));
            return true;
        }

        return false;
    }

    public void RemoveAt(int index)
    {
        if(_types.Count <= index)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        var type = _types.GetAt(index);
        _schemaDefinitions.Remove(new SchemaCoordinate(type.Key));
        _types.RemoveAt(index);
    }

    public void Add(INamedTypeDefinition definition)
    {
        if (definition is null)
        {
            throw new ArgumentNullException(nameof(definition));
        }

        if(_types.TryGetValue(definition.Name, out var existing))
        {
            if (ReferenceEquals(existing, definition))
            {
                return;
            }

            throw new ArgumentException(
                $"The type `{definition.Name}` is already defined.",
                nameof(definition));
        }

        _types.Add(definition.Name, definition);
        _schemaDefinitions.Add(new SchemaCoordinate(definition.Name));
    }

    public bool Remove(INamedTypeDefinition definition)
    {
        if (definition is null)
        {
            throw new ArgumentNullException(nameof(definition));
        }

        if (_types.TryGetValue(definition.Name, out var itemToDelete)
            && ReferenceEquals(definition, itemToDelete))
        {
            Remove(definition.Name);
            return true;
        }

        return false;
    }

    public void Clear()
    {
        foreach (var typeName in _types.Keys)
        {
            _schemaDefinitions.Remove(new SchemaCoordinate(typeName));
        }

        _types.Clear();
    }

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
