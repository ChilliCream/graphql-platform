using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Types.Mutable;

public sealed class DirectiveDefinitionCollection
    : IList<MutableDirectiveDefinition>
    , IReadOnlyDirectiveDefinitionCollection
{
    private readonly List<SchemaCoordinate> _schemaDefinitions;
    private readonly OrderedDictionary<string, MutableDirectiveDefinition> _definitions = new();

    internal DirectiveDefinitionCollection(List<SchemaCoordinate> schemaDefinitions)
    {
        _schemaDefinitions = schemaDefinitions
            ?? throw new ArgumentNullException(nameof(schemaDefinitions));
    }

    public int Count => _definitions.Count;

    public bool IsReadOnly => false;

    public MutableDirectiveDefinition this[string name] => _definitions[name];

    IDirectiveDefinition IReadOnlyDirectiveDefinitionCollection.this[string name] => _definitions[name];

    public bool TryGetDirective(string name, [NotNullWhen(true)] out MutableDirectiveDefinition? definition)
        => _definitions.TryGetValue(name, out definition);

    bool IReadOnlyDirectiveDefinitionCollection.TryGetDirective(
        string name,
        [NotNullWhen(true)] out IDirectiveDefinition? definition)
    {
        if(_definitions.TryGetValue(name, out var directiveDefinition))
        {
            definition = directiveDefinition;
            return true;
        }

        definition = null;
        return false;
    }


    public void Insert(int index, MutableDirectiveDefinition definition)
    {
        if (definition is null)
        {
            throw new ArgumentNullException(nameof(definition));
        }

        var type = _definitions.GetAt(index);
        var definitionIndex = _schemaDefinitions.IndexOf(new SchemaCoordinate(type.Key, ofDirective: true));
        _schemaDefinitions.Insert(definitionIndex, new SchemaCoordinate(definition.Name, ofDirective: true));
        _definitions.Insert(index, definition.Name, definition);
    }

    public bool Remove(string name)
    {
        if (_definitions.Remove(name))
        {
            _schemaDefinitions.Remove(new SchemaCoordinate(name, ofDirective: true));
            return true;
        }

        return false;
    }

    public void RemoveAt(int index)
    {
        var type = _definitions.GetAt(index);
        _definitions.Remove(type.Key);
        _schemaDefinitions.Remove(new SchemaCoordinate(type.Key, ofDirective: true));
    }

    MutableDirectiveDefinition IList<MutableDirectiveDefinition>.this[int index]
    {
        get => _definitions.GetAt(index).Value;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            RemoveAt(index);
            Insert(index, value);
        }
    }

    public void Add(MutableDirectiveDefinition item)
    {
        if (item is null)
        {
            throw new ArgumentNullException(nameof(item));
        }

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
        _schemaDefinitions.Add(new SchemaCoordinate(item.Name, ofDirective: true));
    }

    public bool Remove(MutableDirectiveDefinition item)
    {
        if (item is null)
        {
            throw new ArgumentNullException(nameof(item));
        }

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
            _schemaDefinitions.Remove(new SchemaCoordinate(typeName));
        }

        _definitions.Clear();
    }

    public bool ContainsName(string name)
        => _definitions.ContainsKey(name);

    public int IndexOf(MutableDirectiveDefinition definition)
    {
        if (definition is null)
        {
            throw new ArgumentNullException(nameof(definition));
        }

        return IndexOf(definition.Name);
    }

    public int IndexOf(string name)
        => _definitions.IndexOf(name);

    public bool Contains(MutableDirectiveDefinition item)
    {
        if (item is null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        if (_definitions.TryGetValue(item.Name, out var itemToDelete) && ReferenceEquals(item, itemToDelete))
        {
            return true;
        }

        return false;
    }

    public void CopyTo(MutableDirectiveDefinition[] array, int arrayIndex)
    {
        foreach (var item in _definitions)
        {
            array[arrayIndex++] = item.Value;
        }
    }

    public IEnumerator<MutableDirectiveDefinition> GetEnumerator()
        => _definitions.Values.GetEnumerator();

    IEnumerator<IDirectiveDefinition> IEnumerable<IDirectiveDefinition>.GetEnumerator()
        => _definitions.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}
