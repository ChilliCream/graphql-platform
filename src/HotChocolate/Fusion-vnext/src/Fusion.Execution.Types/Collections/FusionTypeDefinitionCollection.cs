using System.Collections;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Types.Collections;

public sealed class FusionTypeDefinitionCollection
    : IReadOnlyTypeDefinitionCollection
{
    private readonly ITypeDefinition[] _types;
    private readonly FrozenDictionary<string, ITypeDefinition> _typesLookup;

    public FusionTypeDefinitionCollection(ITypeDefinition[] types)
    {
        ArgumentNullException.ThrowIfNull(types);
        _types = types;
        _typesLookup = types.ToFrozenDictionary(t => t.Name);
    }

    public int Count => _types.Length;

    public ITypeDefinition this[int index] => _types[index];

    public ITypeDefinition this[string name] => _typesLookup[name];

    public bool ContainsName(string name) => _typesLookup.ContainsKey(name);

    [return: NotNull]
    public T GetType<T>(string name)
        where T : ITypeDefinition
    {
        if (_typesLookup.TryGetValue(name, out var t))
        {
            if (t is T casted)
            {
                return casted;
            }

            throw new InvalidOperationException(
                $"The specified type '{name}' does not match the requested type.");
        }

        throw new ArgumentException("The specified type name does not exist.", nameof(name));
    }

    public bool TryGetType(string name, [NotNullWhen(true)] out ITypeDefinition? definition)
    {
        if (_typesLookup.TryGetValue(name, out var t))
        {
            definition = t;
            return true;
        }

        definition = null;
        return false;
    }

    public bool TryGetType<T>(string name, [NotNullWhen(true)] out T? type)
        where T : ITypeDefinition
    {
        if (_typesLookup.TryGetValue(name, out var t) && t is T casted)
        {
            type = casted;
            return true;
        }

        type = default;
        return false;
    }

    public IEnumerable<ITypeDefinition> AsEnumerable()
        => Unsafe.As<IEnumerable<ITypeDefinition>>(_types);

    public IEnumerator<ITypeDefinition> GetEnumerator()
        => AsEnumerable().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => AsEnumerable().GetEnumerator();
}
