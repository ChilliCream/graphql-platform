using HotChocolate.Features;
using HotChocolate.Fusion.Types.Collections;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Types.Completion;

internal readonly ref struct CompositeEnumValueCompletionContext(
    IEnumTypeDefinition declaringType,
    FusionDirectiveCollection directives,
    IFeatureCollection features)
{
    public IEnumTypeDefinition DeclaringType { get; } = declaringType;

    public FusionDirectiveCollection Directives { get; } = directives;

    public IFeatureCollection Features { get; } = features;
}
