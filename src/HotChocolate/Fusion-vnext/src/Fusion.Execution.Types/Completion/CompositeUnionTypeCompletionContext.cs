using HotChocolate.Fusion.Types.Collections;

namespace HotChocolate.Fusion.Types.Completion;

internal ref struct CompositeUnionTypeCompletionContext(
    FusionDirectiveCollection directives,
    FusionObjectTypeDefinitionCollection types)
{
    public FusionDirectiveCollection Directives { get; } = directives;

    public FusionObjectTypeDefinitionCollection Types { get; } = types;
}
