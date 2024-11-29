using System.Collections;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Skimmed;

public sealed class ReadOnlyDirectiveDefinitionCollection : IDirectiveDefinitionCollection
{
    private readonly FrozenDictionary<string, DirectiveDefinition> _types;

    private ReadOnlyDirectiveDefinitionCollection(IEnumerable<DirectiveDefinition> directives)
    {
        ArgumentNullException.ThrowIfNull(directives);
        _types = directives.ToFrozenDictionary(t => t.Name, StringComparer.Ordinal);
    }

    public int Count => _types.Count;

    public bool IsReadOnly => false;

    public DirectiveDefinition this[string name] => _types[name];

    public bool TryGetDirective(string name, [NotNullWhen(true)] out DirectiveDefinition? type)
        => _types.TryGetValue(name, out type);

    public void Add(DirectiveDefinition item) => ThrowReadOnly();

    public bool Remove(DirectiveDefinition item)
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
    {
        foreach (var item in _types)
        {
            yield return item.Value;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public static ReadOnlyDirectiveDefinitionCollection Empty { get; } = new(Array.Empty<DirectiveDefinition>());

    public static ReadOnlyDirectiveDefinitionCollection From(IEnumerable<DirectiveDefinition> values)
        => new(values);
}
