using System.Collections;
using System.Collections.Frozen;
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

    public bool ContainsName(string name)
        => _typeNames.Contains(name);

    public IEnumerable<FusionObjectTypeDefinition> AsEnumerable()
        => _types;

    public IEnumerator<FusionObjectTypeDefinition> GetEnumerator()
        => AsEnumerable().GetEnumerator();

    IEnumerator<IObjectTypeDefinition> IEnumerable<IObjectTypeDefinition>.GetEnumerator()
        => AsEnumerable().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => AsEnumerable().GetEnumerator();
}
