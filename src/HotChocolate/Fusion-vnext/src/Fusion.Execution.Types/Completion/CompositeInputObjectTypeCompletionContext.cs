using HotChocolate.Features;
using HotChocolate.Fusion.Types.Collections;

namespace HotChocolate.Fusion.Types.Completion;

internal ref struct CompositeInputObjectTypeCompletionContext(
    FusionDirectiveCollection directives,
    IFeatureCollection features)
{
    public FusionDirectiveCollection Directives { get; } = directives;

    public IFeatureCollection Features { get; } = features;
}
