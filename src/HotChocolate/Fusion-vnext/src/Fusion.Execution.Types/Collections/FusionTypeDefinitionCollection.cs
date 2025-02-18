using System.Collections;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Types.Collections;

public sealed class FusionTypeDefinitionCollection
    : IReadOnlyTypeDefinitionCollection
{
    private readonly FrozenDictionary<string, ITypeDefinition> _types;

    public FusionTypeDefinitionCollection(ITypeDefinition[] types)
    {
        ArgumentNullException.ThrowIfNull(types);
        _types = types.ToFrozenDictionary(t => t.Name);
    }

    public ITypeDefinition this[string name]
        => _types[name];

    public bool ContainsName(string name)
        => _types.ContainsKey(name);

    public bool TryGetType(string name, [NotNullWhen(true)] out ITypeDefinition? definition)
    {
        if (_types.TryGetValue(name, out var t))
        {
            definition = t;
            return true;
        }

        definition = default;
        return false;
    }

    public bool TryGetType<T>(string name, [NotNullWhen(true)] out T? type)
        where T : ITypeDefinition
    {
        if (_types.TryGetValue(name, out var t) && t is T casted)
        {
            type = casted;
            return true;
        }

        type = default;
        return false;
    }

    public IEnumerable<ITypeDefinition> AsEnumerable()
        => _types.Values;

    public IEnumerator<ITypeDefinition> GetEnumerator()
        => AsEnumerable().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => AsEnumerable().GetEnumerator();
}
