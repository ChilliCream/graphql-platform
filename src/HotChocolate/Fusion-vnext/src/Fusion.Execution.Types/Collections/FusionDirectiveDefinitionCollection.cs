using System.Collections;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.Types.Collections;

public sealed class FusionDirectiveDefinitionCollection
    : IReadOnlyDirectiveDefinitionCollection
    , IEnumerable<FusionDirectiveDefinition>
{
    private readonly FrozenDictionary<string, FusionDirectiveDefinition> _directives;

    public FusionDirectiveDefinitionCollection(FusionDirectiveDefinition[] directives)
    {
        ArgumentNullException.ThrowIfNull(directives);
        _directives = directives.ToFrozenDictionary(t => t.Name);
    }

    public FusionDirectiveDefinition this[string name]
        => _directives[name];

    IDirectiveDefinition IReadOnlyDirectiveDefinitionCollection.this[string name]
        => _directives[name];

    public FusionDirectiveDefinition SkipDirective => _directives[SpecDirectiveNames.Skip.Name];

    public FusionDirectiveDefinition IncludeDirective => _directives[SpecDirectiveNames.Include.Name];

    public bool TryGetDirective(
        string name,
        [NotNullWhen(true)] out FusionDirectiveDefinition? definition)
        => _directives.TryGetValue(name, out definition);

    bool IReadOnlyDirectiveDefinitionCollection.TryGetDirective(
        string name, [NotNullWhen(true)]
        out IDirectiveDefinition? definition)
    {
        if (_directives.TryGetValue(name, out var d))
        {
            definition = d;
            return true;
        }

        definition = default;
        return false;
    }

    public bool ContainsName(string name)
        => _directives.ContainsKey(name);

    public IEnumerable<FusionDirectiveDefinition> AsEnumerable()
        => _directives.Values;

    public IEnumerator<FusionDirectiveDefinition> GetEnumerator()
        => AsEnumerable().GetEnumerator();

    IEnumerator<IDirectiveDefinition> IEnumerable<IDirectiveDefinition>.GetEnumerator()
        => AsEnumerable().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => AsEnumerable().GetEnumerator();
}
