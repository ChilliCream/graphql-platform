using HotChocolate.Features;
using HotChocolate.Fusion.Types.Collections;

namespace HotChocolate.Fusion.Types.Completion;

internal readonly ref struct CompositeUnionTypeCompletionContext(
    FusionObjectTypeDefinitionCollection types,
    FusionDirectiveCollection directives,
    SourceUnionTypeCollection sources,
    IFeatureCollection features)
{
    public FusionDirectiveCollection Directives { get; } = directives;

    public FusionObjectTypeDefinitionCollection Types { get; } = types;

    public SourceUnionTypeCollection Sources { get; } = sources;

    public IFeatureCollection Features { get; } = features;
}
