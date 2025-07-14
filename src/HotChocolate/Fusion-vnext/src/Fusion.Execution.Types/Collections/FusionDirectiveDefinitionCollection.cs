using System.Collections;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Types.Collections;

public sealed class FusionDirectiveDefinitionCollection
    : IReadOnlyDirectiveDefinitionCollection
    , IEnumerable<FusionDirectiveDefinition>
{
    private readonly FusionDirectiveDefinition[] _directives;
    private readonly FrozenDictionary<string, FusionDirectiveDefinition> _directiveLookup;

    public FusionDirectiveDefinitionCollection(FusionDirectiveDefinition[] directives)
    {
        ArgumentNullException.ThrowIfNull(directives);
        _directives = directives;
        _directiveLookup = directives.ToFrozenDictionary(t => t.Name);
    }

    public int Count => _directiveLookup.Count;

    public FusionDirectiveDefinition this[string name]
        => _directiveLookup[name];

    IDirectiveDefinition IReadOnlyDirectiveDefinitionCollection.this[string name]
        => _directiveLookup[name];

    public FusionDirectiveDefinition this[int index]
        => _directives[index];

    IDirectiveDefinition IReadOnlyList<IDirectiveDefinition>.this[int index]
        => _directives[index];

    public FusionDirectiveDefinition SkipDirective
        => _directiveLookup[SpecDirectiveNames.Skip.Name];

    public FusionDirectiveDefinition IncludeDirective
        => _directiveLookup[SpecDirectiveNames.Include.Name];

    public bool TryGetDirective(
        string name,
        [NotNullWhen(true)] out FusionDirectiveDefinition? definition)
        => _directiveLookup.TryGetValue(name, out definition);

    bool IReadOnlyDirectiveDefinitionCollection.TryGetDirective(
        string name, [NotNullWhen(true)]
        out IDirectiveDefinition? definition)
    {
        if (_directiveLookup.TryGetValue(name, out var d))
        {
            definition = d;
            return true;
        }

        definition = null;
        return false;
    }

    public bool ContainsName(string name)
        => _directiveLookup.ContainsKey(name);

    public IEnumerable<FusionDirectiveDefinition> AsEnumerable()
        => Unsafe.As<IEnumerable<FusionDirectiveDefinition>>(_directives);

    public IEnumerator<FusionDirectiveDefinition> GetEnumerator()
        => AsEnumerable().GetEnumerator();

    IEnumerator<IDirectiveDefinition> IEnumerable<IDirectiveDefinition>.GetEnumerator()
        => AsEnumerable().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => AsEnumerable().GetEnumerator();
}
