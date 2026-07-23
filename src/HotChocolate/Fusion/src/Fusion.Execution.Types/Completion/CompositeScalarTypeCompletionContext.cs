using HotChocolate.Fusion.Types.Collections;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Types.Completion;

internal readonly ref struct CompositeScalarTypeCompletionContext(
    FusionDirectiveCollection directives,
    string? specifiedBy,
    ScalarSerializationType serializationType,
    string? pattern)
{
    public FusionDirectiveCollection Directives { get; } = directives;

    public string? SpecifiedBy { get; } = specifiedBy;

    public ScalarSerializationType SerializationType { get; } = serializationType;

    public string? Pattern { get; } = pattern;
}
