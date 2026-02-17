using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Types.Mutable;

public sealed class DirectiveDefinitionCollection
    : IList<MutableDirectiveDefinition>
    , IReadOnlyDirectiveDefinitionCollection
{
    private readonly List<SchemaCoordinate> _coordinates;
    private readonly OrderedDictionary<string, MutableDirectiveDefinition> _definitions = [];

    internal DirectiveDefinitionCollection(List<SchemaCoordinate> schemaDefinitions)
    {
        _coordinates = schemaDefinitions
            ?? throw new ArgumentNullException(nameof(schemaDefinitions));
    }

    public int Count => _definitions.Count;

    public bool IsReadOnly => false;

    public MutableDirectiveDefinition this[int index]
    {
        get
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);

            return _definitions.GetAt(index).Value;
        }
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentNullException.ThrowIfNull(value);

            RemoveAt(index);
            Insert(index, value);
        }
    }

    IDirectiveDefinition IReadOnlyList<IDirectiveDefinition>.this[int index]
        => _definitions.GetAt(index).Value;

    public MutableDirectiveDefinition this[string name]
    {
        get
        {
            ArgumentException.ThrowIfNullOrEmpty(name);

            return _definitions[name];
        }
    }

    IDirectiveDefinition IReadOnlyDirectiveDefinitionCollection.this[string name] => _definitions[name];

    public bool TryGetDirective(string name, [NotNullWhen(true)] out MutableDirectiveDefinition? definition)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        return _definitions.TryGetValue(name, out definition);
    }

    bool IReadOnlyDirectiveDefinitionCollection.TryGetDirective(
        string name,
        [NotNullWhen(true)] out IDirectiveDefinition? definition)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        if (_definitions.TryGetValue(name, out var directiveDefinition))
        {
            definition = directiveDefinition;
            return true;
        }

        definition = null;
        return false;
    }

    public void Insert(int index, MutableDirectiveDefinition definition)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentNullException.ThrowIfNull(definition);

        var type = _definitions.GetAt(index);
        var definitionIndex = _coordinates.IndexOf(new SchemaCoordinate(type.Key, ofDirective: true));
        _coordinates.Insert(definitionIndex, new SchemaCoordinate(definition.Name, ofDirective: true));
        _definitions.Insert(index, definition.Name, definition);
    }

    public bool Remove(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        if (_definitions.Remove(name))
        {
            _coordinates.Remove(new SchemaCoordinate(name, ofDirective: true));
            return true;
        }

        return false;
    }

    public void RemoveAt(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        var type = _definitions.GetAt(index);
        _definitions.Remove(type.Key);
        _coordinates.Remove(new SchemaCoordinate(type.Key, ofDirective: true));
    }

    public void Add(MutableDirectiveDefinition item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (_definitions.TryGetValue(item.Name, out var existing))
        {
            if (ReferenceEquals(existing, item))
            {
                return;
            }

            throw new ArgumentException(
                $"The directive definition `@{item.Name}` is already defined.",
                nameof(item));
        }

        _definitions.Add(item.Name, item);
        _coordinates.Add(new SchemaCoordinate(item.Name, ofDirective: true));
    }

    public bool Remove(MutableDirectiveDefinition item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (_definitions.TryGetValue(item.Name, out var itemToDelete)
            && ReferenceEquals(item, itemToDelete))
        {
            return Remove(item.Name);
        }

        return false;
    }

    public void Clear()
    {
        foreach (var typeName in _definitions.Keys)
        {
            _coordinates.Remove(new SchemaCoordinate(typeName));
        }

        _definitions.Clear();
    }

    public bool ContainsName(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        return _definitions.ContainsKey(name);
    }

    public int IndexOf(MutableDirectiveDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        return IndexOf(definition.Name);
    }

    public int IndexOf(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        return _definitions.IndexOf(name);
    }

    public bool Contains(MutableDirectiveDefinition item)
    {
        ArgumentNullException.ThrowIfNull(item);

        return _definitions.TryGetValue(item.Name, out var itemToDelete)
            && ReferenceEquals(item, itemToDelete);
    }

    public void CopyTo(MutableDirectiveDefinition[] array, int arrayIndex)
    {
        ArgumentNullException.ThrowIfNull(array);
        ArgumentOutOfRangeException.ThrowIfNegative(arrayIndex);

        foreach (var item in _definitions)
        {
            array[arrayIndex++] = item.Value;
        }
    }

    public IEnumerable<MutableDirectiveDefinition> AsEnumerable()
        => _definitions.Values;

    public IEnumerator<MutableDirectiveDefinition> GetEnumerator()
        => _definitions.Values.GetEnumerator();

    IEnumerator<IDirectiveDefinition> IEnumerable<IDirectiveDefinition>.GetEnumerator()
        => _definitions.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}
