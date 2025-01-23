using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Skimmed;

public sealed class DirectiveDefinitionCollection : IDirectiveDefinitionCollection
{
    private readonly OrderedDictionary<string, DirectiveDefinition> _types = new();

    public int Count => _types.Count;

    public bool IsReadOnly => false;

    public DirectiveDefinition this[string name] => _types[name];

    public bool TryGetDirective(string name, [NotNullWhen(true)] out DirectiveDefinition? definition)
        => _types.TryGetValue(name, out definition);

    public void Insert(int index, DirectiveDefinition definition)
    {
        if (definition is null)
        {
            throw new ArgumentNullException(nameof(definition));
        }

        _types.Insert(index, definition.Name, definition);
    }

    public bool Remove(string name)
        => _types.Remove(name);

    public void RemoveAt(int index)
        => _types.RemoveAt(index);

    public void Add(DirectiveDefinition item)
    {
        if (item is null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        _types.Add(item.Name, item);
    }

    public bool Remove(DirectiveDefinition item)
    {
        if (item is null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        if (_types.TryGetValue(item.Name, out var itemToDelete) &&
            ReferenceEquals(item, itemToDelete))
        {
            _types.Remove(item.Name);
            return true;
        }

        return false;
    }

    public void Clear() => _types.Clear();

    public bool ContainsName(string name)
        => _types.ContainsKey(name);

    public int IndexOf(DirectiveDefinition definition)
    {
        if (definition is null)
        {
            throw new ArgumentNullException(nameof(definition));
        }

        return IndexOf(definition.Name);
    }

    public int IndexOf(string name)
        => _types.IndexOf(name);

    public bool Contains(DirectiveDefinition item)
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

    public void CopyTo(DirectiveDefinition[] array, int arrayIndex)
    {
        foreach (var item in _types)
        {
            array[arrayIndex++] = item.Value;
        }
    }

    public IEnumerator<DirectiveDefinition> GetEnumerator()
        => _types.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}
