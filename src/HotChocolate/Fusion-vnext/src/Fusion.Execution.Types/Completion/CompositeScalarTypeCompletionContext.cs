using HotChocolate.Fusion.Types.Collections;

namespace HotChocolate.Fusion.Types.Completion;

internal ref struct CompositeScalarTypeCompletionContext(
    ScalarValueKind valueKind,
    FusionDirectiveCollection directives)
{
    public ScalarValueKind ValueKind { get; } = valueKind;

    public FusionDirectiveCollection Directives { get; } = directives;
}
