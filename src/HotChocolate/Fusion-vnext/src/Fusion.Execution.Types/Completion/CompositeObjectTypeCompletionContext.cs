using HotChocolate.Fusion.Types.Collections;

namespace HotChocolate.Fusion.Types.Completion;

internal ref struct CompositeObjectTypeCompletionContext(
    FusionDirectiveCollection directives,
    FusionInterfaceTypeDefinitionCollection interfaces,
    SourceObjectTypeCollection sources)
{
    public FusionDirectiveCollection Directives { get; } = directives;

    public FusionInterfaceTypeDefinitionCollection Interfaces { get; } = interfaces;

    public SourceObjectTypeCollection Sources { get; } = sources;
}
