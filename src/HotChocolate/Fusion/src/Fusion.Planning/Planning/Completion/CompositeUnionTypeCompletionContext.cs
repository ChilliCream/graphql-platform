using System.Collections.Frozen;
using HotChocolate.Fusion.Planning.Collections;

namespace HotChocolate.Fusion.Planning.Completion;

internal ref struct CompositeUnionTypeCompletionContext(
    DirectiveCollection directives,
    FrozenDictionary<string, CompositeObjectType> types)
{
    public DirectiveCollection Directives { get; } = directives;

    public FrozenDictionary<string, CompositeObjectType> Types { get; } = types;
}
