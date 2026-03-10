using System.Collections;
using System.Collections.Frozen;
using System.Runtime.CompilerServices;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Types.Collections;

public sealed class FusionInterfaceTypeDefinitionCollection
    : IReadOnlyInterfaceTypeDefinitionCollection
    , IReadOnlyList<FusionInterfaceTypeDefinition>
{
    private readonly FusionInterfaceTypeDefinition[] _types;
    private readonly FrozenSet<string> _typeNames;

    public FusionInterfaceTypeDefinitionCollection(
        FusionInterfaceTypeDefinition[] types)
    {
        _types = types;
        _typeNames = types.Select(t => t.Name).ToFrozenSet();
    }

    public FusionInterfaceTypeDefinition this[int index]
        => _types[index];

    IInterfaceTypeDefinition IReadOnlyList<IInterfaceTypeDefinition>.this[int index]
        => _types[index];

    public int Count => _types.Length;

    public bool ContainsName(string name)
        => _typeNames.Contains(name);

    public bool Contains(FusionInterfaceTypeDefinition item)
        => _types.Contains(item);

    public IEnumerable<FusionInterfaceTypeDefinition> AsEnumerable()
        => Unsafe.As<IEnumerable<FusionInterfaceTypeDefinition>>(_types);

    public IEnumerator<FusionInterfaceTypeDefinition> GetEnumerator()
        => AsEnumerable().GetEnumerator();

    IEnumerator<IInterfaceTypeDefinition> IEnumerable<IInterfaceTypeDefinition>.GetEnumerator()
        => AsEnumerable().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => AsEnumerable().GetEnumerator();

    public static FusionInterfaceTypeDefinitionCollection Empty { get; } = new([]);
}
