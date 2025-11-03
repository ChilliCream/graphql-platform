using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Types.Mutable;

public sealed class TypeDefinitionCollection
    : IList<ITypeDefinition>
    , IReadOnlyTypeDefinitionCollection
{
    private readonly List<SchemaCoordinate> _schemaDefinitions;
    private readonly OrderedDictionary<string, ITypeDefinition> _types = [];

    internal TypeDefinitionCollection(List<SchemaCoordinate> schemaDefinitions)
    {
        ArgumentNullException.ThrowIfNull(schemaDefinitions);

        _schemaDefinitions = schemaDefinitions
            ?? throw new ArgumentNullException(nameof(schemaDefinitions));
    }

    public int Count => _types.Count;

    public bool IsReadOnly => false;

    public ITypeDefinition this[string name]
    {
        get
        {
            ArgumentException.ThrowIfNullOrEmpty(name);

            return _types[name];
        }
    }

    public ITypeDefinition this[int index]
    {
        get
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);

            return _types.GetAt(index).Value;
        }
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentNullException.ThrowIfNull(value);

            RemoveAt(index);
            Insert(index, value);
        }
    }

    [return: NotNull]
    public T GetType<T>(string typeName) where T : ITypeDefinition
    {
        if (_types[typeName] is T type)
        {
            return type;
        }

        throw new InvalidOperationException(
            $"The type `{typeName}` is not a `{typeof(T).Name}`.");
    }

    public bool TryGetType(string name, [NotNullWhen(true)] out ITypeDefinition? definition)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        return _types.TryGetValue(name, out definition);
    }

    public bool TryGetType<T>(string name, [NotNullWhen(true)] out T? type) where T : ITypeDefinition
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        if (_types.TryGetValue(name, out var namedType)
            && namedType is T casted)
        {
            type = casted;
            return true;
        }

        type = default;
        return false;
    }

    public void Insert(int index, ITypeDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        var type = _types.GetAt(index);
        var definitionIndex = _schemaDefinitions.IndexOf(new SchemaCoordinate(type.Key));
        _schemaDefinitions.Insert(definitionIndex, new SchemaCoordinate(definition.Name));
        _types.Insert(index, definition.Name, definition);
    }

    public bool Remove(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        if (_types.Remove(name))
        {
            _schemaDefinitions.Remove(new SchemaCoordinate(name));
            return true;
        }

        return false;
    }

    public void RemoveAt(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        var type = _types.GetAt(index);
        _schemaDefinitions.Remove(new SchemaCoordinate(type.Key));
        _types.RemoveAt(index);
    }

    public void Add(ITypeDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        if (_types.TryGetValue(definition.Name, out var existing))
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

    public bool Remove(ITypeDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

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
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        return _types.ContainsKey(name);
    }

    public int IndexOf(ITypeDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        return IndexOf(definition.Name);
    }

    public int IndexOf(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        return _types.IndexOf(name);
    }

    public bool Contains(ITypeDefinition item)
    {
        ArgumentNullException.ThrowIfNull(item);

        return _types.TryGetValue(item.Name, out var itemToDelete)
            && ReferenceEquals(item, itemToDelete);
    }

    public void CopyTo(ITypeDefinition[] array, int arrayIndex)
    {
        ArgumentNullException.ThrowIfNull(array);
        ArgumentOutOfRangeException.ThrowIfNegative(arrayIndex);

        foreach (var item in _types)
        {
            array[arrayIndex++] = item.Value;
        }
    }

    public IEnumerable<ITypeDefinition> AsEnumerable()
        => _types.Values;

    public IEnumerator<ITypeDefinition> GetEnumerator()
        => _types.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}
