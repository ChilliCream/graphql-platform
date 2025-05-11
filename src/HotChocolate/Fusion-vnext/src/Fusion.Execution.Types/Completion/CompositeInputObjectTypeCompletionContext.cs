using HotChocolate.Fusion.Types.Collections;

namespace HotChocolate.Fusion.Types.Completion;

internal ref struct CompositeInputObjectTypeCompletionContext(
    FusionDirectiveCollection directives)
{
    public FusionDirectiveCollection Directives { get; } = directives;
}
