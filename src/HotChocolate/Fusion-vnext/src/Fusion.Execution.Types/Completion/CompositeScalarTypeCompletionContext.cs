using HotChocolate.Fusion.Types.Collections;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Types.Completion;

internal readonly ref struct CompositeScalarTypeCompletionContext(
    ScalarValueKind valueKind,
    FusionDirectiveCollection directives,
    Uri? specifiedBy,
    ScalarSerializationType serializationType,
    string? pattern)
{
    public ScalarValueKind ValueKind { get; } = valueKind;

    public FusionDirectiveCollection Directives { get; } = directives;

    public Uri? SpecifiedBy { get; } = specifiedBy;

    public ScalarSerializationType SerializationType { get; } = serializationType;

    public string? Pattern { get; } = pattern;
}
