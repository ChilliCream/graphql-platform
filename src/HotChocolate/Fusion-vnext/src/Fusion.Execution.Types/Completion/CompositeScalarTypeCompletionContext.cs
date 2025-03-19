using HotChocolate.Fusion.Types.Collections;

namespace HotChocolate.Fusion.Types.Completion;

internal ref struct CompositeScalarTypeCompletionContext(
    FusionDirectiveCollection directives)
{
    public FusionDirectiveCollection Directives { get; } = directives;
}
