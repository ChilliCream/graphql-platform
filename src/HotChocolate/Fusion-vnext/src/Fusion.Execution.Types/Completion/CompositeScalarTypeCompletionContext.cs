using HotChocolate.Fusion.Types.Collections;

namespace HotChocolate.Fusion.Types.Completion;

internal readonly ref struct CompositeScalarTypeCompletionContext(
    ScalarValueKind valueKind,
    FusionDirectiveCollection directives,
    Uri? specifiedBy)
{
    public ScalarValueKind ValueKind { get; } = valueKind;

    public FusionDirectiveCollection Directives { get; } = directives;

    public Uri? SpecifiedBy { get; } = specifiedBy;
}
