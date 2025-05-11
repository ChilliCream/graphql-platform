using System.Collections;
using System.Collections.Frozen;
using System.Runtime.CompilerServices;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Types.Collections;

public sealed class FusionObjectTypeDefinitionCollection
    : IReadOnlyObjectTypeDefinitionCollection
    , IEnumerable<FusionObjectTypeDefinition>
{
    private readonly FusionObjectTypeDefinition[] _types;
    private readonly FrozenSet<string> _typeNames;

    public FusionObjectTypeDefinitionCollection(
        FusionObjectTypeDefinition[] types)
    {
        _types = types;
        _typeNames = types.Select(t => t.Name).ToFrozenSet();
    }

    public int Count => _types.Length;

    public FusionObjectTypeDefinition this[int index]
        => _types[index];

    IObjectTypeDefinition IReadOnlyList<IObjectTypeDefinition>.this[int index]
        => _types[index];

    public bool ContainsName(string name)
        => _typeNames.Contains(name);

    public IEnumerable<FusionObjectTypeDefinition> AsEnumerable()
        => Unsafe.As<IEnumerable<FusionObjectTypeDefinition>>(_types);

    public IEnumerator<FusionObjectTypeDefinition> GetEnumerator()
        => AsEnumerable().GetEnumerator();

    IEnumerator<IObjectTypeDefinition> IEnumerable<IObjectTypeDefinition>.GetEnumerator()
        => AsEnumerable().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => AsEnumerable().GetEnumerator();

    public static FusionObjectTypeDefinitionCollection Empty { get; } = new([]);
}
