using System.Collections.Frozen;
using HotChocolate.Fusion.Types.Collections;

namespace HotChocolate.Fusion.Types.Completion;

internal ref struct CompositeUnionTypeCompletionContext(
    DirectiveCollection directives,
    FrozenDictionary<string, FusionObjectType> types)
{
    public DirectiveCollection Directives { get; } = directives;

    public FrozenDictionary<string, FusionObjectType> Types { get; } = types;
}
