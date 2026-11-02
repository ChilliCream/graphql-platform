using HotChocolate.Features;
using HotChocolate.Fusion.Types.Collections;

namespace HotChocolate.Fusion.Types.Completion;

internal readonly ref struct CompositeInterfaceTypeCompletionContext(
    FusionDirectiveCollection directives,
    FusionInterfaceTypeDefinitionCollection interfaces,
    SourceInterfaceTypeCollection sources,
    IFeatureCollection features)
{
    public FusionDirectiveCollection Directives { get; } = directives;

    public FusionInterfaceTypeDefinitionCollection Interfaces { get; } = interfaces;

    public SourceInterfaceTypeCollection Sources { get; } = sources;

    public IFeatureCollection Features { get; } = features;
}
