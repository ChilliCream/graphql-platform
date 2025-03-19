using HotChocolate.Fusion.Types.Collections;

namespace HotChocolate.Fusion.Types.Completion;

internal ref struct CompositeInterfaceTypeCompletionContext(
    FusionDirectiveCollection directives,
    FusionInterfaceTypeDefinitionCollection interfaces,
    SourceInterfaceTypeCollection sources)
{
    public FusionDirectiveCollection Directives { get; } = directives;

    public FusionInterfaceTypeDefinitionCollection Interfaces { get; } = interfaces;

    public SourceInterfaceTypeCollection Sources { get; } = sources;
}
