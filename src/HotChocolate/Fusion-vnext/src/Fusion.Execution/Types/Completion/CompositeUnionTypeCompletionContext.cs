using System.Collections.Frozen;
using HotChocolate.Fusion.Types.Collections;

namespace HotChocolate.Fusion.Types.Completion;

internal ref struct CompositeUnionTypeCompletionContext(
    DirectiveCollection directives,
    FrozenDictionary<string, CompositeObjectType> types)
{
    public DirectiveCollection Directives { get; } = directives;

    public FrozenDictionary<string, CompositeObjectType> Types { get; } = types;
}
